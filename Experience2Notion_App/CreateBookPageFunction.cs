using Experience2Notion.Services;
using Experience2Notion_App.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Experience2Notion_App;

public class CreateBookPageFunction(ILogger<CreateBookPageFunction> logger)
{
    private readonly ILogger<CreateBookPageFunction> _logger = logger;

    [Function("CreateBookPage")]
    public async Task< IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        _logger.LogInformation(requestBody);
        var data = JsonSerializer.Deserialize<CreateBookRequest>(requestBody);
        if (data is null || string.IsNullOrWhiteSpace(data.Isbn))
        {
            return new BadRequestObjectResult("ISBN���w�肳��Ă��܂���B");
        }

        var service = new GoogleBookSeacher();
        var book = await service.SearchByIsbnAsync(data.Isbn);

        Console.WriteLine($"�^�C�g��: {book.Title}");
        Console.WriteLine($"����: {string.Join(", ", book.Authors)}");
        Console.WriteLine($"�o�œ�: {book.PublishedDate}");

        var searcher = new GoogleImageSearcher();
        var (imageData, mime) = await searcher.DownloadImageAsync($"{book.Title} {string.Join(' ', book.Authors)}");

        var notionClient = new NotionClient();
        var imageId = await notionClient.UploadImageAsync($"{book.Title}.jpg", imageData, mime);
        var result = await notionClient.CreateBookPageAsync(book.Title, book.Authors, book.CanonicalVolumeLink, book.PublishedDate, imageId);
        return new OkObjectResult(result);
    }
}