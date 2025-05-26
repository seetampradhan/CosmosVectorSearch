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
            
        /// <summary>
        /// Searches for incidents similar to a provided incident
        /// </summary>
        /// <param name="incident">The incident to use as a search template</param>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <param name="titleWeight">Weight for the title embedding</param>
        /// <param name="summaryWeight">Weight for the summary embedding</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <returns>A list of similar incidents with their similarity scores</returns>
        Task<IReadOnlyList<(Incident Item, double SimilarityScore)>> SearchSimilarIncidentsAsync(
            Incident incident,
            string databaseName, 
            string collectionName,
            double titleWeight = 0.7, 
            double summaryWeight = 0.3,
            int maxResults = 10);
            
        /// <summary>
        /// Searches for incidents similar to a provided incident using search parameters
        /// </summary>
        /// <param name="searchParams">The search parameters including incident and search configuration</param>
        /// <returns>A list of similar incidents with their similarity scores</returns>
        Task<IReadOnlyList<(Incident Item, double SimilarityScore)>> SearchSimilarIncidentsAsync(
            IncidentSearchParameters searchParams);
    }
}
