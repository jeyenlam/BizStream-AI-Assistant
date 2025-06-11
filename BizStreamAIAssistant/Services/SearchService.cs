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

        public async Task<List<string>> SearchAsync(string userQuery, float[] userQueryEmbedding)
        {
            var vectorQuery = new VectorizedQuery(userQueryEmbedding)
            {
                Fields = {"embedding"}, // Must match the vector field in index
                KNearestNeighborsCount = 5
            };

            var searchOptions = new SearchOptions
            {
                Size = 5,
                QueryType = SearchQueryType.Semantic,
                VectorSearch = new VectorSearchOptions
                {
                    Queries = { vectorQuery }
                },
                SemanticSearch = new SemanticSearchOptions
                {
                    SemanticConfigurationName = _azureAISearchSettings.SemanticConfigurationName
                },
                Select = {"text"}
            };
            var response = await _searchClient.SearchAsync<SearchDocument>(userQuery, searchOptions);

            var topChunks = response.Value.GetResults()
                .Select(r => r.Document["text"]?.ToString())
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .Cast<string>()
                .ToList();
                

            if (topChunks.Count == 0)
            {
                throw new Exception("No results found.");
            }

            return topChunks;
        }
    }
}