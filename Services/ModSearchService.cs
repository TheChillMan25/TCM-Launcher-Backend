using System.Reflection.PortableExecutable;
using System.Text.Json;
using TCM_Launcher_Backend.Model.Curseforge;
using TCM_Launcher_Backend.Model.Modrinth;
using TCML_Class_library;

namespace TCM_Launcher_Backend.Services
{
    public class ModSearchService
    {
        private static readonly HttpClient client = new HttpClient();
        public static readonly ModSearchService Instance = new ModSearchService();

        public async Task<List<ModSearchResult>> SearchModrinthAsync(string query, string version)
        {
            var result = new List<ModSearchResult>();
            string facets = $"[[\"versions:{version}\"]]";
            string url = $"https://api.modrinth.com/v2/search?query={query}&facets={Uri.EscapeDataString(facets)}";

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
                    var searchResultJson = await response.Content.ReadAsStringAsync();
                    var searchResult = JsonSerializer.Deserialize<ModrinthSearchResult>(searchResultJson);

                    if(searchResult.Hits != null &&  searchResult.Hits.Count > 0)
                    {
                        foreach(var hit in searchResult.Hits)
                        {
                            var modSearchResult = new ModSearchResult
                            {
                                Id = hit.ProjectId,
                                Name = hit.Title,
                                Source = ModSource.Modrinth,
                                Summary = hit.Description,
                                Author = hit.Author,
                                DownloadCount = hit.Downloads,
                                IconUrl = hit.IconUrl,
                                Client_Side = hit.ClientSide,
                                Server_Side = hit.ServerSide,
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
            var result = new List<ModSearchResult>();

            string url = $"https://api.curseforge.com/v1/mods/search/?gameId=432&classId=6&searchFilter={query}&gameVersion={version}";

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
                    var searchResultJson = await response.Content.ReadAsStringAsync();
                    var searchResult = JsonSerializer.Deserialize<CurseforgeSearchResult>(searchResultJson);

                    if (searchResult.Data != null && searchResult.Data.Count > 0)
                    {
                        foreach (var hit in searchResult.Data)
                        {
                            var enviroments = ConvertForgeCategoriesToEnviroments(hit.Categories);
                            var modSearchResult = new ModSearchResult
                            {
                                Id = hit.Id.ToString(),
                                Name = hit.Name,
                                Source = ModSource.CurseForge,
                                Summary = hit.Summary,
                                Author = hit.Authors[0].Name ?? "NULL",
                                DownloadCount = hit.DownloadCount,
                                Client_Side = enviroments["ClientSide"],
                                Server_Side = enviroments["ServerSide"],
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
            Task<List<ModSearchResult>> modrinthTask = SearchModrinthAsync(query, version);
            Task<List<ModSearchResult>> curseforgeTask = SearchCurseforgeAsync(query, version);
            await Task.WhenAll(modrinthTask,  curseforgeTask);
            var modrinthSearchRes = modrinthTask.Result;
            var curseforgeSearchRes = curseforgeTask.Result;

            var concatList = modrinthSearchRes.Concat(curseforgeSearchRes).ToList();

            var mergedList = MergeAndDeduplicate(concatList);

            return mergedList;
        }

        private List<ModSearchResult> MergeAndDeduplicate(List<ModSearchResult> rawResult)
        {
            var mergedDirectory = new Dictionary<string, ModSearchResult>();

            foreach (var mod in rawResult)
            {
                string key = NormalizeModName(mod.Name);

                if(string.IsNullOrEmpty(key)) continue;

                if(mergedDirectory.TryGetValue(key, out var existingMod))
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
                        existingMod.Client_Side = mod.Client_Side;
                        existingMod.Server_Side = mod.Server_Side;
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
                string name = mod.Name.ToLowerInvariant();
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
                score += Math.Log10(mod.DownloadCount - 1) * 100;
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
            var cleanChars = Array.FindAll(charArray, c => char.IsLetterOrDigit(c));

            return new string(cleanChars);
        }

        private Dictionary<string, string> ConvertForgeCategoriesToEnviroments(List<CurseforgeCategory> categories)
        {
            var enviroments = new Dictionary<string, string>();

            enviroments["ClientSide"] = "required";
            enviroments["ServerSide"] = "required";

            if(categories != null)
            {
                bool isClientOnly = false;
                bool isServerOnly = false;
                bool hasContent = false;

                foreach (var cat in categories)
                {
                    string slug = cat.Slug?.ToLowerInvariant() ?? "";

                    if (slug == "cosmetic" ||
                        slug == "map-and-information" ||
                        slug == "twitch-integration" ||
                        slug == "mc-miscellaneous")
                    {
                        isClientOnly = true;
                    }
                    else if (slug == "server-utility")
                    {
                        isServerOnly = true;
                    }
                    else if (slug == "armor-tools-and-weapons" ||
                     slug == "technology" ||
                     slug == "magic" ||
                     slug == "storage" ||
                     slug == "food" ||
                     slug == "adventure-and-rpg" ||
                     slug == "world-gen" ||
                     slug == "biomes" ||
                     slug == "dimensions" ||
                     slug == "mobs" ||
                     slug == "ores-and-resources" ||
                     slug == "structures")
                    {
                        hasContent = true;
                    }
                }

                if (isServerOnly)
                {
                    enviroments["ClientSide"] = "unsupported";
                    enviroments["ServerSide"] = "required";
                }
                if (isServerOnly && !hasContent)
                {
                    enviroments["ClientSide"] = "required";
                    enviroments["ServerSide"] = "unsupported";
                }
            }

            return enviroments;
        }
    }
}
