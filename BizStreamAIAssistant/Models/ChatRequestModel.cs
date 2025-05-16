using System.Text.Json.Serialization;

namespace BizStreamAIAssistant.Models
{
    public class Message
    {
        [JsonPropertyName("role")]
        public required string Role { get; set; }   // "user", "assistant", "system"
        [JsonPropertyName("content")]
        public required string Content { get; set; }
    }
    public class ChatRequestModel
        {
            public required List<Message> Messages { get; set; }
        }
    
}
