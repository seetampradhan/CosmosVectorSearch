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