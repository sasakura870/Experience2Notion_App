// using Experience2Notion.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Temporarily commented out until Experience2Notion library is available
// builder.Services.AddSingleton<GoogleBookSeacher>();
// builder.Services.AddSingleton<GoogleImageSearcher>();
// builder.Services.AddSingleton<SpotifyClient>();
// builder.Services.AddSingleton<NotionClient>();

builder.Build().Run();
