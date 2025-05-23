#pragma warning disable SKEXP0070

namespace CosmosVectorSearchApi.Extensions
{
    using CosmosVectorSearchApi.Options;
    using CosmosVectorSearchApi.Clients;
    using CosmosVectorSearchApi.Interfaces;
    using CosmosVectorSearchApi.Services;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Configuration;

    public static class ServiceExtensions
    {
        /// <summary>
        /// Adds the Cosmos DB options to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection SetUpCosmosVectorSearchClient(this IServiceCollection services)
        {
            // Register options
            services.AddSingleton<IConfigureOptions<KustoOptions>, DefaultKustoOptions>();
            services.AddSingleton<IConfigureOptions<CosmosDbOptions>, DefaultCosmosDbOptions>();
            services.AddSingleton<IConfigureOptions<OllamaOptions>, DefaultOllamaOptions>();
            services.AddSingleton<ICosmosDbClient, CosmosDbClient>();
            services.AddSingleton<IKustoClient, KustoClient>();
            services.AddSingleton<IVectorEmbeddingService, VectorEmbeddingService>();
            services.AddSingleton<IVectorDbService, VectorDbServices>();

            return services;
        }
        public static IServiceCollection SetUpCosmosVectorSearchServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Get Ollama config section directly from configuration
            var ollamaSection = configuration.GetSection(OllamaOptions.SectionName);
            var ollamaUri = ollamaSection.GetValue<string>("OllamaUri") ?? string.Empty;
            var ollamaModelName = ollamaSection.GetValue<string>("ModelName") ?? string.Empty;

            // Add Ollama embedding generator that will be used by VectorEmbeddingService
            services.AddOllamaEmbeddingGenerator(endpoint: new Uri(ollamaUri), modelId: ollamaModelName);

            // Get Cosmos DB config section directly from configuration
            var cosmosSection = configuration.GetSection(CosmosDbOptions.SectionName);
            var cosmosEndpoint = cosmosSection.GetValue<string>("Endpoint") ?? string.Empty;
            var cosmosDatabaseName = cosmosSection.GetValue<string>("DatabaseName") ?? string.Empty;

            // Add Cosmos vector store
            services.AddCosmosNoSqlVectorStore(cosmosEndpoint, cosmosDatabaseName);

            return services;
        }
    }
}

