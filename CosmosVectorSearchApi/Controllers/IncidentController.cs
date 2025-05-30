using Microsoft.AspNetCore.Mvc;
using CosmosVectorSearchApi.Interfaces;
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
            [FromQuery] string databaseName,
            [FromQuery] string collectionName = "incidents")
        {
            try
            {
                _logger.LogInformation("Starting data ingestion process");
                
                if (string.IsNullOrEmpty(databaseName))
                {
                    return BadRequest("Database name is required");
                }
                
                // Call the simplified service method that handles the Kusto and Cosmos logic
                bool success = await _vectorDbService.IngestDataAsync(databaseName, collectionName);
                
                if (success)
                {
                    return Ok(new
                    {
                        Message = "Successfully ingested incidents into Vector DB",
                        CollectionName = collectionName
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
                _logger.LogError(ex, "Error ingesting data");
                return StatusCode(500, $"Error ingesting data: {ex.Message}");
            }
        }
    }
}
