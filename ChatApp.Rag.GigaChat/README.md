# AI Chat with GigaChat - RAG Demo

This project demonstrates how to build a RAG (Retrieval-Augmented Generation) chat application using GigaChat AI models. The application allows users to chat with custom data stored in PDF documents.

> [!NOTE]
> This demo uses the GigaChat.SemanticKernel NuGet package to integrate GigaChat API with Semantic Kernel.

## Features

- **Chat with GigaChat models** - Uses GigaChat for natural language understanding and generation
- **Vector embeddings** - Uses GigaChat Embeddings API for semantic search
- **RAG (Retrieval-Augmented Generation)** - Searches through PDF documents to provide context-aware answers
- **Streaming responses** - Real-time token streaming for better UX

## Configure the GigaChat API

To use GigaChat models, you need to:

1. Get your GigaChat authorization key from [GigaChat Developers Portal](https://developers.sber.ru/portal/products/gigachat-api)
2. Configure your token using .NET User Secrets:

```sh
cd ChatApp.Rag.GigaChat
dotnet user-secrets set GigaChat:Token YOUR-GIGACHAT-TOKEN
```

> Learn more about getting started with GigaChat:
> - [Quick Start for Individuals](https://developers.sber.ru/docs/ru/gigachat/individuals-quickstart)
> - [Quick Start for Legal Entities](https://developers.sber.ru/docs/ru/gigachat/legal-quickstart)

## How to Run

1. Configure your GigaChat token (see above)
2. Build and run the application:

```sh
dotnet build
dotnet run
```

3. Open your browser and navigate to the URL shown in the console (typically https://localhost:5001)

## Project Structure

- **Services/** - Contains GigaChat integration services
  - `GigaChatAIChatClient.cs` - Adapter for Microsoft.Extensions.AI chat interface
  - `GigaChatEmbeddingGenerator.cs` - Adapter for embeddings generation
  - `SemanticSearch.cs` - Vector search implementation
  - `Ingestion/` - PDF document processing and chunking
- **Components/** - Blazor UI components
- **wwwroot/Data/** - PDF documents to be indexed

## Available Models

### Chat Models
- `GigaChat` - Base model
- `GigaChat-Plus` - Enhanced version
- `GigaChat-Pro` - Most capable model

### Embedding Models
- `Embeddings` - Base embedding model (default)
- `EmbeddingsGigaR` - Advanced model with larger context window

## Customization

You can change the models in `Program.cs`:

```csharp
// Change chat model
kernelBuilder.AddGigaChatChatCompletion(
    authorizationKey: gigaChatToken,
    modelId: "GigaChat-Pro"  // or "GigaChat-Plus"
);

// Change embedding model
var embeddingGenerator = new GigaChatEmbeddingGenerator(
    gigaChatToken, 
    "EmbeddingsGigaR"  // or "Embeddings"
);
```

## Learn More

- [GigaChat API Documentation](https://developers.sber.ru/docs/ru/gigachat/api/reference/rest/gigachat-api)
- [GigaChat Models](https://developers.sber.ru/docs/ru/gigachat/models)
- [Vector Embeddings Guide](https://developers.sber.ru/docs/ru/gigachat/guides/embeddings)
- [GigaChat.SemanticKernel on GitHub](https://github.com/arigatory/GigaChat.SemanticKernel)


