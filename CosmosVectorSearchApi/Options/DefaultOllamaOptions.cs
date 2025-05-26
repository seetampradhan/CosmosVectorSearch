using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;

namespace CosmosVectorSearchApi.Options
{
    public class DefaultOllamaOptions : IConfigureOptions<OllamaOptions>
    {
        private readonly IConfiguration _configuration;
        
        public DefaultOllamaOptions(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        public void Configure(OllamaOptions options)
        {
            options.OllamaUri = _configuration.GetValue<string>($"{OllamaOptions.SectionName}:OllamaUri") ?? "http://localhost:11434";
            options.ModelName = _configuration.GetValue<string>($"{OllamaOptions.SectionName}:ModelName") ?? string.Empty;
        }
    }
}
