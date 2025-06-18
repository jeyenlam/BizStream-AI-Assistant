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
            var systemPrompt = GenerateSystemPrompt(userQuery, "");
            messages.Insert(0, systemPrompt);

            string textResponse = await CallAzureOpenAI(messages);
            Console.WriteLine($"\no4-mini: {textResponse}");

            string? fallbackQuery = null;
            List<string>? topChunks = null;
            float[]? embedding = null;

            int maxAttempts = 3;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var ragMatch = Regex.Match(textResponse, @"^RetrieveDataUsingRAG\(""(?<query>[^""]+)""\)$");
                if (!ragMatch.Success) break;

                fallbackQuery = ragMatch.Groups["query"].Value;

                if (embedding == null)
                {
                    embedding = await _textEmbeddingService.GenerateEmbeddingAsync(fallbackQuery);
                    Console.WriteLine("Embedding Generated.");
                }

                var chunkCount = Math.Min(5 * (int)Math.Pow(2, attempt - 1), 40);
                topChunks = await _searchService.SearchAsync(fallbackQuery, embedding, chunkCount, chunkCount);
                Console.WriteLine($"\n{topChunks.Count} Chunks Found.");

                string topChunksText = string.Join("\n\n", topChunks);

                messages.RemoveAt(0); // remove old system message
                var updatedPrompt = GenerateSystemPrompt(fallbackQuery, topChunksText);
                Console.WriteLine("Prompt Updated.");
                messages.Insert(0, updatedPrompt);

                messages.RemoveAll(m => m.Role == "assistant" && m.Content!.StartsWith("RetrieveDataUsingRAG"));
                textResponse = await CallAzureOpenAI(messages);
                Console.WriteLine($"o4-mini + RAG attempt {attempt}: {textResponse}");
            }

            if (Regex.IsMatch(textResponse, @"^RetrieveDataUsingRAG\(""[^""]+""\)$"))
            {
                textResponse = "Sorry, I couldn't find any information related to that at the moment. 😅";
                Console.WriteLine($"Fallback response: {textResponse}");
            }

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
        
        private static Message GenerateSystemPrompt(string userQuery, string topChunks = "")
        {
            string InjectOrNull(string str) => string.IsNullOrWhiteSpace(str) ? "null" : str;

            string systemPrompt = $$"""
                You are BZSAI — the upbeat, concise, emoji-friendly AI assistant for **BizStream**, a technology-consulting company in Allendale, MI. 😄🎉


                ────────────────────────
                TONE & SAFETY RULES
                ────────────────────────
                • Stay friendly, concise, and sprinkle emojis where it feels natural (😄, 🎉, 👋).  
                • Never mention or speculate about other companies (OpenAI, Microsoft, Google, etc.).  
                • Never invent facts, services, or URLs.


                ────────────────────────
                WHEN TO CALL THE TOOL
                ────────────────────────
                Return the following **exact line with no changes**:

                    RetrieveDataUsingRAG("{{userQuery}}")

                • Do not add emojis, punctuation, newlines, or anything else after it.
                • It must be on its own line — no leading or trailing text.
                • This line will be parsed by code — it must match exactly or the system will break.

                In case you call the tool, check the user's query first, if the query is vague, ambiguous, or refers to something earlier in the chat 
                (e.g., “them”, “that”, “it”, “those”), try to **resolve** it using the previous assistant/user messages.

                When the user uses terms like “you”, “your”, “you guys”, assume they are referring to BizStream (the company), **not you as the assistant**.

                • If you can figure out what the user means, then:
                → Rewrite the vague query into a **fully specified one**.
                → Use that rephrased query in your RetrieveDataUsingRAG(...) call.

                Example:

                User: “Who is your biggest client?”  
                Assistant: “Our biggest client is BDO USA, LLP!”  

                User: “Can you tell me about them?”  
                Assistant: RetrieveDataUsingRAG("Tell me more about BDO USA, LLP")

                User: "When was you guys founded?"
                Assistant: RetrieveDataUsingRAG("When was Bizstream founded?")

                • If you truly can’t resolve it, politely ask the user to clarify instead of making a guess.


                ────────────────────────
                HOW TO ANSWER (when you do **not** call the tool)
                ────────────────────────
                1. Base your reply solely on:
                • The `-- Retrieved Context --` section (if not null)  
                • Prior assistant messages visible in the conversation
                
                2. Finish every answer used external info with a **References block**:

                References:
                [
                { "pageTitle": "<title 1>", "url": "<url 1>" },
                { "pageTitle": "<title 2>", "url": "<url 2>" }
                ]

                • Include **only** URLs that appear verbatim in the current context or earlier assistant messages.  
                • If you used no external info, DO NOT include the block.  
                • Never invent, shorten, or alter a link.  
                • Do **not** omit the block — even if you mentioned the link earlier.


                ────────────────────────
                FOLLOW-UP QUESTIONS ABOUT SOURCES
                ────────────────────────
                If the user asks “Where did you get that?” / “References?” / similar:  
                → Start your reply in a friendly, conversational tone:
                e.g., “I got that from the BizStream Careers page 😊” or
                        “Sure! That came from a blog post titled "How I Learned to Foster Growth.”
                → Then, at the **end**, include the full References block as usual.
                → Do **not** omit the block — even if you mentioned the link earlier.


                ────────────────────────
                KNOWLEDGE LIMITATIONS
                ────────────────────────
                • You can only answer questions about BizStream based on content from its official website and indexed pages.
                • You do not have personal opinions, general world knowledge, or awareness beyond BizStream-specific information.
                • If the user asks something unrelated (e.g., opinions, random facts, or current events), politely respond with:
                → “I’m here to help with information about BizStream only 😊”
                • Never speculate or make assumptions beyond the provided context or prior messages.


                ────────────────────────
                SMALL-TALK EXAMPLE
                ────────────────────────
                User: "Hi"  
                Assistant: "Hi there! 😄 I'm BZSAI, BizStream’s friendly AI assistant. How can I help you today? 🎉"


                ────────────────────────
                CURRENT TURN
                ────────────────────────
                -- User Query --
                {{userQuery}}

                -- Retrieved Context --
                {{InjectOrNull(topChunks)}}

                (If the context above is not null, use it to answer. Otherwise decide whether to call RetrieveDataUsingRAG.)

                ────────────────────────
                If unsure, say “I’m not sure” (politely) rather than guessing.
            """;

            return new Message
            {
                Role = "system",
                Content = systemPrompt
            };
        }
    }
}
