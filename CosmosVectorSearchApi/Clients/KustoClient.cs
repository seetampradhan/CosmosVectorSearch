using Microsoft.Extensions.Options;
using Kusto.Data.Net.Client;
using Kusto.Data;
using CosmosVectorSearchApi.Options;
using CosmosVectorSearchApi.Interfaces;

namespace CosmosVectorSearchApi.Clients
{
    /// <summary>
    /// Client for interacting with Azure Data Explorer (Kusto)
    /// </summary>
    public class KustoClient : IKustoClient
    {
        private readonly IOptions<KustoOptions> _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="KustoClient"/> class.
        /// </summary>
        /// <param name="options">The Kusto options.</param>
        public KustoClient(IOptions<KustoOptions> options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }
        public async Task<List<T>> Query<T>(string query, string database)
        {
            var kcsb = new KustoConnectionStringBuilder(_options.Value.KustoUri).WithAadUserPromptAuthentication(_options.Value.TenantId);
            using (var client = KustoClientFactory.CreateCslQueryProvider(kcsb))
            {
                using (var reader = await client.ExecuteQueryAsync(database, query, null))
                {
                    var results = new List<T>();
                    var type = typeof(T);

                    // Dictionary case - keep existing implementation
                    if (type == typeof(Dictionary<string, object>))
                    {
                        var dictResults = new List<Dictionary<string, object>>();
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var columnName = reader.GetName(i);
                                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                row[columnName] = value;
                            }
                            dictResults.Add(row);
                        }
                        return dictResults as List<T>;
                    }

                    // Get all properties of type T
                    var properties = type.GetProperties()
                        .Where(p => p.CanWrite)
                        .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

                    // Process each row
                    while (reader.Read())
                    {
                        // Create a new instance of T
                        var instance = Activator.CreateInstance<T>();

                        // Map each column to a property
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var columnName = reader.GetName(i);

                            // Check if type T has a property matching the column name
                            if (properties.TryGetValue(columnName, out var property))
                            {
                                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);

                                // Only set the value if it's not null or if the property type is nullable
                                if (value != null || property.PropertyType.IsClass || Nullable.GetUnderlyingType(property.PropertyType) != null)
                                {
                                    // Convert the value to the property type if needed
                                    if (value != null && property.PropertyType != value.GetType() &&
                                        value is IConvertible && property.PropertyType.IsInstanceOfType(value) == false)
                                    {
                                        try
                                        {
                                            value = Convert.ChangeType(value, Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);
                                        }
                                        catch
                                        {
                                            // Skip setting this property if conversion fails
                                            continue;
                                        }
                                    }
                                    property.SetValue(instance, value);
                                }
                            }
                        }
                        results.Add(instance);
                    }

                    return results;
                }
            }
        }
    }
}