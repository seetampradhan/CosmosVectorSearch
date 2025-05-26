// filepath: c:\Users\seeta\Code\CosmosVectorSearch\CosmosVectorSearchApi\Interfaces\IKustoClient.cs
using Kusto.Data.Common;
using System.Data;
using System.Collections.Generic;

namespace CosmosVectorSearchApi.Interfaces
{
    public interface IKustoClient
    {
         Task<List<T>> Query<T>(string query);
    }
}
