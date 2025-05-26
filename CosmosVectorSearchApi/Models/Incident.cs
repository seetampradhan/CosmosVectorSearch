using Microsoft.Extensions.VectorData;

namespace CosmosVectorSearchApi.Models
{
    public class Incident
    {
        [VectorStoreKey]
        public string IncidentId { get; set; } = string.Empty;
        [VectorStoreData]
        public int Severity { get; set; }
        [VectorStoreData]
        public string Status { get; set; } = string.Empty;
        [VectorStoreData]
        public string OwningTeamName { get; set; } = string.Empty;
        [VectorStoreData]
        public string OwningContactAlias { get; set; } = string.Empty;
        [VectorStoreData]
        public string OwningContactName { get; set; } = string.Empty;
        [VectorStoreData]
        public string TsgId { get; set; } = string.Empty;
        [VectorStoreData]
        public string ResolveDate { get; set; } = string.Empty;
        [VectorStoreData]
        public string ResolvedBy { get; set; } = string.Empty;
        [VectorStoreData]
        public string Title { get; set; } = string.Empty;
        [VectorStoreData]
        public string Mitigation { get; set; } = string.Empty;
        [VectorStoreData]
        public string MitigateDate { get; set; } = string.Empty;
        [VectorStoreData]
        public string MitigatedBy { get; set; } = string.Empty;
        [VectorStoreData]
        public string HowFixed { get; set; } = string.Empty;
        [VectorStoreData]
        public string Summary { get; set; } = string.Empty;
        [VectorStoreVector(Dimensions:3, IndexKind = IndexKind.QuantizedFlat)]
        public ReadOnlyMemory<float> TittleEmbedding { get; set; }
        [VectorStoreVector(Dimensions:3, IndexKind = IndexKind.DiskAnn)]
        public ReadOnlyMemory<float> SummaryEmbedding { get; set; }
    }
}
