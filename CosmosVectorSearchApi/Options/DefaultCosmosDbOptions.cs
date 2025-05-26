
namespace CosmosVectorSearchApi.Options
{
    using Microsoft.Extensions.Options;

    public class DefaultCosmosDbOptions : IConfigureOptions<CosmosDbOptions>
    {
        private readonly IConfiguration _configuration;
        public DefaultCosmosDbOptions(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public void Configure(CosmosDbOptions options)
        {
            options.Endpoint = _configuration.GetValue<string>($"{CosmosDbOptions.SectionName}:Endpoint") ?? string.Empty;
            options.ConnectionString = _configuration.GetValue<string>($"{CosmosDbOptions.SectionName}:ConnectionString") ?? string.Empty;
        }

    }
}

