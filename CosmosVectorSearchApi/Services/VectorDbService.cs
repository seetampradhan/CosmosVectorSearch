namespace CosmosVectorSearchApi.Services
{
    using System;
    using System.Threading.Tasks;
    using System.Linq;
    using Microsoft.Extensions.Logging;
    using CosmosVectorSearchApi.Interfaces;
    using CosmosVectorSearchApi.Models;
    using Microsoft.Extensions.Options;
    using CosmosVectorSearchApi.Options;
    using System.Collections.Generic;
    using Microsoft.Extensions.AI;
    using System.Numerics.Tensors;

    /// <summary>
    /// Service for data ingestion into Cosmos DB
    /// </summary>
    public class VectorDbServices : BaseOllamaEmbeddingService, IVectorDbService
    {
        private readonly ILogger<VectorDbServices> _logger;
        private readonly ICosmosDbClient _cosmosDbClient;
        private readonly IKustoClient _kustoClient;

        public VectorDbServices(
            ILogger<VectorDbServices> logger,
            ICosmosDbClient cosmosDbClient,
            IOptions<OllamaOptions> ollamaOptions,
            IKustoClient kustoClient)
        : base(logger, ollamaOptions) // Correctly pass logger and ollamaOptions to base class
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cosmosDbClient = cosmosDbClient ?? throw new ArgumentNullException(nameof(cosmosDbClient));
            _kustoClient = kustoClient ?? throw new ArgumentNullException(nameof(kustoClient));
        }

        public async Task<bool> IngestDataAsync(string databaseName, string collectionName)
        {
            try
            {
                _logger.LogInformation("Starting Kusto query and data ingestion");

                // Default query if none provided
                string query = "datawarehouse | take 2000";

                _logger.LogInformation("Executing Kusto query: {Query}", query);

                // Get incidents from Kusto
                var incidents = await _kustoClient.Query<Incident>(query);

                _logger.LogInformation("Retrieved {Count} incidents from Kusto", incidents.Count);

                if (incidents.Count > 0)
                {
                    // Create a vector store collection
                    var collection = await _cosmosDbClient.GetCosmosNoSqlCollectionAsync<string, Incident>(collectionName, databaseName);

                    await collection.EnsureCollectionExistsAsync();

                    // Process incidents in batches to generate embeddings efficiently
                    const int batchSize = 20; // Adjust based on your API limits
                    
                    for (int i = 0; i < incidents.Count; i += batchSize)
                    {
                        // Get the current batch of incidents
                        var batch = incidents.Skip(i).Take(batchSize).ToList();
                        
                        // Extract titles and summaries from this batch
                        var titles = batch.Select(incident => incident.Title).ToList();
                        var summaries = batch.Select(incident => incident.Summary).ToList();
                        
                        // Generate embeddings for all titles and summaries in this batch
                        var titleEmbeddings = await GenerateEmbeddingsAsync(titles);
                        var summaryEmbeddings = await GenerateEmbeddingsAsync(summaries);
                        
                        // Assign embeddings to the corresponding incidents
                        for (int j = 0; j < batch.Count; j++)
                        {
                            if (j < titleEmbeddings.Count)
                            {
                                batch[j].TittleEmbedding = titleEmbeddings[j].Vector;
                            }
                            
                            if (j < summaryEmbeddings.Count)
                            {
                                batch[j].SummaryEmbedding = summaryEmbeddings[j].Vector;
                            }
                        }
                        
                        // Add a delay between batches to prevent throttling
                        if (i + batchSize < incidents.Count)
                        {
                            await Task.Delay(500);
                        }
                    }

                    // Apply dimension reduction before upserting
                    ApplyDimensionReduction(incidents, targetDimension: 5);

                    // Upsert the processed incidents
                    await collection.UpsertAsync(incidents);

                    _logger.LogInformation("Successfully ingested {Count} incidents into Vector DB", incidents.Count);

                    return true;
                }

                _logger.LogInformation("No incidents found in Kusto query results");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ingesting data into Cosmos DB");
                throw;
            }
        }
        
        /// <summary>
        /// Applies Principal Component Analysis (PCA) dimension reduction to embeddings
        /// </summary>
        /// <param name="incidents">List of incidents with embeddings to reduce</param>
        /// <param name="targetDimension">The target dimension (2 or 3 recommended)</param>
        private void ApplyDimensionReduction(List<Incident> incidents, int targetDimension = 3)
        {
            _logger.LogInformation("Applying dimension reduction to embeddings, target dimension: {TargetDimension}", targetDimension);
            
            if (incidents == null || incidents.Count == 0)
            {
                return;
            }

            // Create matrices for title and summary embeddings
            var titleVectors = new List<float[]>();
            var summaryVectors = new List<float[]>();
            
            // Collect all vectors
            foreach (var incident in incidents)
            {
                if (!incident.TittleEmbedding.IsEmpty)
                {
                    titleVectors.Add(incident.TittleEmbedding.ToArray());
                }
                
                if (!incident.SummaryEmbedding.IsEmpty)
                {
                    summaryVectors.Add(incident.SummaryEmbedding.ToArray());
                }
            }
            
            // Apply PCA to title embeddings if there are any
            if (titleVectors.Count > 0)
            {
                var reducedTitleVectors = PerformPCA(titleVectors, targetDimension);
                
                // Assign reduced vectors back to incidents
                int titleIndex = 0;
                foreach (var incident in incidents)
                {
                    if (!incident.TittleEmbedding.IsEmpty)
                    {
                        incident.TittleEmbedding = new ReadOnlyMemory<float>(reducedTitleVectors[titleIndex]);
                        titleIndex++;
                    }
                }
            }
            
            // Apply PCA to summary embeddings if there are any
            if (summaryVectors.Count > 0)
            {
                var reducedSummaryVectors = PerformPCA(summaryVectors, targetDimension);
                
                // Assign reduced vectors back to incidents
                int summaryIndex = 0;
                foreach (var incident in incidents)
                {
                    if (!incident.SummaryEmbedding.IsEmpty)
                    {
                        incident.SummaryEmbedding = new ReadOnlyMemory<float>(reducedSummaryVectors[summaryIndex]);
                        summaryIndex++;
                    }
                }
            }
            
            _logger.LogInformation("Dimension reduction completed. Reduced {TitleCount} title embeddings and {SummaryCount} summary embeddings to {Dimension} dimensions", 
                titleVectors.Count, summaryVectors.Count, targetDimension);
        }
        
        /// <summary>
        /// Performs PCA dimension reduction on a set of vectors
        /// </summary>
        /// <param name="vectors">List of vectors to reduce</param>
        /// <param name="targetDimension">Target dimension for the reduced vectors</param>
        /// <returns>List of reduced vectors</returns>
        private List<float[]> PerformPCA(List<float[]> vectors, int targetDimension)
        {
            if (vectors == null || vectors.Count == 0)
            {
                return new List<float[]>();
            }
            
            int originalDimension = vectors[0].Length;
            int numVectors = vectors.Count;
            
            // If we have fewer vectors than the target dimension or original dimension is already small enough
            if (numVectors < targetDimension || originalDimension <= targetDimension)
            {
                return vectors;
            }
            
            // Step 1: Center the data (subtract mean from each dimension)
            float[] means = new float[originalDimension];
            
            // Calculate means for each dimension
            for (int i = 0; i < originalDimension; i++)
            {
                float sum = 0;
                for (int j = 0; j < numVectors; j++)
                {
                    sum += vectors[j][i];
                }
                means[i] = sum / numVectors;
            }
            
            // Center the data
            float[][] centeredData = new float[numVectors][];
            for (int i = 0; i < numVectors; i++)
            {
                centeredData[i] = new float[originalDimension];
                for (int j = 0; j < originalDimension; j++)
                {
                    centeredData[i][j] = vectors[i][j] - means[j];
                }
            }
            
            // Step 2: Compute covariance matrix
            float[][] covarianceMatrix = new float[originalDimension][];
            for (int i = 0; i < originalDimension; i++)
            {
                covarianceMatrix[i] = new float[originalDimension];
                for (int j = 0; j < originalDimension; j++)
                {
                    float sum = 0;
                    for (int k = 0; k < numVectors; k++)
                    {
                        sum += centeredData[k][i] * centeredData[k][j];
                    }
                    covarianceMatrix[i][j] = sum / (numVectors - 1);
                }
            }
            
            // Step 3: Use a simple power iteration method to find principal components
            // This is a simplified approach - for production, use a proper eigenvalue decomposition library
            float[][] principalComponents = new float[targetDimension][];
            
            for (int d = 0; d < targetDimension; d++)
            {
                // Initialize random vector
                float[] v = new float[originalDimension];
                Random rand = new Random(d); // Seed for reproducibility
                for (int i = 0; i < originalDimension; i++)
                {
                    v[i] = (float)rand.NextDouble();
                }
                
                // Power iteration to find eigenvector
                for (int iter = 0; iter < 100; iter++) // Usually converges in far fewer iterations
                {
                    float[] vNew = new float[originalDimension];
                    
                    // Matrix-vector multiplication
                    for (int i = 0; i < originalDimension; i++)
                    {
                        for (int j = 0; j < originalDimension; j++)
                        {
                            vNew[i] += covarianceMatrix[i][j] * v[j];
                        }
                    }
                    
                    // Normalize
                    float norm = 0;
                    for (int i = 0; i < originalDimension; i++)
                    {
                        norm += vNew[i] * vNew[i];
                    }
                    norm = (float)Math.Sqrt(norm);
                    
                    for (int i = 0; i < originalDimension; i++)
                    {
                        v[i] = vNew[i] / norm;
                    }
                }
                
                // Store the principal component
                principalComponents[d] = v;
                
                // Deflate the covariance matrix (optional, for orthogonality)
                if (d < targetDimension - 1)
                {
                    for (int i = 0; i < originalDimension; i++)
                    {
                        for (int j = 0; j < originalDimension; j++)
                        {
                            covarianceMatrix[i][j] -= v[i] * v[j];
                        }
                    }
                }
            }
            
            // Step 4: Project the data onto the principal components
            List<float[]> reducedVectors = new List<float[]>();
            
            for (int i = 0; i < numVectors; i++)
            {
                float[] reducedVector = new float[targetDimension];
                
                for (int d = 0; d < targetDimension; d++)
                {
                    float dotProduct = 0;
                    for (int j = 0; j < originalDimension; j++)
                    {
                        dotProduct += centeredData[i][j] * principalComponents[d][j];
                    }
                    reducedVector[d] = dotProduct;
                }
                
                reducedVectors.Add(reducedVector);
            }
            
            return reducedVectors;
        }
    }
}