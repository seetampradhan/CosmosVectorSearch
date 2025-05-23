using Microsoft.Extensions.Options;

namespace CosmosVectorSearchApi.Options
{
    public class DefaultOllamaOptions : IConfigureOptions<OllamaOptions>
    {
        public void Configure(OllamaOptions options)
        {
            // Configure default values if needed, or rely on appsettings.json
        }
    }
}
