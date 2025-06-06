using System.Text.Json.Serialization;

public class ChunkModel
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    [JsonPropertyName("text")]
    public required string Text { get; set; }
    [JsonPropertyName("pageTitle")]
    public required string PageTitle { get; set; }
    [JsonPropertyName("url")]
    public required string Url { get; set; }
}