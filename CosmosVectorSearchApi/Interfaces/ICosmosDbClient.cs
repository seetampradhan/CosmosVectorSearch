// filepath: c:\Users\seeta\Code\CosmosVectorSearch\CosmosVectorSearchApi\Interfaces\ICosmosDbClient.cs
using Microsoft.Azure.Cosmos;
using System.Threading.Tasks;

namespace CosmosVectorSearchApi.Interfaces
{
    public interface ICosmosDbClient
    {
        Task<Database> GetDatabaseAsync();
        Task<Container> GetContainerAsync(string containerName);
        Task<Container> CreateContainerIfNotExistsAsync(string containerName, string partitionKeyPath);
        Task<Container> GetOrCreateDatabaseAndContainerAsync(string containerName, string partitionKeyPath);
    }
}
