namespace CosmosVectorSearchApi.Options
{
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Configuration;

    public class DefaultKustoOptions : IConfigureOptions<KustoOptions>
    {
        private readonly IConfiguration _configuration;
        public DefaultKustoOptions(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public void Configure(KustoOptions options)
        {
            options.KustoUri = _configuration.GetValue<string>($"{KustoOptions.SectionName}:KustoUri") ?? string.Empty;
            options.KustoDatabase = _configuration.GetValue<string>($"{KustoOptions.SectionName}:KustoDatabase") ?? string.Empty;
            options.TenantId = _configuration.GetValue<string>($"{KustoOptions.SectionName}:TenantId") ?? string.Empty;
        }
    }
}