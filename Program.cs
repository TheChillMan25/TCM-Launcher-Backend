using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TCM_Launcher_Backend;
using TCM_Launcher_Backend.Interfaces;
using TCM_Launcher_Backend.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();
builder.Services.AddHttpClient<IModSearchService, ModSearchService>();
builder.Services.AddHttpClient<IModDetailsService, ModDetailsService>();
builder.Services.AddMemoryCache();

builder.Build().Run();
