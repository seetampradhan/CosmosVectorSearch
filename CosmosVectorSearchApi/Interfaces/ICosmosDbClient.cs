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
    }
}
