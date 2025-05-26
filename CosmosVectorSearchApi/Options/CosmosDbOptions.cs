namespace CosmosVectorSearchApi.Options;

/// <summary>
/// Configuration options for Cosmos DB connection
/// </summary>
public class CosmosDbOptions
{
    /// <summary>
    /// The configuration section name for CosmosDB options
    /// </summary>
    public const string SectionName = "Cosmos";

    /// <summary>
    /// The Cosmos DB endpoint URI
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// The Cosmos DB connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
}