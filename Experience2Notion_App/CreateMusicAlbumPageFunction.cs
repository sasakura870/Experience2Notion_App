using Experience2Notion.Services;
using Experience2Notion_App.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net.Mime;
using System.Text.Json;

namespace Experience2Notion_App;

public class CreateMusicAlbumPageFunction(ILogger<CreateMusicAlbumPageFunction> logger, MusicAlbumSearchClient musicAlbumSearchClient, NotionClient notionClient)
{
    private readonly ILogger<CreateMusicAlbumPageFunction> _logger = logger;
    private readonly MusicAlbumSearchClient _musicAlbumSearchClient = musicAlbumSearchClient;
    private readonly NotionClient _notionClient = notionClient;

    [Function("CreateMusicAlbumPage")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        _logger.LogInformation(requestBody);
        var album = JsonSerializer.Deserialize<CreateMusicAlbumRequest>(requestBody);
        if (album is null || string.IsNullOrWhiteSpace(album.Title) || string.IsNullOrWhiteSpace(album.Artist))
        {
            return new BadRequestObjectResult($"アルバム名もしくはアーティスト名が指定されていません。");
        }
        var albumData = await _musicAlbumSearchClient.SearchAlbumAsync(album.Title, album.Artist);
        if (albumData is null)
        {
            return new BadRequestObjectResult($"指定されたアルバムが見つかりませんでした。Title: {album.Title}, Artist: {album.Artist}");
        }
        var imageData = Array.Empty<byte>();
        if (albumData.Images.Count != 0)
        {
            using var httpClient = new HttpClient();
            imageData = await httpClient.GetByteArrayAsync(albumData.Images[0].Url);
        }
        var imageId = await _notionClient.UploadImageAsync($"{albumData.Name}.jpg", imageData, MediaTypeNames.Image.Jpeg);
        var result = await _notionClient.CreateMusicAlbumPageAsync(albumData.Name, albumData.Artists.Select(artist => artist.Name), albumData.ExternalUrl, albumData.ReleaseDate, imageId);
        return new OkObjectResult(result);
    }
}