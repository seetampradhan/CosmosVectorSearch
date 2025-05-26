namespace CosmosVectorSearchApi.Services
{
    using Microsoft.Extensions.AI;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Logging;
    using CosmosVectorSearchApi.Options;
    using OllamaSharp;

    public class BaseOllamaEmbeddingService
    {
        private readonly ILogger<BaseOllamaEmbeddingService> _logger;
        private readonly OllamaOptions _ollamaOptions;
        private readonly OllamaApiClient _ollamaApiClient;
        public IEmbeddingGenerator<string, Embedding<float>> EmbeddingGenerator { get;set; }

        public BaseOllamaEmbeddingService(
                 ILogger<BaseOllamaEmbeddingService> logger,
                 IOptions<OllamaOptions> ollamaOptions)
        {
            _ollamaOptions = ollamaOptions.Value;
            _logger = logger;
            _ollamaApiClient = new OllamaApiClient(_ollamaOptions.OllamaUri, _ollamaOptions.ModelName);
            // Set the EmbeddingGenerator property to the OllamaApiClient which implements IEmbeddingGenerator
            this.EmbeddingGenerator = _ollamaApiClient;
        }
        
        /// <summary>
        /// Generates embeddings for multiple strings and returns all of them
        /// </summary>
        /// <param name="values">The strings to generate embeddings for</param>
        /// <returns>A collection of embeddings, one for each input string</returns>
        public async Task<IReadOnlyList<Embedding<float>>> GenerateEmbeddingsAsync(IEnumerable<string> values)
        {
            if (values == null || !values.Any())
            {
                _logger.LogWarning("No values provided for embedding generation.");
                return Array.Empty<Embedding<float>>();
            }

            try
            {
                // Use the EmbeddingGenerator property directly
                var embeddingResults = await this.EmbeddingGenerator.GenerateAsync(values, null, CancellationToken.None);
                return embeddingResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embeddings for values: {Values}", string.Join(", ", values));
                throw;
            }
        }
    }
}