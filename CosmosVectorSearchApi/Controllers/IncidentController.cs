using Microsoft.AspNetCore.Mvc;
using CosmosVectorSearchApi.Interfaces;
using CosmosVectorSearchApi.Models;
using CosmosVectorSearchApi.Options;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

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

        /// <summary>
        /// Searches for similar incidents using multi-vector search based on an existing incident
        /// </summary>
        /// <returns>Incidents similar to the provided incident ordered by relevance</returns>
        [HttpPost("search-similar")]
        public async Task<IActionResult> SearchSimilarIncidents([FromBody] IncidentSearchParameters searchParams)
        {
            try
            {
                if (searchParams == null)
                {
                    return BadRequest("Search parameters are required");
                }

                if (searchParams.Incident == null)
                {
                    return BadRequest("Incident is required");
                }

                if (string.IsNullOrEmpty(searchParams.DatabaseName))
                {
                    return BadRequest("Database name is required");
                }

                _logger.LogInformation("Searching for incidents similar to incident with title: {Title}", searchParams.Incident.Title);
                
                // Use the VectorDbService to perform the search using the parameters model
                var searchResults = await _vectorDbService.SearchSimilarIncidentsAsync(searchParams);
                
                // Transform the results to a more client-friendly format
                var results = searchResults.Select(result => new
                {
                    incident = result.Item,
                    similarityScore = result.SimilarityScore
                }).ToList();
                
                return Ok(new
                {
                    sourceIncident = new { title = searchParams.Incident.Title, summary = searchParams.Incident.Summary },
                    results,
                    count = results.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for similar incidents");
                return StatusCode(500, $"Error searching for similar incidents: {ex.Message}");
            }
        }
    }
}
