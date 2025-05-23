namespace CosmosVectorSearchApi.Options
{
    public class OllamaOptions
    {
        public const string SectionName = "Ollama";

        public string OllamaUri { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
    }
}
