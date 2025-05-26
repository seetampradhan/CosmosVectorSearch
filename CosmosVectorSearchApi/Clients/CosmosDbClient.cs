using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Azure.Identity;
using CosmosVectorSearchApi.Options;
using CosmosVectorSearchApi.Interfaces;
using System.Text.Json;
using Microsoft.SemanticKernel.Connectors.CosmosNoSql;

namespace CosmosVectorSearchApi.Clients
{
    /// <summary>
    /// Client for interacting with Azure Cosmos DB
    /// </summary>
    public class CosmosDbClient : ICosmosDbClient
    {
        private readonly CosmosClient _cosmosClient;
        private readonly IOptions<CosmosDbOptions> _options;
        private Dictionary<string, Database> _databases = new Dictionary<string, Database>();
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbClient"/> class.
        /// </summary>
        /// <param name="options">The Cosmos DB options.</param>
        public CosmosDbClient(IOptions<CosmosDbOptions> options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            // Check if connection string is provided, otherwise use endpoint with DefaultAzureCredential
            if (!string.IsNullOrEmpty(_options.Value.ConnectionString))
            {
                _cosmosClient = new CosmosClient(
                    _options.Value.ConnectionString,
                    new CosmosClientOptions
                    {
                        UseSystemTextJsonSerializerWithOptions = JsonSerializerOptions.Default
                    });
            }
            else
            {
                var credential = new DefaultAzureCredential();
                _cosmosClient = new CosmosClient(
                    _options.Value.Endpoint,
                    credential,
                    new CosmosClientOptions
                    {
                        UseSystemTextJsonSerializerWithOptions = JsonSerializerOptions.Default
                    });
            }
        }

        /// <summary>
        /// Gets the Cosmos DB database instance, creating it if it does not exist.
        /// </summary>
        /// <param name="databaseName">Optional database name. If not provided, the default from configuration will be used.</param>
        /// <returns>The Cosmos DB database.</returns>
        public async Task<Database> GetDatabaseAsync(string databaseName)
        {
            // Use the provided database name or fall back to the one in options
            string dbName = databaseName;
            
            if (string.IsNullOrEmpty(dbName))
            {
                throw new ArgumentException("Database name must be provided either through method parameter or configuration.");
            }

            // Check if we already have a cached database instance
            if (_databases.TryGetValue(dbName, out var database))
            {
                return database;
            }

            // Create the database if it doesn't exist
            database = await _cosmosClient.CreateDatabaseIfNotExistsAsync(dbName);
            if (database == null)
            {
                throw new InvalidOperationException($"Failed to create or retrieve the database '{dbName}'.");
            }

            // Cache the database instance
            _databases[dbName] = database;
            return database;
        }

