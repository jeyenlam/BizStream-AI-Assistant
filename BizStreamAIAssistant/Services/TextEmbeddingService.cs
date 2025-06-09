using Azure;
using Azure.Search.Documents;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Azure.Search.Documents.Models;
using BizStreamAIAssistant.Services.Helpers;
using System.Text;
using BizStreamAIAssistant.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace BizStreamAIAssistant.Services
{
    public class TextEmbeddingService
    {
        private readonly AzureAISearchSettingsModel _azureAISearchSettings;
        private readonly SearchClient _searchClient;
        private readonly AzureOpenAISettingsModel _azureOpenAITextEmbeddingSettings;
        private readonly HttpClient _httpClient;
        private const int BatchSize = 5;

        public TextEmbeddingService(
            IOptions<AzureAISearchSettingsModel> searchOptions,
            IOptionsMonitor<AzureOpenAISettingsModel> textEmbeddingOptions)
        {
            _azureAISearchSettings = searchOptions.Value;
            if (string.IsNullOrWhiteSpace(_azureAISearchSettings.Endpoint) ||
                string.IsNullOrWhiteSpace(_azureAISearchSettings.ApiKey))
            {
                throw new InvalidOperationException("AzureAISearchSettings.Endpoint or AzureAISearchSettings.ApiKey is missing or empty.");
            }

            _searchClient = new SearchClient(
                new Uri(_azureAISearchSettings.Endpoint),
                _azureAISearchSettings.IndexName,
                new AzureKeyCredential(_azureAISearchSettings.ApiKey));

            _azureOpenAITextEmbeddingSettings = textEmbeddingOptions.Get("TextEmbedding");
            if (string.IsNullOrWhiteSpace(_azureOpenAITextEmbeddingSettings.Endpoint) ||
                string.IsNullOrWhiteSpace(_azureOpenAITextEmbeddingSettings.ApiKey))
            {
                throw new InvalidOperationException("AzureOpenAITextEmbeddings.Endpoint or AzureOpenAITextEmbeddings.ApiKey is missing or empty.");
            }

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_azureOpenAITextEmbeddingSettings.Endpoint)
            };
            _httpClient.DefaultRequestHeaders.Add("api-key", _azureOpenAITextEmbeddingSettings.ApiKey);
        }

        public async Task UploadEmbeddingsAsync()
        {
            string jsonlFilePath = TempDataPathConfig.JsonlFilePath;
            Console.WriteLine($"[UploadEmbeddingsAsync] jsonlFilePath: {jsonlFilePath}");

            var lines = await File.ReadAllLinesAsync(jsonlFilePath);
            var vectors = await this.GenerateEmbeddingsAsync(jsonlFilePath);

            const int batchSize = 1;
            int total = vectors.Count;

            for (int i = 0; i < total; i += batchSize)
            {
                var batchDocuments = new List<IndexedDocumentModel>();

                for (int j = i; j < i + batchSize && j < total; j++)
                {
                    var chunk = JsonSerializer.Deserialize<ChunkModel>(lines[j]);
                    var document = new IndexedDocumentModel
                    {
                        Id = chunk!.Id.ToString(),
                        Text = chunk.Text,
                        PageTitle = chunk.PageTitle,
                        Url = chunk.Url,
                        Embedding = vectors[j],
                    };
                    batchDocuments.Add(document);
                }

                try
                {
                    var batch = IndexDocumentsBatch.Upload(batchDocuments);
                    await _searchClient.IndexDocumentsAsync(batch);
                    Console.WriteLine($"Uploaded batch {i / batchSize + 1} of {Math.Ceiling((double)total / batchSize)}: {batchDocuments.Count} documents.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to upload batch {i / batchSize + 1}: {ex.Message}");
                }
            }

            Console.WriteLine("✅ All batches processed.");
        }

        public async Task<List<float[]>> GenerateEmbeddingsAsync(string jsonlFilePath)
        {
            var vectors = new List<float[]>();
            var lines = await File.ReadAllLinesAsync(jsonlFilePath);
            var inputs = lines.Select(TextEmbeddingHelper.ExtractTextFromJsonLine).ToList();
            var totalBatches = (int)Math.Ceiling(inputs.Count / (double)BatchSize);

            for (int i = 0; i < inputs.Count; i += BatchSize)
            {
                var batch = inputs.Skip(i).Take(BatchSize).ToList();
                Console.WriteLine($"Processing batch {(i / BatchSize) + 1} of {totalBatches} ({batch.Count} items)...");

                var payload = new
                {
                    input = batch,
                    model = _azureOpenAITextEmbeddingSettings.Model
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var url = $"/openai/deployments/{_azureOpenAITextEmbeddingSettings.DeploymentName}/embeddings?api-version={_azureOpenAITextEmbeddingSettings.ApiVersion}";

                var response = await PostWithRetryAsync(content, url);
                var json = await response.Content.ReadAsStringAsync();
                var root = JsonDocument.Parse(json).RootElement;

                var batchVectors = root.GetProperty("data")
                    .EnumerateArray()
                    .Select(d => d.GetProperty("embedding").EnumerateArray().Select(e => e.GetSingle()).ToArray())
                    .ToList();

                vectors.AddRange(batchVectors);
            }

            return vectors;
        }

        public async Task<float[]> GenerateEmbeddingAsync(string input)
        {
            var payload = new
            {
                input = input,
                model = _azureOpenAITextEmbeddingSettings.Model
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var url = $"/openai/deployments/{_azureOpenAITextEmbeddingSettings.DeploymentName}/embeddings?api-version={_azureOpenAITextEmbeddingSettings.ApiVersion}";

            var response = await PostWithRetryAsync(content, url);
            var json = await response.Content.ReadAsStringAsync();
            var root = JsonDocument.Parse(json).RootElement;

            return root.GetProperty("data")
                .EnumerateArray()
                .Select(d => d.GetProperty("embedding").EnumerateArray().Select(e => e.GetSingle()).ToArray())
                .FirstOrDefault() ?? throw new InvalidOperationException("No embedding found in the response.");
        }

        private async Task<HttpResponseMessage> PostWithRetryAsync(HttpContent content, string url, int maxRetries = 10)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try 
                {
                    var response = await _httpClient.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                        return response;

                    if ((int)response.StatusCode == 429)
                    {
                        var delay = CalculateBackoffDelay(attempt);
                        Console.WriteLine($"⚠️ Rate limited (429). Retry {attempt}/{maxRetries} in {delay}ms...");
                        await Task.Delay(delay);
                        continue;
                    }

                    // For other error status codes, get the error message
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Request failed with status {response.StatusCode}: {errorContent}");
                    response.EnsureSuccessStatusCode(); // fail fast on other errors
                }
                catch (HttpRequestException ex)
                {
                    if (attempt == maxRetries)
                        throw new HttpRequestException($"❌ Failed after {maxRetries} retries. Last error: {ex.Message}", ex);
                    
                    var delay = CalculateBackoffDelay(attempt);
                    Console.WriteLine($"⚠️ Request failed. Retry {attempt}/{maxRetries} in {delay}ms. Error: {ex.Message}");
                    await Task.Delay(delay);
                }
            }

            throw new HttpRequestException($"❌ Exceeded retry limit of {maxRetries} attempts.");
        }

        private static int CalculateBackoffDelay(int attempt)
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


