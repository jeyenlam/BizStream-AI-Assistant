public class AzureOpenAISettingsModel
{
    public required string ApiKey { get; set; }
    public required string Endpoint { get; set; }
    public required string DeploymentName { get; set; }
    public required string Model { get; set; }
    public required string ApiVersion { get; set; }
    public required string ResourceName { get; set; }
    public required string MaxTokens { get; set; }
}