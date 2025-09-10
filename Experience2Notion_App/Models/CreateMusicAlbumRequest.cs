using System.Text.Json.Serialization;

namespace Experience2Notion_App.Models;
internal class CreateMusicAlbumRequest
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("artist")]
    public string Artist { get; set; } = string.Empty;
}
