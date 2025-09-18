using System.Text.Json.Serialization;

namespace Experience2Notion_App.Models;
public class CreateRestaurantRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("visit_date")]
    public DateTime VisitDate { get; set; }

    [JsonPropertyName("photos")]
    public string[] Photos { get; set; } = [];
}