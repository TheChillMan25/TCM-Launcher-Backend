using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using TCML_Class_library;

namespace TCM_Launcher_Backend.Functions;

public class ModDetailsFunction
{
    private readonly ILogger<ModDetailsFunction> _logger;
    private readonly IModDetailsService modDetailsService;

    public ModDetailsFunction(ILogger<ModDetailsFunction> logger, IModDetailsService modDetailsService)
    {
        _logger = logger;
        this.modDetailsService = modDetailsService;
    }

    [Function("GetModDetails")]
    public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        string? query = req.Query["query"];
        string? mcVersion = req.Query["mcVersion"];
        string? sourceText = req.Query["source"];
        string? needDescTxt = req.Query["needDesc"];

        bool.TryParse(needDescTxt, out var needDesc);
        Enum.TryParse<ModSource>(sourceText, true, out var modSource);

        if (string.IsNullOrEmpty(query))
        {
            return new BadRequestObjectResult("Query is required");
        }

        ModDetails result = null;
        switch (modSource)
        {
            case ModSource.Modrinth:
                result = await modDetailsService.GetModrinthModDetails(query, mcVersion);
                break;
            case ModSource.CurseForge:
                result = await modDetailsService.GetCurseforgeModDetails(query, mcVersion, needDesc);
                break;
        }
        return new OkObjectResult(result);
    }
}