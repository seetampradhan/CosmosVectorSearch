namespace CosmosVectorSearchApi.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Linq;
    using Microsoft.Extensions.Logging;
    using CosmosVectorSearchApi.Interfaces;
    using Microsoft.Extensions.VectorData;
    using CosmosVectorSearchApi.Models;

    /// <summary>
    /// Service for data ingestion into Cosmos DB
    /// </summary>
    public class VectorDbServices : IVectorDbService
    {
        private readonly IVectorEmbeddingService _vectorEmbeddingService;
        private readonly ILogger<VectorDbServices> _logger;
        private readonly ICosmosDbClient _cosmosDbClient;

        public VectorDbServices(IVectorEmbeddingService vectorEmbeddingService, ILogger<VectorDbServices> logger, ICosmosDbClient cosmosDbClient)
        {
            _vectorEmbeddingService = vectorEmbeddingService ?? throw new ArgumentNullException(nameof(vectorEmbeddingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cosmosDbClient = cosmosDbClient ?? throw new ArgumentNullException(nameof(cosmosDbClient));
        }

        public async Task IngestDataAsync<TKey, TValue>(string containerName, string partitionKeyPath, VectorStoreCollection<TKey, TValue> collection, IEnumerable<TValue> items)
            where TKey : notnull
            where TValue : class
        {
            try
            {
                var container = await _cosmosDbClient.GetOrCreateDatabaseAndContainerAsync(containerName, partitionKeyPath);
                await collection.EnsureCollectionExistsAsync();               
                if (typeof(TValue) == typeof(Incident))
                {
                    var incidents = items as IEnumerable<Incident>;
                    if (incidents != null)
                    {
                        var tasks = incidents.Select((icm, index) => Task.Run(async () =>
                        {
                            // Add a short delay between tasks based on index to prevent throttling
                            await Task.Delay(index * 50); // 50 milliseconds delay per item
                            icm.TittleEmbedding = (await _vectorEmbeddingService.GenerateEmbeddingAsync(icm.Title)).Vector;
                            icm.SummaryEmbedding = (await _vectorEmbeddingService.GenerateEmbeddingAsync(icm.Summary)).Vector;
                        })); await Task.WhenAll(tasks);

                        // Cast the incidents collection back to IEnumerable<TValue> for type safety
                        await collection.UpsertAsync(incidents.Cast<TValue>());
                        return; // Exit early since we've handled the upsert
                    }
                }

                // Only reaches here if not Incident type or incidents was null
                await collection.UpsertAsync(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ingesting data into Cosmos DB");
                throw;
            }
        }

        public async Task<IAsyncEnumerable<VectorSearchResult<TValue>>> SearchAsync<TKey, TValue>(
            VectorStoreCollection<TKey, TValue> collection,
            string queryText,
            string vectorField,
            int limit = 10)
            where TKey : notnull
            where TValue : class
        {
            try
            {
                // Generate embedding for the query text
                var queryEmbedding = await _vectorEmbeddingService.GenerateEmbeddingAsync(queryText);

                _logger.LogInformation("Performing vector search with query: {QueryText}", queryText);

                // Return search results using the native SearchAsync method of the collection
                return collection.SearchAsync(queryEmbedding.Vector, limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing vector search with query: {QueryText}", queryText);
                throw;
            }
        }
    }
}