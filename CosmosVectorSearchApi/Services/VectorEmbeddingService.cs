using CosmosVectorSearchApi.Interfaces;
using CosmosVectorSearchApi.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace CosmosVectorSearchApi.Services
{
    public class VectorEmbeddingService : IVectorEmbeddingService
    {
        private readonly ILogger<VectorEmbeddingService> _logger;
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

        public VectorEmbeddingService(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator, ILogger<VectorEmbeddingService> logger)
        {
            _logger = logger;
            _embeddingGenerator = embeddingGenerator;
        }

        public async Task<Embedding<float>> GenerateEmbeddingAsync(string text)
        {
            try
            {
                var embedding = await _embeddingGenerator.GenerateAsync(text);
                return embedding;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding for text: {Text}", text);
                throw;
            }
        }
    }
}