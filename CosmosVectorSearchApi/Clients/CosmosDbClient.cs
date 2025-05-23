using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Azure.Identity;
using CosmosVectorSearchApi.Options;
using CosmosVectorSearchApi.Interfaces;
using System.Text.Json;
using System.Collections.ObjectModel;

namespace CosmosVectorSearchApi.Clients
{
    /// <summary>
    /// Client for interacting with Azure Cosmos DB
    /// </summary>
    public class CosmosDbClient : ICosmosDbClient
    {
        private readonly CosmosClient _cosmosClient;
        private readonly IOptions<CosmosDbOptions> _options;
        private Database? _database;        /// <summary>
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
        /// Gets the Cosmos DB database instance.
        /// </summary>
        /// <returns>The Cosmos DB database.</returns>
        public async Task<Database> GetDatabaseAsync()
        {
            if (_database is null)
            {
                _database = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_options.Value.DatabaseName);
                if (_database == null)
                {
                    throw new InvalidOperationException("Failed to create or retrieve the database.");
                }
            }

            return _database;
        }

        /// <summary>
        /// Gets a container from the database.
        /// </summary>
        /// <param name="containerName">The name of the container.</param>
        /// <returns>The container.</returns>
        public async Task<Container> GetContainerAsync(string containerName)
        {
            var database = await GetDatabaseAsync();
            return database.GetContainer(containerName);
        }

        /// <summary>
        /// Creates a container in the database if it doesn't exist.
        /// </summary>
        /// <param name="containerName">The name of the container.</param>
        /// <param name="partitionKeyPath">The path to the partition key.</param>
        /// <returns>The container.</returns>
        public async Task<Container> CreateContainerIfNotExistsAsync(string containerName, string partitionKeyPath)
        {
            var database = await GetDatabaseAsync();
            return await database.CreateContainerIfNotExistsAsync(containerName, partitionKeyPath);
        }

        /// <summary>
        /// Gets or creates the database and container.
        /// </summary>
        /// <param name="containerName">The name of the container.</param>
        /// <param name="partitionKeyPath">The path to the partition key.</param>
        /// <returns>The container.</returns>
        public async Task<Container> GetOrCreateDatabaseAndContainerAsync(string containerName, string partitionKeyPath)
        {
            var database = await GetDatabaseAsync();

            List<Embedding> embeddings = new List<Embedding>()
            {
                new Embedding()
                {
                    Path = "/tittleEmbedding",
                    DataType = VectorDataType.Float32,
                    DistanceFunction = DistanceFunction.Cosine,
                    Dimensions = 2406,

                },
                new Embedding()
                {
                    Path = "/summaryEmbedding",
                    DataType = VectorDataType.Float32,
                    DistanceFunction = DistanceFunction.Cosine,
                    Dimensions = 2406,
                }
            };

            Collection<Embedding> collection = new Collection<Embedding>(embeddings);

            ContainerProperties properties = new ContainerProperties(containerName, partitionKeyPath)
            {
                VectorEmbeddingPolicy = new(collection),
                IndexingPolicy = new IndexingPolicy()
                {
                    VectorIndexes = new()
                    {
                        new VectorIndexPath()
                        {
                            Path = "/tittleEmbedding",
                            Type = VectorIndexType.QuantizedFlat,
                        },
                        new VectorIndexPath()
                        {
                            Path = "/summaryEmbedding",
                            Type = VectorIndexType.DiskANN,
                        }
                    }
                },
            };
            properties.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/*" });
            properties.IndexingPolicy.ExcludedPaths.Add(new ExcludedPath { Path = "/tittleEmbedding/*" });
            properties.IndexingPolicy.ExcludedPaths.Add(new ExcludedPath { Path = "/summaryEmbedding/*" });
            
            return await database.CreateContainerIfNotExistsAsync(properties);
        }
    }
}