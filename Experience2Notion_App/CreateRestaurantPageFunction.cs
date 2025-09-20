using Experience2Notion.Services;
using Experience2Notion_App.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net.Mime;
using System.Text.Json;

namespace Experience2Notion_App;

public class CreateRestaurantPageFunction(ILogger<CreateRestaurantPageFunction> logger, NotionClient notionClient)
{
    private readonly ILogger<CreateRestaurantPageFunction> _logger = logger;
    private readonly NotionClient _notionClient = notionClient;

    [Function("CreateRestaurantPage")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation(requestBody);
            var data = JsonSerializer.Deserialize<CreateRestaurantRequest>(requestBody);

            if (data is null || string.IsNullOrWhiteSpace(data.Name))
            {
                return new BadRequestObjectResult("店名が指定されていません。");
            }
            if (string.IsNullOrWhiteSpace(data.Address))
            {
                return new BadRequestObjectResult("住所が指定されていません。");
            }
            var address = data.Address;
            var link = "";

            var imageIdList = new List<string>();
            foreach (var photo in data.Photos)
            {
                var photoData = Convert.FromBase64String(photo);
                var imageId = await _notionClient.UploadImageAsync($"{data.Name}.jpg", photoData, MediaTypeNames.Image.Jpeg);
                imageIdList.Add(imageId);
            }

            var result = await _notionClient.CreateRestaurantPageAsync(data.Name, address, link, data.VisitDate, imageIdList);
            return new OkObjectResult(result);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON format in request");
            return new BadRequestObjectResult("Invalid JSON format in request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing restaurant page creation request");
            return new StatusCodeResult(500);
        }
    }
}