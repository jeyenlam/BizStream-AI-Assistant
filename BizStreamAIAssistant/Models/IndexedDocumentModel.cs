using System.Text.Json.Serialization;

public class IndexedDocumentModel
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    [JsonPropertyName("text")]
    public required string Text { get; set; }
    [JsonPropertyName("pageTitle")]
    public required string PageTitle { get; set; }
    [JsonPropertyName("url")]
    public required string Url { get; set; }
    [JsonPropertyName("embedding")]
    public required float[] Embedding { get; set; }
}