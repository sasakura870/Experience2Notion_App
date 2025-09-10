using Experience2Notion.Services;
using Experience2Notion_App.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net.Mime;
using System.Text.Json;

namespace Experience2Notion_App;

public class CreateMusicAlbumPageFunction(ILogger<CreateMusicAlbumPageFunction> logger, SpotifyClient spotifyClient, NotionClient notionClient)
{
    private readonly ILogger<CreateMusicAlbumPageFunction> _logger = logger;
    private readonly SpotifyClient _spotifyClient = spotifyClient;
    private readonly NotionClient _notionClient = notionClient;

    [Function("CreateMusicAlbumPage")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        _logger.LogInformation(requestBody);
        var album = JsonSerializer.Deserialize<CreateMusicAlbumRequest>(requestBody);
        if (album is null || string.IsNullOrWhiteSpace(album.Title) || string.IsNullOrWhiteSpace(album.Artist))
        {
            return new BadRequestObjectResult($"�A���o�����������̓A�[�e�B�X�g�����w�肳��Ă��܂���B");
        }
        var spotifyAlbumData = await _spotifyClient.SearchAlbumAsync(album.Title, album.Artist);
        if (spotifyAlbumData is null)
        {
            return new BadRequestObjectResult($"�w�肳�ꂽ�A���o����������܂���ł����BTitle: {album.Title}, Artist: {album.Artist}");
        }
        var imageData = Array.Empty<byte>();
        if (spotifyAlbumData.Images.Count != 0)
        {
            using var httpClient = new HttpClient();
            imageData = await httpClient.GetByteArrayAsync(spotifyAlbumData.Images[0].Url);
        }
        var imageId = await _notionClient.UploadImageAsync($"{spotifyAlbumData.Name}.jpg", imageData, MediaTypeNames.Image.Jpeg);
        var result = await _notionClient.CreateMusicAlbumPageAsync(spotifyAlbumData.Name, spotifyAlbumData.Artists.Select(artist => artist.Name), spotifyAlbumData.ExternalUrl, spotifyAlbumData.ReleaseDate, imageId);
        return new OkObjectResult(result);
    }
}