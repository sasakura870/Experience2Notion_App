using System.Text.Json.Serialization;

namespace Experience2Notion_App.Models;
public class CreateBookRequest
{
    [JsonPropertyName("isbn")]
    public string Isbn { get; set; } = string.Empty;
}
