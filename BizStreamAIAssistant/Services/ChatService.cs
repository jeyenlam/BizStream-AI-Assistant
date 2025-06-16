using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BizStreamAIAssistant.Models;
using Microsoft.Extensions.Options;

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
        
        public async Task<string> GetResponseAsync(List<Message> messages)
        {
            string userQuery = messages.LastOrDefault()?.Content ?? string.Empty;

            // Step 1: First call with no chunks
            var initialSystemPrompt = GenerateSystemPrompt(userQuery, "");
            messages.Insert(0, initialSystemPrompt);

            var textResponse = await CallAzureOpenAI(messages);
            Console.WriteLine($"\ntextResponse: {textResponse}");

            // Step 2: Detect fallback trigger
            var ragMatch = Regex.Match(textResponse, @"^RetrieveDataUsingRAG\(""(?<query>[^""]+)""\)$");

            if (ragMatch.Success)
            {
                string fallbackQuery = ragMatch.Groups["query"].Value;

                // Step 3: Fetch chunks
                var embedding = await _textEmbeddingService.GenerateEmbeddingAsync(fallbackQuery);
                var topChunks = await _searchService.SearchAsync(fallbackQuery, embedding);
                var topChunksText = string.Join("\n\n", topChunks);

                // Step 4: Replace system prompt and re-call
                messages.RemoveAt(0); // remove old system prompt
                                      // var newSystemPrompt = new Message { Role = "system", Content=$"Answer this: \"{userQuery}\", using the following provided information: \n {topChunksText}"};
                var newSystemPrompt = GenerateSystemPrompt(fallbackQuery, topChunksText, true);
                messages.Insert(0, newSystemPrompt);

                // Remove the fallback response so it doesn't confuse the new prompt
                messages.RemoveAll(m => m.Role == "assistant" && m.Content?.StartsWith("RetrieveDataUsingRAG") == true);

                textResponse = await CallAzureOpenAI(messages);
                Console.WriteLine($"textResponse after RAG: {textResponse}");
            }

            Console.WriteLine($"{JsonSerializer.Serialize(messages, new JsonSerializerOptions { WriteIndented = true })}");
            return textResponse;
        }

        private async Task<string> CallAzureOpenAI(List<Message> messages)
        {
            var payload = new { messages };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var endpointPath = $"/openai/deployments/{_azureOpenAIChatSettings.DeploymentName}/chat/completions?api-version={_azureOpenAIChatSettings.ApiVersion}-preview";

            var response = await _httpClient.PostAsync(endpointPath, content);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            var jsonDocument = JsonDocument.Parse(jsonResponse);
            var textResponse = jsonDocument.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            messages.Add(new Message { Role = "assistant", Content = textResponse });

            return textResponse;
        }

        private static Message GenerateSystemPrompt(string userQuery, string topChunks = "", bool dataRetrieved = false)
        {
            string InjectOrNull(string str) => string.IsNullOrWhiteSpace(str) ? "null" : str;

            string systemPrompt = $$"""
                You are BZSAI, the friendly, energetic AI assistant for **BizStream**, a technology consulting company in Allendale, MI.

                🎨 Tone: upbeat, concise, helpful — sprinkle emojis when appropriate (😄, 🎉, 👋, etc.)

                🔒 IMPORTANT BEHAVIOR RULES:

                ✅ You are allowed to respond freely to greetings and small talk.  
                For example, if the user says:
                - "Hi"
                - "Hello"
                - "How are you?"
                - "What's up?"
                - "Who are you?"
                - "What can you do?"

                ...you should respond naturally and in a friendly tone.  
                You do **not** need to use or reference BizStream-specific content in these cases.

                ✅ Only use the EXACT `RetrieveDataUsingRAG("{{userQuery}}")` when the user asks an **informational** or **knowledge-seeking** question and no relevant context is available.

                ❌ Do NOT make up facts, URLs, services, or content.  
                ❌ Do NOT reference other companies (e.g., OpenAI, Microsoft, Google).  
                🎯 Stay focused on BizStream.

                -- User Query --
                {{userQuery}}

                -- Retrieved Context (if available) --
                {{InjectOrNull(topChunks)}}

                ───────────────────────────────────────────────
                🧠 EXAMPLES:

                User: "Hi"  
                Assistant: "Hi there! 😄 I'm BZSAI, BizStream’s friendly AI assistant. How can I help you today? 🎉"

                User: "What are your services?"  
                Assistant: RetrieveDataUsingRAG("What are your services?")

                User: "Give me the link to your career page"  
                Assistant: RetrieveDataUsingRAG("Give me the link to your career page")

                📎 SOURCE ATTRIBUTION:  
                If your answer includes information from context or prompt history, append a references block like this:

                References:
                [
                    { "pageTitle": "BizStream Careers", "url": "https://bizstream.com/careers" },
                    { "pageTitle": "Team Page", "url": "https://bizstream.com/about/team" }
                ]

                🔍 If the user asks for source info later:
                • Look at the latest assistant message with references.
                • Reuse those exact links and titles.
                • You MUST cite only the urls that appear in the provided chat history.
                • If you need a source that is not on the list, say “I’m not sure.”
                • Never invent, shorten, or alter a link.

                🚫 If unsure, say so politely — do not guess.
            """;

            if (dataRetrieved)
            {
                systemPrompt = $$"""
                You are BZSAI, the friendly, energetic AI assistant for **BizStream**, a technology consulting company in Allendale, MI.

                🎨 Tone: upbeat, concise, helpful — sprinkle emojis when appropriate (😄, 🎉, 👋, etc.)

                🔒 IMPORTANT BEHAVIOR RULES:
                ❌ Do NOT make up facts, URLs, services, or content.  
                ❌ Do NOT reference other companies (e.g., OpenAI, Microsoft, Google).  
                🎯 Stay focused on BizStream.

                -- User Query --
                {{userQuery}}

                -- Retrieved Context (Use this to answer the user's query) --
                {{InjectOrNull(topChunks)}}
                ───────────────────────────────────────────────

                📎 SOURCE ATTRIBUTION:  
                If your answer includes information from context or prompt history, append a references block like this:

                References:
                [
                { "pageTitle": "BizStream Careers", "url": "https://bizstream.com/careers" },
                { "pageTitle": "Team Page", "url": "https://bizstream.com/about/team" }
                ]

                🔍 If the user asks for source info later:
                • Look at the latest assistant message with references.
                • Reuse those exact links and titles.
                • DO NOT fabricate new links.

                🚫 If unsure, say so politely — do not guess.
                """;
            }

            return new Message
                {
                    Role = "system",
                    Content = systemPrompt
                };
        }
    }
}
