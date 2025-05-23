# Cosmos Vector Search API

This project demonstrates how to use Azure Cosmos DB's vector search capabilities to implement semantic search and retrieval over structured data. It uses Ollama for local embedding generation and integrates with Azure Data Explorer (Kusto) for additional data querying capabilities.

## Overview

The CosmosVectorSearchApi provides:

- Vector embedding generation using Ollama locally
- Storage and retrieval of vector embeddings in Azure Cosmos DB
- Integration with Azure Data Explorer (Kusto) for advanced querying

## Prerequisites

- .NET 9.0 SDK
- Azure Cosmos DB account
- Azure Data Explorer (Kusto) instance (optional)
- Ollama installed locally for embedding generation

## Setup

### 1. Clone the repository

```powershell
git clone <repository-url>
cd CosmosVectorSearch
```

### 2. Configure application settings

Update `appsettings.json` and `appsettings.Development.json` with your Cosmos DB, Kusto, and Ollama settings:

```json
{
  "Cosmos": {
    "Endpoint": "your-cosmos-endpoint",
    "ConnectionString": "your-cosmos-connection-string",
    "DatabaseName": "your-database-name",
    "ContainerName": "your-container-name"
  },
  "Kusto": {
    "KustoUri": "your-kusto-uri",
    "KustoDatabase": "your-kusto-database",
    "TenantId": "your-tenant-id"
  },
  "Ollama": {
    "OllamaUri": "http://localhost:11434",
    "ModelName": "nomic-embed-text:latest"
  }
}
```

### 3. Build and run the application

```powershell
dotnet build
dotnet run --project CosmosVectorSearchApi
```

## Setting up Ollama Locally

This project uses Ollama to generate vector embeddings locally. Here's how to set it up:

### Installing Ollama

1. Download and install Ollama from the [official website](https://ollama.ai/download)
2. Verify the installation by running:

```powershell
ollama --version
```

### Setting up the embedding model

1. Pull the embedding model used by this project:

```powershell
ollama pull nomic-embed-text:latest
```

2. Verify the model is working:

```powershell
ollama run nomic-embed-text "This is a test"
```

### Configuration in the application

The application is configured to use Ollama at `http://localhost:11434` with the `nomic-embed-text:latest` model. If you need to change these settings, update the `Ollama` section in your appsettings files.

## Using the API

### Ingesting Data

The API supports ingesting data with vector embeddings through the `IngestDataAsync` method in the `VectorDbService`. This method:

1. Creates or gets the specified Cosmos DB container
2. Processes each item to generate embeddings (if they are of type `Incident`)
3. Upserts the items into the Cosmos DB container

### Searching Data

The `SearchAsync` method in the `VectorDbService` allows you to:

1. Generate an embedding for a query text
2. Perform a vector similarity search on the specified field
3. Return results as an async enumerable

### API Endpoints

The API exposes endpoints for working with incident data through the `IncidentController`. These endpoints allow you to:

- Ingest incidents with automatically generated embeddings
- Perform semantic searches over the incident data

## Project Structure

- `Clients/`: Contains clients for Cosmos DB and Kusto
- `Controllers/`: API controllers defining the endpoints
- `Extensions/`: Service registration and other extensions
- `Interfaces/`: Interfaces for dependency injection
- `Models/`: Data models including the `Incident` class
- `Options/`: Configuration options for various services
- `Services/`: Core service implementations including `VectorDbService` and `VectorEmbeddingService`

## Additional Notes

- The API uses the Microsoft.Extensions.AI library for vector operations
- Authentication and authorization are not included in this sample
- Rate limiting is implemented for embedding generation to prevent throttling

## Troubleshooting

### Common Issues with Ollama

1. **Ollama Service Not Running**: Ensure the Ollama service is running with:

```powershell
# Check if Ollama is running
Get-Process ollama -ErrorAction SilentlyContinue

# Start Ollama if it's not running
if (-not $?) { Start-Process ollama -WindowStyle Hidden }
```

2. **Model Not Found**: Ensure you've pulled the correct model:

```powershell
ollama list
```

3. **Connection Issues**: Check if you can connect to the Ollama API:

```powershell
Invoke-RestMethod -Uri "http://localhost:11434/api/tags"
```

4. **Slow Embedding Generation**: The first request may be slow as Ollama loads the model into memory. Subsequent requests should be faster.
