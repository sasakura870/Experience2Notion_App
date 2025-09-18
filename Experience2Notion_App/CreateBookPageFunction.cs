// using Experience2Notion.Exceptions;
// using Experience2Notion.Services;
using Experience2Notion_App.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Experience2Notion_App;

public class CreateBookPageFunction(ILogger<CreateBookPageFunction> logger/*, GoogleBookSeacher googleBookSeacher, GoogleImageSearcher googleImageSearcher, NotionClient notionClient*/)
{
    private readonly ILogger<CreateBookPageFunction> _logger = logger;
    // Temporarily commented out until Experience2Notion library is available
    // private readonly GoogleBookSeacher _googleBookSeacher = googleBookSeacher;
    // private readonly GoogleImageSearcher _googleImageSearcher = googleImageSearcher;
    // private readonly NotionClient _notionClient = notionClient;

    [Function("CreateBookPage")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation(requestBody);
            var data = JsonSerializer.Deserialize<CreateBookRequest>(requestBody);
            if (data is null || string.IsNullOrWhiteSpace(data.Isbn))
            {
                return new BadRequestObjectResult("ISBN���w�肳��Ă��܂���B");
            }

            // TODO: Implement when Experience2Notion library is available
            // var book = await _googleBookSeacher.SearchByIsbnAsync(data.Isbn);
            // var (imageData, mime) = await _googleImageSearcher.DownloadImageAsync($"{book.Title} {string.Join(' ', book.Authors)}");
            // var imageId = await _notionClient.UploadImageAsync($"{book.Title}.jpg", imageData, mime);
            // var result = await _notionClient.CreateBookPageAsync(book.Title, book.Authors, book.CanonicalVolumeLink, book.PublishedDate, imageId);
            
            var result = new { message = "Book data received successfully", isbn = data.Isbn };
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing book page creation request");
            return new BadRequestObjectResult("An error occurred while processing the request.");
        }
    }
}