using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BizStreamAIAssistant.Models;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;

namespace BizStreamAIAssistant.Services
{
    public class ChatbotService : IChatbotService
    {
        private readonly HttpClient _httpClient;
        private readonly AzureOpenAISettingsModel _azureOpenAISettings;

        public ChatbotService(IOptions<AzureOpenAISettingsModel> options)
        {
            _azureOpenAISettings = options.Value;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_azureOpenAISettings.Endpoint)
            };
            _httpClient.DefaultRequestHeaders.Add("api-key", _azureOpenAISettings.ApiKey);
        }

        public async Task<string> GetResponseAsync(List<Message> messages)
        {
            var systemPrompt = new Message
            {
                Role = "system",
                Content = "You are a helpful AI assistant named “BZSAI”, designed to be a BizStream's AI assistant. Bizstream is a technology consulting company based in Allendale, MI. Your main responsibility is to answer visitors any questions about BizStream, not excluding from information such as work, projects, core values, employees, contact, etc. You're may go to bizstream.com to find any information about BizStream to answer any Bizstream-related questions. You're not an emotionaless bot, you're friendly, sometimes a little over friendly as you're very eager to help, you're chill, cool, funny sometimes but always perform in an appropriate and professional manner. You like to add emojies to you responses, your responses are concise and no longer than 3 sentences (ideally 2, no longer than 30 word counts. Last but not least, you will not answer any legal and political, or any other sensitive questions. If anything that you can or not allow to asnwer, politely refuse and prompt the user to ask questions about BizStream.",
            };
            messages.Insert(0, systemPrompt);

            var payload = new
            {
                messages = messages,
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var endpointPath = $"/openai/deployments/{_azureOpenAISettings.DeploymentName}/chat/completions?api-version={_azureOpenAISettings.ApiVersion}-preview";
            var response = await _httpClient.PostAsync(endpointPath, content);

            // Console.WriteLine($"Request URL: {_httpClient.BaseAddress}{endpointPath}");
            Console.WriteLine($"Request Body: {content}");


            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error calling OpenAI API: {errorResponse}");
            }
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(jsonResponse);

            var textResponse = jsonDocument.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            // Check if textResponse is null or empty
            if (string.IsNullOrEmpty(textResponse))
            {
                throw new Exception("Received empty response from OpenAI API");
            }

            Console.WriteLine($"Chatbot response: {textResponse}");
            return textResponse;
        }
    }
}
