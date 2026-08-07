using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using TCM_Launcher_Backend.Interfaces;
using TCM_Launcher_Backend.Model.Curseforge;
using TCM_Launcher_Backend.Model.Modrinth;
using TCML_Class_library;

namespace TCM_Launcher_Backend.Services
{
    public class ModSearchService : IModSearchService
    {
        private readonly HttpClient client;
        private readonly IConfiguration configuration;
        private readonly IMemoryCache cache;

        public ModSearchService(HttpClient httpClient, IConfiguration configuration, IMemoryCache cache)
        {
            this.client = httpClient;
            this.configuration = configuration;
            this.cache = cache;
        }

        public async Task<List<ModSearchResult>> SearchModrinthAsync(string query, string version)
        {
            var result = new List<ModSearchResult>();
            string encodedQuery = Uri.EscapeDataString(query);
            string facets = $"[[\"versions:{version}\"],[\"project_type:mod\"],[\"categories:forge\"]]";
            string url = $"https://api.modrinth.com/v2/search?query={encodedQuery}&facets={Uri.EscapeDataString(Uri.UnescapeDataString(facets))}";

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                string uAHeader = Environment.GetEnvironmentVariable("MODRINTH_USERAGENT_HEADER");
                if (!string.IsNullOrEmpty(uAHeader))
                {
                    request.Headers.UserAgent.ParseAdd(uAHeader);
                }

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    using var stream= await response.Content.ReadAsStreamAsync();
                    var searchResult = JsonSerializer.Deserialize<ModrinthSearchResult>(stream);

                    if(searchResult.Hits != null &&  searchResult.Hits.Count > 0)
                    {
                        foreach(var hit in searchResult.Hits)
                        {
                            var modSearchResult = new ModSearchResult
                            {
                                Id = hit.ProjectId,
                                Title = hit.Title,
                                Source = ModSource.Modrinth,
                                Summary = hit.Description,
                                Author = hit.Author,
                                DownloadCount = hit.Downloads,
                                IconUrl = hit.IconUrl,
                                Client_Side = hit.ClientSide,
                                Server_Side = hit.ServerSide,
                                Categories = hit.Categories,
                            };
                            result.Add(modSearchResult);
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Error during modrinth api search: {response.StatusCode}-{response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return result;
        }

        public async Task<List<ModSearchResult>> SearchCurseforgeAsync(string query, string version)
        {
            string encodedQuery = Uri.EscapeDataString(query);
            string encodedVersion = Uri.EscapeDataString(version);
            string url = $"https://api.curseforge.com/v1/mods/search?gameId=432&classId=6&searchFilter={encodedQuery}&gameVersion={encodedVersion}&modLoaderType=1&searchFilter=2&pageSize=50&sortField=2&sortOrder=desc";

            var result = await FetchCurseforgeModsAsync(url);
            
            if(result.Count == 0)
            {
                string fallbackUrl = $"https://api.curseforge.com/v1/mods/search?gameId=432&classId=6&searchFilter={encodedQuery}&gameVersion={encodedVersion}&pageSize=50&sortField=2&sortOrder=desc";
                result = await FetchCurseforgeModsAsync(fallbackUrl);
            }

            return result;
        }

        private async Task<List<ModSearchResult>> FetchCurseforgeModsAsync(string url)
        {
            var result = new List<ModSearchResult>();
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                string apiKey = Environment.GetEnvironmentVariable("CURSEFORGE_API_KEY");
                if (!string.IsNullOrEmpty(apiKey))
                {
                    request.Headers.Add("x-api-key", apiKey);
                }

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    using var stream = await response.Content.ReadAsStreamAsync();
                    var searchResult = JsonSerializer.Deserialize<CurseforgeSearchResult>(stream);

                    if (searchResult.Data != null && searchResult.Data.Count > 0)
                    {
                        foreach (var hit in searchResult.Data)
                        {
                            var categories = hit.Categories ?? new List<CurseforgeCategory>();
                            var enviroments = CurseforgeHelper.ConvertForgeCategoriesToEnviroments(categories);
                            var modSearchResult = new ModSearchResult
                            {
                                Id = hit.Id.ToString(),
                                Title = hit.Name,
                                Source = ModSource.CurseForge,
                                Summary = hit.Summary,
                                IconUrl = hit.Logo?.Url ?? "",
                                Author = (hit.Authors != null && hit.Authors.Count > 0)
                                    ? (hit.Authors[0].Name ?? "Unknown")
                                    : "Unknown",
                                DownloadCount = hit.DownloadCount,
                                Client_Side = enviroments["ClientSide"],
                                Server_Side = enviroments["ServerSide"],
                                Categories = categories.Select(c => c.Name).ToList(),
                            };
                            result.Add(modSearchResult);
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Error during modrinth api search: {response.StatusCode}-{response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return result;
        }

        public async Task<List<ModSearchResult>> SearchModsAsync(string query, string version)
        {
            string cacheKey = $"search_{query.ToLower().Trim()}_{version}";

            if (cache.TryGetValue(cacheKey, out List<ModSearchResult>? cachedResult))
            {
                return cachedResult!;
            }

            Task<List<ModSearchResult>> modrinthTask = SearchModrinthAsync(query, version);
            Task<List<ModSearchResult>> curseforgeTask = SearchCurseforgeAsync(query, version);
            await Task.WhenAll(modrinthTask,  curseforgeTask);
            var modrinthSearchRes = modrinthTask.Result;
            var curseforgeSearchRes = curseforgeTask.Result;

            var concatList = modrinthSearchRes.Concat(curseforgeSearchRes).ToList();
            var mergedList = MergeAndDeduplicate(concatList);
            var sortedList = SortByPseudoRelevance(mergedList, query);

            var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
            cache.Set(cacheKey, sortedList, cacheOptions);

            return sortedList;
        }

        private List<ModSearchResult> MergeAndDeduplicate(List<ModSearchResult> rawResult)
        {
            var mergedDirectory = new Dictionary<string, ModSearchResult>();

            foreach (var mod in rawResult)
            {
                string key = NormalizeModName(mod.Title);

                if(string.IsNullOrEmpty(key)) continue; 
                if (mod.Source == ModSource.Modrinth) mod.ModrinthId = mod.Id;
                if (mod.Source == ModSource.CurseForge) mod.CurseforgeId = mod.Id;

                if (mergedDirectory.TryGetValue(key, out var existingMod))
                {
                    existingMod.DownloadCount += mod.DownloadCount;
                    if(existingMod.Summary.Length < mod.Summary.Length)
                    {
                        existingMod.Summary = mod.Summary;
                    }
                    if(string.IsNullOrEmpty(existingMod.IconUrl) && !string.IsNullOrEmpty(mod.IconUrl))
                    {
                        existingMod.IconUrl = mod.IconUrl;
                    }
                    if (mod.Source == ModSource.Modrinth)
                    {
                        existingMod.ModrinthId = mod.Id;
                        existingMod.Client_Side = mod.Client_Side;
                        existingMod.Server_Side = mod.Server_Side;
                    }
                    else if (mod.Source == ModSource.CurseForge)
                    {
                        existingMod.CurseforgeId = mod.Id;
                    }
                }
                else
                {
                    mergedDirectory[key] = mod;
                }
            }
            return mergedDirectory.Values.ToList();
        }

        private List<ModSearchResult> SortByPseudoRelevance(List<ModSearchResult> modList, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return modList;

            string q = query.ToLowerInvariant().Trim();

            return modList.OrderByDescending(mod =>
            {
                double score = 0;
                string name = mod.Title.ToLowerInvariant();
                string summary = mod.Summary?.ToLowerInvariant() ?? "";

                if (name == q)
                {
                    score += 10000;
                }
                else if (name.StartsWith(q))
                {
                    score += 5000;
                }
                else if (name.Contains(q))
                {
                    score += 2000;
                }
                if (summary.Contains(q))
                {
                    score += 200;
                }
                long downloads = Math.Max(1, mod.DownloadCount);
                score += Math.Log10(downloads) * 100;
                return score;
            }).ToList();
        }

        private string NormalizeModName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;

            string normalized = name.ToLowerInvariant();

            normalized = normalized.Replace("(fabric)", "")
                                   .Replace("(forge)", "")
                                   .Replace("[fabric]", "")
                                   .Replace("[forge]", "")
                                   .Replace("-fabric", "")
                                   .Replace("-forge", "");

            var charArray = normalized.ToCharArray();
            var cleanChars = Array.FindAll(charArray, c => char.IsLetterOrDigit(c) || c == '+');
            return new string(cleanChars);
        }
    }
}
