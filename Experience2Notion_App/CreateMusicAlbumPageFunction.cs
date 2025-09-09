using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Experience2Notion_App;

public class CreateMusicAlbumPageFunction
{
    private readonly ILogger<CreateMusicAlbumPageFunction> _logger;

    public CreateMusicAlbumPageFunction(ILogger<CreateMusicAlbumPageFunction> logger)
    {
        _logger = logger;
    }

    [Function("CreateMusicAlbumPage")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}