        /// <summary>
        /// Performs a multi-vector similarity search on a Cosmos DB container.
        /// </summary>
        /// <typeparam name="T">The type of the items to return from the search.</typeparam>
        /// <param name="containerName">The name of the container to search in.</param>
        /// <param name="databaseName">The name of the database containing the container.</param>
        /// <param name="embeddingsAndFields">A dictionary mapping vector field names to their corresponding embeddings.</param>
        /// <param name="weights">Dictionary of weights for each vector field.</param>
        /// <param name="selectFields">Optional comma-separated list of fields to select. If null, selects all fields.</param>
        /// <param name="filter">Optional additional filter condition for the query.</param>
        /// <param name="maxResults">Optional maximum number of results to return. Default is 10.</param>
        /// <returns>A list of search results with their combined similarity scores.</returns>
        public async Task<IReadOnlyList<(T Item, double SimilarityScore)>> MultiVectorSearchAsync<T>(
            string containerName,
            string databaseName,
            IDictionary<string, IReadOnlyList<float>> embeddingsAndFields,
            IDictionary<string, double> weights,
            string? selectFields = null,
            string? filter = null,
            int maxResults = 10)
        {
            if (string.IsNullOrEmpty(containerName))
                throw new ArgumentException("Container name cannot be null or empty", nameof(containerName));
            
            if (embeddingsAndFields == null || embeddingsAndFields.Count == 0)
                throw new ArgumentException("Embeddings dictionary cannot be null or empty", nameof(embeddingsAndFields));

            if (weights == null || weights.Count == 0)
                throw new ArgumentException("Weights dictionary cannot be null or empty", nameof(weights));

            // Validate embeddings and weights
            foreach (var fieldName in embeddingsAndFields.Keys)
            {
                if (string.IsNullOrEmpty(fieldName))
                    throw new ArgumentException("Vector field name cannot be null or empty");
                
                if (embeddingsAndFields[fieldName] == null || embeddingsAndFields[fieldName].Count == 0)
                    throw new ArgumentException($"Embedding vector for field '{fieldName}' cannot be null or empty");
                
                if (!weights.ContainsKey(fieldName))
                    throw new ArgumentException($"Weight for field '{fieldName}' not provided in weights dictionary");
            }

            // Get the database
            Database database = await GetDatabaseAsync(databaseName);
            
            // Get the container
            Container container = database.GetContainer(containerName);
            
            // Build the vector distance functions for each field
            var vectorDistanceFunctions = new Dictionary<string, string>();
            var parameterCount = 0;

            foreach (var (fieldName, _) in embeddingsAndFields)
            {
                string paramName = $"@embedding{parameterCount}";
                string vectorDistanceFunc = $"VectorDistance(c.{fieldName}, {paramName})";
                vectorDistanceFunctions.Add(fieldName, vectorDistanceFunc);
                parameterCount++;
            }

            // Build the weighted sum expression for the combined similarity score
            // Using a simpler expression format for ORDER BY compatibility
            var weightedSumExpressions = new List<string>();
            foreach (var (fieldName, _) in embeddingsAndFields)
            {
                double weight = weights[fieldName];
                weightedSumExpressions.Add($"{weight} * {vectorDistanceFunctions[fieldName]}");
            }

            string combinedScoreExpression = string.Join(" + ", weightedSumExpressions);
            
            // Build individual similarity score expressions for the SELECT clause
            var similarityScoreExpressions = new List<string>();
            foreach (var (fieldName, func) in vectorDistanceFunctions)
            {
                similarityScoreExpressions.Add($"{func} AS {fieldName}_Score");
            }
            similarityScoreExpressions.Add($"{combinedScoreExpression} AS CombinedScore");
            
            string similarityScoresClause = string.Join(", ", similarityScoreExpressions);
            
            // Build the query following Microsoft's recommended pattern for vector search
            string query;
            
            // Determine which fields to select
            string selectClause = string.IsNullOrEmpty(selectFields) ? "c" : selectFields;
            
            // Create vector distance clause - important to avoid using aliases in ORDER BY
            string vectorDistanceClause = combinedScoreExpression;
            
            // Basic query structure
            query = $"SELECT {selectClause}, {similarityScoresClause} FROM c";
            
            // Add filter if provided
            if (!string.IsNullOrEmpty(filter))
            {
                query = $"{query} WHERE {filter}";
            }
            
            // Add ORDER BY clause directly with the vector distance expression
            // This follows the official Microsoft pattern for Cosmos DB vector search
            query = $"{query} ORDER BY {vectorDistanceClause}";
            
            // Add LIMIT clause if maxResults is specified
            if (maxResults > 0)
            {
                query = $"{query} OFFSET 0 LIMIT {maxResults}";
            }
            
            // Create query definition and add embedding parameters
            QueryDefinition queryDef = new QueryDefinition(query);
            
            parameterCount = 0;
            foreach (var (fieldName, embedding) in embeddingsAndFields)
            {
                queryDef = queryDef.WithParameter($"@embedding{parameterCount}", embedding.ToArray());
                parameterCount++;
            }
            
            // Execute the query
            var results = new List<(T Item, double SimilarityScore)>();
            
            using (FeedIterator<dynamic> feedIterator = container.GetItemQueryIterator<dynamic>(queryDef))
            {
                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<dynamic> response = await feedIterator.ReadNextAsync();
                    
                    foreach (var item in response)
                    {
                        try
                        {
                            // Extract the combined similarity score
                            double combinedScore = Convert.ToDouble(item.CombinedScore);
                            
                            // Create a copy of the item without the score properties for conversion to T
                            var itemJson = JsonSerializer.Serialize(item);
                            var itemCopy = JsonSerializer.Deserialize<Dictionary<string, object>>(itemJson);
                            
                            // Remove all score properties
                            itemCopy.Remove("CombinedScore");
                            foreach (var fieldName in embeddingsAndFields.Keys)
                            {
                                itemCopy.Remove($"{fieldName}_Score");
                            }
                            
                            // If we selected "c" as the document, unwrap it
                            if (itemCopy.ContainsKey("c") && selectClause == "c")
                            {
                                var originalDoc = itemCopy["c"];
                                itemCopy = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(originalDoc));
                            }
                            
                            // Convert to the requested type
                            T typedItem = JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(itemCopy));
                            
                            results.Add((typedItem, combinedScore));
                        }
                        catch (Exception ex)
                        {
                            // Log error and continue with next item
                            Console.WriteLine($"Error processing search result item: {ex.Message}");
                            continue;
                        }
                    }
                }
            }
            
            return results;
        }

        /// <summary>
        /// Gets a CosmosNoSqlCollection instance for the specified collection name.
        /// </summary>
        /// <typeparam name="TKey">The type of the key for the collection.</typeparam>
        /// <typeparam name="TValue">The type of the value for the collection.</typeparam>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="databaseName">Optional database name. If not provided, the default from configuration will be used.</param>
        /// <returns>A CosmosNoSqlCollection instance.</returns>
        public async Task<CosmosNoSqlCollection<TKey, TValue>> GetCosmosNoSqlCollectionAsync<TKey, TValue>(string collectionName, string databaseName)
            where TKey : notnull
            where TValue : class
        {
            var database = await GetDatabaseAsync(databaseName);
            var collection = new CosmosNoSqlCollection<TKey, TValue>(database, collectionName);
            
            return collection;
        }
    }
}