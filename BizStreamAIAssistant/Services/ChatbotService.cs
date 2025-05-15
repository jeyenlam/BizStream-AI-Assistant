using System.Threading.Tasks;

namespace BizStreamAIAssistant.Services
{
    public class ChatbotService : IChatbotService
    {
        public async Task<string> GetResponseAsync(string message)
        {
            // Placeholder for real logic (OpenAI, rules, etc.)
            await Task.Delay(50); // Simulate async processing

            // Example business rule
            if (message.ToLower().Contains("hello"))
                return "Hi there! How can I assist you today?";

            return $"You said: {message}";
        }
    }
}
