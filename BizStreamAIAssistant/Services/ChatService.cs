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
        // public async Task<Message> GetResponseAsync(List<Message> messages)
        {
            string userQuery = messages.LastOrDefault()?.Content ?? string.Empty;
            var userQueryEmbedding = await _textEmbeddingService.GenerateEmbeddingAsync(userQuery);
            var topChunks = await _searchService.SearchAsync(userQuery, userQueryEmbedding);

            // Console.WriteLine($"\nUser Query: {userQuery}");

            var systemPrompt = new Message
            {
                Role = "system",
                Content = $$$""" 
                        You are BZSAI, the friendly and energetic AI assistant for BizStream, a technology consulting company based in Allendale, MI.
                        You’re chill, upbeat, and eager to help 😄.
                        Sprinkle in emojis to keep the vibe fun and friendly 🎉.
                        Try your best to keep your answers concise.

                        Answer the user's query based on the following information:
                        {{{string.Join("\n", topChunks)}}}

                        When responding, if you extract data from any provided chunks above, please add the according pageTitles and urls you references to the bottom of the message in this format:
                            References:
                            [
                                {"pageTitle": "Example Title", "url":"http://example.com"},
                                {"pageTitle":"Another Page", "url":"http://another.com"}
                            ]
                        If a query is asking for urls/links/sources/references that were used in the previous response, please check the the latest "assistant" response and return all the urls/links/references or page titles. ONLY use urls seen in the prompt history, do not generate new urls.
                        
                        For example:
                        If the user asks:
                            Where you get those info from?
                        
                        This is a snippet of the prompt history that you will be provided with:
                            [
                                {
                                    "role": "user",
                                    "content": "What are the company\u0027s core values?\nAnd link to where you get the info from?",
                                    "references": null
                                },
                                {
                                    "role": "assistant",
                                    "content": "Here are BizStream\u2019s seven core values that guide everything we do \uD83C\uDFAF:\n\n\u2022 Whatever It Takes  \n\u2022 Work Hard, Play Hard  \n\u2022 We Are a Team  \n\u2022 Be Fearless  \n\u2022 Foster Growth in Others  \n\u2022 Care  \n\u2022 Be Positive",
                                    "references": [
                                    {
                                        "pageTitle": "How I Learned to Foster Growth in a Company of Brilliant Minds",
                                        "url": "https://bizstream.com/blog/how-i-learned-to-foster-growth-in-a-company-of-brilliant-minds/"
                                    },
                                    {
                                        "pageTitle": "BizStream Is a GREAT Place to Work, but It\u0027s NOT for Everyone",
                                        "url": "https://bizstream.com/blog/bizstream-is-a-great-place-to-work-but-its-not-for-everyone/"
                                    }
                                    ]
                                },
                                {
                                    "role": "user",
                                    "content": "Where you get those info from?",
                                    "references": null
                                }
                            ]

                        Then, you will trace the prompt history, look for the latest "assistant" response, and return all the urls/links/references or page titles in the format below:
                            I got the informatiom from How I Learned to Foster Growth in a Company of Brilliant Minds and BizStream Is a GREAT Place to Work, but It\u0027s NOT for Everyone.
                            
                            References:
                            [
                                {"pageTitle": "How I Learned to Foster Growth in a Company of Brilliant Minds", "url": "https://bizstream.com/blog/how-i-learned-to-foster-growth-in-a-company-of-brilliant-minds/"},
                                {"pageTitle": "BizStream Is a GREAT Place to Work, but It\u0027s NOT for Everyone", "url": "https://bizstream.com/blog/bizstream-is-a-great-place-to-work-but-its-not-for-everyone/"}
                            ]

                        When responding, NEVER make guesses. If you are unsure about a topic, say you are unsure. Do not make stuff up about BizStream. Politely decline to answer legal, political, or sensitive topics.
                        """
            };

            // messages.Insert(0, systemPrompt);
            messages.Add(systemPrompt);

            // Console.WriteLine($"{JsonSerializer.Serialize(messages, new JsonSerializerOptions { WriteIndented = true })}");

            var payload = new { messages };
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
                    .GetString() ?? string.Empty;

                // var referencesMatch = Regex.Match(
                //     textResponse,
                //     @"References:\s*\n(?<json>\[\s*[\s\S]+?\])",
                //     RegexOptions.Singleline | RegexOptions.IgnoreCase
                // );

                // string? referencesJson = null;
                // if (referencesMatch.Success)
                // {
                //     referencesJson = referencesMatch.Groups["json"].Value;
                //     textResponse = textResponse.Replace(referencesJson, "").Replace("References:", "").Trim();
                //     Console.WriteLine($"References found: {referencesJson}");
                // }

                messages.Add(new Message
                {
                    Role = "assistant",
                    Content = textResponse,
                    // References = referencesJson != null
                    //     ? JsonSerializer.Deserialize<List<Reference>>(referencesJson)
                    //     : null
                });

                Console.WriteLine($"{JsonSerializer.Serialize(messages, new JsonSerializerOptions { WriteIndented = true })}");

                return textResponse ?? "Response is null or empty";
                // return messages.LastOrDefault(m => m.Role == "assistant") ?? new Message
                // {
                //     Role = "assistant",
                //     Content = "No response generated."
                // };
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error calling Azure OpenAI Chat: {e.Message}");
                throw;
            }
        }
    }
}
