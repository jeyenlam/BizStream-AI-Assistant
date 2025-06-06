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
    }
}