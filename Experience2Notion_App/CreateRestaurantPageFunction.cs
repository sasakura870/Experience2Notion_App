using Experience2Notion_App.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Experience2Notion_App;

public class CreateRestaurantPageFunction
{
    private readonly ILogger<CreateRestaurantPageFunction> _logger;

    public CreateRestaurantPageFunction(ILogger<CreateRestaurantPageFunction> logger)
    {
        _logger = logger;
    }

    [Function("CreateRestaurantPage")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation(requestBody);
            var data = JsonSerializer.Deserialize<CreateRestaurantRequest>(requestBody);

            foreach (var photo in data.Photos)
            {
                var photoData = Convert.FromBase64String(photo);
                File.WriteAllBytes($"{Guid.NewGuid()}.jpg", photoData);
            }

            if (data is null || string.IsNullOrWhiteSpace(data.Name) || string.IsNullOrWhiteSpace(data.Address))
            {
                return new BadRequestObjectResult("Restaurant name and address are required.");
            }

            // Log the received data for verification
            _logger.LogInformation($"Restaurant page creation requested - Name: {data.Name}, Address: {data.Address}, Visit Date: {data.VisitDate}, Photos Count: {data.Photos.Length}");

            // TODO: Implement Notion page creation when Experience2Notion library is available
            // For now, return success with the received data
            var result = new
            {
                message = "Restaurant data received successfully",
                restaurantName = data.Name,
                address = data.Address,
                visitDate = data.VisitDate,
                photosCount = data.Photos.Length
            };

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