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
            services.AddSingleton<IVectorDbService, VectorDbServices>();
            services.AddSingleton<BaseOllamaEmbeddingService>();

            return services;
        }
    }
}

