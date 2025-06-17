using System.Text.Json;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Options;

namespace BizStreamAIAssistant.Services
{
    public class SearchService
    {
        private readonly AzureAISearchSettingsModel _azureAISearchSettings;
        private readonly SearchClient _searchClient;

        public SearchService(IOptions<AzureAISearchSettingsModel> searchOptions)
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
        }

        public async Task<List<string>> SearchAsync(string userQuery, float[] userQueryEmbedding, int KNearestNeighborsCount = 3, int retrievedDataNum= 3)
        {
            var vectorQuery = new VectorizedQuery(userQueryEmbedding)
            {
                Fields = {"embedding"}, // Must match the vector field in index
                KNearestNeighborsCount = KNearestNeighborsCount
            };

            var searchOptions = new SearchOptions
            {
                Size = retrievedDataNum,
                QueryType = SearchQueryType.Semantic,
                VectorSearch = new VectorSearchOptions
                {
                    Queries = { vectorQuery }
                },
                SemanticSearch = new SemanticSearchOptions
                {
                    SemanticConfigurationName = _azureAISearchSettings.SemanticConfigurationName
                },
                Select = {"*"}
            };
            var response = await _searchClient.SearchAsync<SearchDocument>(userQuery, searchOptions);

            var topChunks = new List<string>();
            await foreach (var result in response.Value.GetResultsAsync())
            {
                var doc = result.Document;

                string json = JsonSerializer.Serialize(doc, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                topChunks.Add(json);
            }

            if (topChunks.Count == 0)
            {
                throw new Exception("No results found.");
            }

            return topChunks;
        }
    }
}