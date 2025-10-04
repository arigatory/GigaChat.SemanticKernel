# GigaChat.SemanticKernel

[![NuGet](https://img.shields.io/nuget/v/GigaChat.SemanticKernel.svg)](https://www.nuget.org/packages/GigaChat.SemanticKernel)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Official GigaChat AI integration for Microsoft Semantic Kernel. This package enables seamless use of Sberbank's GigaChat models in Semantic Kernel applications.

## Features

- **Chat Completion**: `IChatCompletionService` implementation
- **Text Generation**: `ITextGenerationService` support
- **Text Embeddings**: `ITextEmbeddingGenerationService` (coming soon)
- **Token Counting**: Built-in token management
- **Multi-Model Support**: GigaChat-Pro, GigaChat-Max, etc.

## Installation

```bash
dotnet add package GigaChat.SemanticKernel
```

## Quick Start

```csharp
using Microsoft.SemanticKernel;
using GigaChat.SemanticKernel;

var builder = Kernel.CreateBuilder();

// Configure GigaChat with your API key
builder.AddGigaChat(
    apiKey: "your_api_key_here",
    modelId: "GigaChat-Pro", // Optional (default: GigaChat-Pro)
    endpoint: "https://gigachat.devices.sberbank.ru/api/v1" // Optional
);

var kernel = builder.Build();

// Chat example
var chat = kernel.GetRequiredService<IChatCompletionService>();
var history = new ChatHistory();
history.AddUserMessage("Hello, who are you?");

var response = await chat.GetChatMessageContentAsync(history);
Console.WriteLine(response.Content);
```

## Authentication

1. Get your API key from [GigaChat Developers Portal](https://developers.sber.ru/portal/products/gigachat-api)
2. Pass it to `AddGigaChat()`:

```csharp
builder.AddGigaChat("your_api_key_here");
```

## Advanced Configuration

### Custom HttpClient

```csharp
builder.AddGigaChat(
    apiKey: "your_key",
    httpClient: new HttpClient() { Timeout = TimeSpan.FromSeconds(120) }
);
```

### Multiple Models

```csharp
// Primary model
builder.AddGigaChat("main_api_key", modelId: "GigaChat-Pro");

// Secondary model
builder.Services.AddKeyedSingleton<IChatCompletionService>(
    "GigaChat-Max",
    (_,_) => new GigaChatClient("secondary_key", "GigaChat-Max").CreateChatService()
);
```

## API Reference

### Supported Models

| Model ID          | Description                     |
|-------------------|---------------------------------|
| `GigaChat`        | Base model                     |
| `GigaChat-Pro`    | Advanced version (default)     |
| `GigaChat-Max`    | Highest capability model       |
| `Embeddings`      | Text embedding model           |

### Endpoints

All endpoints follow the official [GigaChat API documentation](https://developers.sber.ru/docs/ru/gigachat/api/reference/rest/overview).

## Limitations

- Requires .NET 8+
- Currently only supports chat completion and text generation
- Token refresh not yet implemented

## Contributing

Pull requests are welcome! For major changes, please open an issue first.

## License

MIT
