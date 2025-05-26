using System.ComponentModel.DataAnnotations;

namespace CosmosVectorSearchApi.Models
{
    /// <summary>
    /// Represents the parameters used for searching similar incidents
    /// </summary>
    public class IncidentSearchParameters
    {
        /// <summary>
        /// The incident to use as a search template
        /// </summary>
        [Required]
        public Incident Incident { get; set; }

        /// <summary>
        /// The database name
        /// </summary>
        [Required]
        public string DatabaseName { get; set; }

        /// <summary>
        /// The collection name
        /// </summary>
        public string CollectionName { get; set; } = "incidents";

        /// <summary>
        /// Weight for the title embedding
        /// </summary>
        public double TitleWeight { get; set; } = 0.7;

        /// <summary>
        /// Weight for the summary embedding
        /// </summary>
        public double SummaryWeight { get; set; } = 0.3;

        /// <summary>
        /// Maximum number of results to return
        /// </summary>
        public int MaxResults { get; set; } = 5;
    }
}
