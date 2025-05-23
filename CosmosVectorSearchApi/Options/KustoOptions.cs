namespace CosmosVectorSearchApi.Options;

/// <summary>
/// Configuration options for Kusto (Azure Data Explorer) connection
/// </summary>
public class KustoOptions
{
    /// <summary>
    /// The configuration section name for Kusto options
    /// </summary>
    public const string SectionName = "Kusto";

    /// <summary>
    /// The Kusto cluster URI
    /// </summary>
    public string KustoUri { get; set; } = string.Empty;

    /// <summary>
    /// The Kusto database name
    /// </summary>
    public string KustoDatabase { get; set; } = string.Empty;

    /// <summary>
    /// The Azure AD tenant ID for authentication
    /// </summary>
    public string TenantId { get; set; } = string.Empty;
}