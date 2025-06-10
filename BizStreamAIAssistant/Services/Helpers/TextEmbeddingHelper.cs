using System.Text.Json;

namespace BizStreamAIAssistant.Services.Helpers
{
    public class TextEmbeddingHelper
    {
        public static string ExtractTextFromJsonLine(string jsonLine)
        {
            var json = JsonDocument.Parse(jsonLine);
            return json.RootElement.GetProperty("text").GetString()!;
        }
        
        public static int CalculateBackoffDelay(int attempt)
        {
            // Base delay of 2 seconds
            const int baseDelay = 2000;

            // Add jitter to avoid thundering herd problem
            var random = new Random();
            var jitter = random.Next(-500, 500);

            // Calculate delay with exponential backoff: baseDelay * 2^(attempt-1)
            // Cap maximum delay at 2 minutes
            var delay = Math.Min(baseDelay * Math.Pow(2, attempt - 1) + jitter, 120000);

            return (int)delay;
        }
    }
}