namespace CosmosVectorSearchApi.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Extensions.VectorData;
    using CosmosVectorSearchApi.Models;

    /// <summary>
    /// Interface for Vector Database Services
    /// </summary>
    public interface IVectorDbService
    {
        /// <summary>
        /// Ingests incident data from Kusto into a vector database collection
        /// </summary>
        /// <param name="databaseName">The name of the database</param>
        /// <param name="collectionName">The name of the collection</param>
        /// <returns>A task representing the asynchronous operation that returns true if successful</returns>
        Task<bool> IngestDataAsync(string databaseName, string collectionName);
    }
}
