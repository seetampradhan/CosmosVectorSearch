using Microsoft.Azure.Cosmos;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Connectors.CosmosNoSql;

namespace CosmosVectorSearchApi.Interfaces
{
    public interface ICosmosDbClient
    {
        /// <summary>
        /// Gets the Cosmos DB database instance, creating it if it does not exist.
        /// </summary>
        /// <param name="databaseName">Optional database name. If not provided, the default from configuration will be used.</param>
        /// <returns>The Cosmos DB database.</returns>
        Task<Database> GetDatabaseAsync(string databaseName);

        /// <summary>
        /// Gets a CosmosNoSqlCollection instance for the specified collection name.
        /// </summary>
        /// <typeparam name="TKey">The type of the key for the collection.</typeparam>
        /// <typeparam name="TValue">The type of the value for the collection.</typeparam>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="databaseName">Optional database name. If not provided, the default from configuration will be used.</param>
        /// <returns>A CosmosNoSqlCollection instance.</returns>
        Task<CosmosNoSqlCollection<TKey, TValue>> GetCosmosNoSqlCollectionAsync<TKey, TValue>(string collectionName, string databaseName)
            where TKey : notnull
            where TValue : class;

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
        Task<IReadOnlyList<(T Item, double SimilarityScore)>> MultiVectorSearchAsync<T>(
            string containerName,
            string databaseName,
            IDictionary<string, IReadOnlyList<float>> embeddingsAndFields,
            IDictionary<string, double> weights,
            string? selectFields = null,
            string? filter = null,
            int maxResults = 10);
    }
}
