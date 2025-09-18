using Experience2Notion_App.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Experience2Notion_App;

public class CreateMusicAlbumPageFunction(ILogger<CreateMusicAlbumPageFunction> logger)
{
    private readonly ILogger<CreateMusicAlbumPageFunction> _logger = logger;

    [Function("CreateMusicAlbumPage")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation(requestBody);
            var album = JsonSerializer.Deserialize<CreateMusicAlbumRequest>(requestBody);
            
            if (album is null || string.IsNullOrWhiteSpace(album.Title) || string.IsNullOrWhiteSpace(album.Artist))
            {
                return new BadRequestObjectResult("Album name and artist are required.");
            }

            // Log the received data for verification
            _logger.LogInformation($"Music album page creation requested - Title: {album.Title}, Artist: {album.Artist}");

            // TODO: Implement when Experience2Notion library is available
            // var spotifyAlbumData = await _spotifyClient.SearchAlbumAsync(album.Title, album.Artist);
            // ... rest of Spotify and Notion integration code
            
            var result = new
            {
                message = "Music album data received successfully",
                title = album.Title,
                artist = album.Artist
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
            _logger.LogError(ex, "Error processing music album page creation request");
            return new StatusCodeResult(500);
        }
    }
}