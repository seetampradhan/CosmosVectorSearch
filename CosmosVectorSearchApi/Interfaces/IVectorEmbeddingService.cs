using Microsoft.Extensions.AI;
using System.Threading.Tasks;

namespace CosmosVectorSearchApi.Interfaces
{
    public interface IVectorEmbeddingService
    {
        Task<Embedding<float>> GenerateEmbeddingAsync(string text);
    }
}
