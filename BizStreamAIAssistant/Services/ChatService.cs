using System.Text;
using System.Text.Json;
using BizStreamAIAssistant.Models;
using Microsoft.Extensions.Options;
using BizStreamAIAssistant.Services;

namespace BizStreamAIAssistant.Services
{
    public class ChatService
    {
        private readonly HttpClient _httpClient;
        private readonly AzureOpenAISettingsModel _azureOpenAIChatSettings;
        private readonly TextEmbeddingService _textEmbeddingService;
        private readonly SearchService _searchService;

        public ChatService(
            IOptionsMonitor<AzureOpenAISettingsModel> options,
            TextEmbeddingService textEmbeddingService,
            SearchService searchService)
        {
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
            _textEmbeddingService = textEmbeddingService ?? throw new ArgumentNullException(nameof(textEmbeddingService));
            if (options == null) throw new ArgumentNullException(nameof(options));
            {
                _azureOpenAIChatSettings = options.Get("Chat");

                if (string.IsNullOrWhiteSpace(_azureOpenAIChatSettings.Endpoint) ||
                    string.IsNullOrWhiteSpace(_azureOpenAIChatSettings.ApiKey))
                {
                    throw new InvalidOperationException("AzureOpenAI.Endpoint or AzureOpenAI.ApiKey is missing or empty.");
                }

                _httpClient = new HttpClient
                {
                    BaseAddress = new Uri(_azureOpenAIChatSettings.Endpoint)
                };
                _httpClient.DefaultRequestHeaders.Add("api-key", _azureOpenAIChatSettings.ApiKey);
            }
        }

        public async Task<string> GetResponseTestAsync(List<Message> messages)
        {
            string userQuery = messages.LastOrDefault()?.Content ?? string.Empty;
            // Console.WriteLine($"User Query: {userQuery}");
            var userQueryEmbedding = await _textEmbeddingService.GenerateEmbeddingAsync(userQuery);
            var topChunks = await _searchService.SearchAsync(userQuery, userQueryEmbedding);
            // Console.WriteLine($"Top Chunks: {string.Join("\n---\n", topChunks)}");

            // Add system prompt to the beginning of the messages list
            var systemPrompt = new Message
            {
                Role = "system",
                Content = $""" 
                        You are BZSAI, the friendly and energetic AI assistant for BizStream, a technology consulting company based in Allendale, MI.
                        You’re not a dry bot—you’re chill, upbeat, and eager to help 😄.
                        Keep your answers concise (ideally 3 sentences).
                        Sprinkle in emojis to keep the vibe fun and friendly 🎉.
                        Politely decline to answer legal, political, or sensitive topics.
                        If unsure, ask for more context.

                        Please answer the user's query based on the following context:
                        {string.Join("\n", topChunks)}
                        """
            };
            Console.WriteLine($"Top chunks: \n{string.Join("\n", topChunks)}");
            messages.Insert(0, systemPrompt);

            var payload = new { messages = messages, };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var endpointPath = $"/openai/deployments/{_azureOpenAIChatSettings.DeploymentName}/chat/completions?api-version={_azureOpenAIChatSettings.ApiVersion}-preview";

            try
            {
                var response = await _httpClient.PostAsync(endpointPath, content);
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var jsonDocument = JsonDocument.Parse(jsonResponse);
                var textResponse = jsonDocument.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return textResponse ?? "Response is null or empty";
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error calling Azure OpenAI Chat: {e.Message}");
                throw;
            }
        }
    }
}
