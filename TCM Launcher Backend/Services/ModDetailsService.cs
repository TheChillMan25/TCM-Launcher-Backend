using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using TCM_Launcher_Backend.Model.Curseforge;
using TCM_Launcher_Backend.Model.Modrinth;
using TCML_Class_library;

namespace TCM_Launcher_Backend.Services
{
    public class ModDetailsService : IModDetailsService
    {
        private readonly HttpClient client;
        private readonly IConfiguration configuration;
        private readonly IMemoryCache cache;

        public ModDetailsService(HttpClient httpClient, IConfiguration configuration, IMemoryCache cache)
        {
            this.client = httpClient;
            this.configuration = configuration;
            this.cache = cache;
        }

        public async Task<ModDetails?> GetCurseforgeModDetails(string procjectId, string version, bool needDesc = true)
        {
            string cacheKey = $"details_{procjectId.ToLower().Trim()}_{version}";

            if (cache.TryGetValue(cacheKey, out ModDetails? cachedResult))
            {
                return cachedResult;
            }
            string url = $"https://api.curseforge.com/v1/mods/{Uri.EscapeDataString(procjectId)}";
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
                    var versionsTask = GetCurseforgeModVersions(procjectId, version);
                    var descriptionTask = needDesc ? GetCurseforgeModDescription(procjectId) : Task.FromResult(string.Empty);
                    using var stream = await response.Content.ReadAsStreamAsync();
                    var wrapper = await JsonSerializer.DeserializeAsync<CurseforgeResponse<CurseforgeHit>>(stream);
                    var curseforgeDetails = wrapper?.Data;
                    if (curseforgeDetails == null) return null;
                    var enviroments = CurseforgeHelper.ConvertForgeCategoriesToEnviroments(curseforgeDetails.Categories);
                    await Task.WhenAll(versionsTask, descriptionTask);
                    var details = new ModDetails
                    {
                        Id = curseforgeDetails.Id.ToString(),
                        Title = curseforgeDetails.Name,
                        Source = ModSource.CurseForge,
                        Summary = curseforgeDetails.Summary,
                        IconUrl = curseforgeDetails.Logo.Url,
                        Author = curseforgeDetails.Authors.FirstOrDefault()?.Name ?? "Unkown",
                        DownloadCount = curseforgeDetails.DownloadCount,
                        Client_Side = enviroments["ClientSide"],
                        Server_Side = enviroments["ServerSide"],
                        Categories = curseforgeDetails.Categories.Select(c => c.Name).ToList(),
                        Versions = versionsTask.Result,
                        Description = descriptionTask.Result,
                    };

                    var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                    cache.Set(cacheKey, details, cacheOptions);
                    
                    return details;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }

        }
        private async Task<List<ModVersion>> GetCurseforgeModVersions(string projectId, string version)
        {
            string url = $"https://api.curseforge.com/v1/mods/{Uri.EscapeDataString(projectId)}/files?gameVersion={Uri.EscapeDataString(version)}&modLoaderType=1";

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
                    var filesResponse = await JsonSerializer.DeserializeAsync<CurseforgeFiles>(stream);

                    if (filesResponse?.Data == null) return new List<ModVersion>();

                    var universalVersions = filesResponse.Data.Select(f => new ModVersion
                    {
                        Id = f.Id.ToString(),
                        Name = f.DisplayName,
                        ProjectId = f.ModId.ToString(),
                        VersionNumber = f.FileName,
                        VersionType = f.ReleaseType == 1 ? "release" : f.ReleaseType == 2 ? "beta" : "alpha",
                        Downloads = f.DownloadCount,
                        Files = new List<ModFile>
                {
                    new ModFile
                    {
                        Url = f.DownloadUrl,
                        FileName = f.FileName,
                        Size = f.FileLength,
                        Primary = true
                    }
                },
                        Dependencies = f.Dependencies?.Select(d => new ModDependency
                        {
                            ProjectId = d.ModId.ToString(),
                            DependencyType = d.RelationType == 3 ? "required" : d.RelationType == 2 ? "optional" : "embedded"
                        }).ToList() ?? new List<ModDependency>()
                    }).ToList();

                    return universalVersions;
                }
                return new List<ModVersion>();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<ModVersion>();
            }
        }
        private async Task<string> GetCurseforgeModDescription(string projectId)
        {
            string url = $"https://api.curseforge.com/v1/mods/{Uri.EscapeDataString(projectId)}/description";

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
                    var wrapper = await JsonSerializer.DeserializeAsync<CurseforgeResponse<string>>(stream);
                    string htmlContent = wrapper?.Data ?? string.Empty;

                    var config = new ReverseMarkdown.Config
                    {
                        Tags = { Unknown = ReverseMarkdown.Config.UnknownTagsOption.Drop },
                        GithubFlavored = true
                    };

                    var converter = new ReverseMarkdown.Converter(config);
                    string markdownContent = converter.Convert(htmlContent);

                    return markdownContent;
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return string.Empty;
            }
        }

        public async Task<ModDetails?> GetModrinthModDetails(string procjectId, string version)
        {
            string cacheKey = $"details_{procjectId.ToLower().Trim()}_{version}";

            if(cache.TryGetValue(cacheKey, out ModDetails? cachedResult))
            {
                return cachedResult;
            }

            var versionsTask = GetModrinthModVersions(procjectId, version);
            var authorTask = GetModrinthAuthorAsync(procjectId);
            string url = $"https://api.modrinth.com/v2/project/{Uri.EscapeDataString(procjectId)}";
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
                    using var stream = await response.Content.ReadAsStreamAsync();
                    var modrinthDetails = JsonSerializer.Deserialize<ModrinthDetails>(stream);
                    await Task.WhenAll(versionsTask, authorTask);
                    var details = new ModDetails
                    {
                        Id = modrinthDetails.ProjectId,
                        Title = modrinthDetails.Title,
                        Author = authorTask.Result,
                        Categories = modrinthDetails.Categories,
                        Client_Side = modrinthDetails.ClientSide,
                        Server_Side = modrinthDetails.ServerSide,
                        DownloadCount = modrinthDetails.Downloads,
                        Source = ModSource.Modrinth,
                        Description = modrinthDetails.Body,
                        IconUrl = modrinthDetails.IconUrl,
                        Summary = modrinthDetails.Description,
                        Versions = versionsTask.Result,
                    };

                    var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                    cache.Set(cacheKey, details, cacheOptions);

                    return details;
                }
                return null;

            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }
        private async Task<List<ModVersion>> GetModrinthModVersions(string procjectId, string version)
        {
            string rawVersion = $"[\"{version}\"]";
            string encodedVersion = $"game_versions={Uri.EscapeDataString(rawVersion)}";
            string encodedLoaders = $"loaders={Uri.EscapeDataString("[\"forge\"]")}";
            string url = $"https://api.modrinth.com/v2/project/{Uri.EscapeDataString(procjectId)}/version?{encodedVersion}&{encodedLoaders}&include_changelog=false";

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
                    using var stream = await response.Content.ReadAsStreamAsync();
                    var modrinthVersions = JsonSerializer.Deserialize<List<ModrinthVersion>>(stream);
                    if(modrinthVersions == null) return new List<ModVersion>();
                    var universalVersions = modrinthVersions.Select(v => new ModVersion
                    {
                        Id = v.Id,
                        Name = v.Name,
                        ProjectId = v.ProjectId,
                        VersionNumber = v.VersionNumber,
                        VersionType = v.VersionType,
                        Downloads = v.Downloads,
                        Files = v.Files.Select(f => new ModFile
                        {
                            Url = f.Url,
                            Primary = f.Primary,
                            FileName = f.FileName,
                            Size = f.Size
                        }).ToList(),
                        Dependencies = v.Dependencies.Select(d => new ModDependency
                        {
                            VersionId = d.VersionId,
                            ProjectId = d.ProjectId,
                            FileName = d.FileName,
                            DependencyType = d.DependencyType
                        }).ToList()
                    }).ToList();
                    return universalVersions;
                }
                return new List<ModVersion>();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<ModVersion>();
            }
        }
        private async Task<string> GetModrinthAuthorAsync(string projectId)
        {
            string url = $"https://api.modrinth.com/v2/project/{Uri.EscapeDataString(projectId)}/members";
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
                    using var stream = await response.Content.ReadAsStreamAsync();
                    var members = await JsonSerializer.DeserializeAsync<List<ModrinthMember>>(stream);
                    return members?.FirstOrDefault()?.User?.Username ?? "Unknown";
                }
            }
            catch
            {
            }
            return "Unknown";
        }
    }
}
