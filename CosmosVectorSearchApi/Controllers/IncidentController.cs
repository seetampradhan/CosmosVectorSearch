using Microsoft.AspNetCore.Mvc;
using CosmosVectorSearchApi.Interfaces;
using CosmosVectorSearchApi.Models;
using Microsoft.SemanticKernel.Connectors.CosmosNoSql;
using CosmosVectorSearchApi.Options;
using Microsoft.Extensions.Options;

namespace CosmosVectorSearchApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IncidentController : ControllerBase
    {
        private readonly IKustoClient _kustoClient;
        private readonly IVectorDbService _vectorDbService;
        private readonly ILogger<IncidentController> _logger;
        private readonly ICosmosDbClient _cosmosDbClient;
        private readonly KustoOptions _kustoOptions;

        public IncidentController(
            IKustoClient kustoClient,
            IVectorDbService vectorDbService,
            ILogger<IncidentController> logger,
            ICosmosDbClient cosmosDbClient,
            IOptions<KustoOptions> kustoOptions)
        {
            _kustoClient = kustoClient ?? throw new ArgumentNullException(nameof(kustoClient));
            _vectorDbService = vectorDbService ?? throw new ArgumentNullException(nameof(vectorDbService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cosmosDbClient = cosmosDbClient ?? throw new ArgumentNullException(nameof(cosmosDbClient));
            _kustoOptions = kustoOptions?.Value ?? throw new ArgumentNullException(nameof(kustoOptions));
        }

        [HttpPost("ingest")]
        public async Task<IActionResult> IngestIncidentsFromKusto(
            [FromQuery] string containerName = "incidents",
            [FromQuery] string partitionKeyPath = "/IncidentId",
            [FromQuery] string? kustoQuery = null)
        {
            try
            {
                _logger.LogInformation("Starting Kusto query and data ingestion");                // Execute the Kusto query
                string query = !string.IsNullOrEmpty(kustoQuery)
                    ? kustoQuery
                    : "datawarehouse | take 2000"; // Default query if none provided

                _logger.LogInformation("Executing Kusto query: {Query}", query);

                // Use the new generic ExecuteQuery<T> method to get strongly-typed results directly
                var incidents = await _kustoClient.Query<Incident>(query, _kustoOptions.KustoDatabase);

                _logger.LogInformation("Retrieved {Count} incidents from Kusto", incidents.Count);

                if (incidents.Count > 0)
                {
                    var database = await _cosmosDbClient.GetDatabaseAsync();

                    // Create a vector store collection
                    var collection = new CosmosNoSqlCollection<string, Incident>(database, "incidents");

                    // Ingest the data
                    await _vectorDbService.IngestDataAsync(containerName, partitionKeyPath, collection, incidents);

                    _logger.LogInformation("Successfully ingested {Count} incidents into Vector DB", incidents.Count);

                    return Ok(new
                    {
                        Message = $"Successfully ingested {incidents.Count} incidents into Vector DB",
                        ContainerName = containerName
                    });
                }
                else
                {
                    return Ok(new
                    {
                        Message = "No incidents found in Kusto query results"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying Kusto and ingesting data");
                return StatusCode(500, $"Error querying Kusto and ingesting data: {ex.Message}");
            }
        }
    }
}
