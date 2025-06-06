public class AzureAISearchSettingsModel
{
    public required string ApiKey { get; set; }
    public required string Endpoint { get; set; }
    public required string IndexName { get; set; }
    public required string SemanticConfigurationName { get; set; }
}