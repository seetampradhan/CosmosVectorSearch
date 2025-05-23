namespace CosmosVectorSearchApi.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Extensions.VectorData;

    /// <summary>
    /// Interface for Vector Database Services
    /// </summary>
    public interface IVectorDbService
    {
        /// <summary>
        /// Ingests data into a vector database collection
        /// </summary>
        /// <typeparam name="TKey">The type of the key for the vector store collection</typeparam>
        /// <typeparam name="TValue">The type of the value for the vector store collection</typeparam>
        /// <param name="containerName">The name of the container</param>
        /// <param name="partitionKeyPath">The partition key path</param>
        /// <param name="collection">The vector store collection</param>
        /// <param name="items">The items to ingest</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task IngestDataAsync<TKey, TValue>(string containerName, string partitionKeyPath, 
            VectorStoreCollection<TKey, TValue> collection, IEnumerable<TValue> items) 
            where TKey : notnull
            where TValue : class;
            
        /// <summary>
        /// Performs a vector search in the collection
        /// </summary>
        /// <typeparam name="TKey">The type of the key for the vector store collection</typeparam>
        /// <typeparam name="TValue">The type of the value for the vector store collection</typeparam>
        /// <param name="collection">The vector store collection</param>
        /// <param name="queryText">The text to search for</param>
        /// <param name="vectorField">The vector field to search in</param>
        /// <param name="limit">Maximum number of results to return</param>
        /// <returns>A collection of search results</returns>
        Task<IAsyncEnumerable<VectorSearchResult<TValue>>> SearchAsync<TKey, TValue>(
            VectorStoreCollection<TKey, TValue> collection, 
            string queryText, 
            string vectorField,
            int limit = 10)
            where TKey : notnull
            where TValue : class;
    }
}
