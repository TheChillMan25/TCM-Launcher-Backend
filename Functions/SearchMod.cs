using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using TCM_Launcher_Backend.Services;

namespace TCM_Launcher_Backend.Functions
{
    public class SearchMod
    {
        private readonly ILogger<SearchMod> _logger;

        public SearchMod(ILogger<SearchMod> logger)
        {
            _logger = logger;
        }

        [Function("SearchMod")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string? query = req.Query["query"];
            string? version = req.Query["version"];

            if (string.IsNullOrEmpty(query))
            {
                return new BadRequestObjectResult("Query is required");
            }

            var result = await ModSearchService.Instance.SearchModsAsync(query, version);

            return new OkObjectResult(result.OrderByDescending(r => r.DownloadCount));
        }
    }
}
