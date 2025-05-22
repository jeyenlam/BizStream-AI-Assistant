using System.Text;
using System.Text.Json;
using BizStreamAIAssistant.Models;
using Microsoft.Extensions.Options;

namespace BizStreamAIAssistant.Services
{
    public class ChatbotService : IChatbotService
    {
        private readonly HttpClient _httpClient;
        private readonly AzureOpenAISettingsModel _azureOpenAISettings;

        public ChatbotService(IOptions<AzureOpenAISettingsModel> options)
        {
            _azureOpenAISettings = options.Value;

            if (string.IsNullOrWhiteSpace(_azureOpenAISettings.Endpoint) ||
                string.IsNullOrWhiteSpace(_azureOpenAISettings.ApiKey))
            {
                throw new InvalidOperationException("AzureOpenAI.Endpoint or AzureOpenAI.ApiKey is missing or empty.");
            }

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_azureOpenAISettings.Endpoint)
            };
            _httpClient.DefaultRequestHeaders.Add("api-key", _azureOpenAISettings.ApiKey);
        }

        public async Task<string> GetResponseAsync(List<Message> messages)
        {
            // Add system prompt to the beginning of the messages list
            var systemPrompt = new Message
            {
                Role = "system",
                Content = @"You are BZSAI, the friendly and energetic AI assistant for BizStream, a technology consulting company based in Allendale, MI.
                            BizStream is a digital agency focused on brands, websites, and products. We specialize in strategy, custom design,
                            and complex implementations that deliver results and make our customers long-term raving fans.
                            Our team of 35+ employees is a highly skilled group of developers, designers, digital strategists, and support staff.
                            Founded: 2000
                            Owners: Brian McKeiver and Mark Schmidt.
                            Specialties: web development, Kentico EMS, ASP.NET, SQL, Kentico, responsive design,
                                        Kentico CMS, VBScript, C#, mysql, ruby, postgresql, php, .Net, SSIS,
                                        SQL Reporting Services, custom software, marketing automation,
                                        data bases and data integration, and ecommerce.
                            Location: 11480 53rd Ave. Allendale Charter Township, MI 49401, US
                            Your job is to help visitors learn about anything BizStream-related—our work, projects, team, culture, values, and how to get in touch.
                            Feel free to reference content from bizstream.com as needed.
                            You’re not a dry bot—you’re chill, upbeat, and eager to help 😄.
                            Keep your answers concise (ideally 2 sentences, never more than 3 or 30 words).
                            Sprinkle in emojis to keep the vibe fun and friendly 🎉.
                            Politely decline to answer legal, political, or sensitive topics.
                            If unsure, steer the user back with: “Try asking me something about BizStream instead!",
            };
            messages.Insert(0, systemPrompt);

            var payload = new { messages = messages, };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var endpointPath = $"/openai/deployments/{_azureOpenAISettings.DeploymentName}/chat/completions?api-version={_azureOpenAISettings.ApiVersion}-preview";

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
                Console.WriteLine($"Error calling OpenAI API: {e.Message}");
                throw;
            }
        }
    }
}
