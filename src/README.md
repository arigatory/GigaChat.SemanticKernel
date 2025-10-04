# GigaChat.SemanticKernel

[![NuGet](https://img.shields.io/nuget/v/GigaChat.SemanticKernel.svg)](https://www.nuget.org/packages/GigaChat.SemanticKernel)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Официальная интеграция GigaChat AI для Microsoft Semantic Kernel. Этот пакет позволяет легко использовать модели GigaChat от Сбербанка в приложениях на базе Semantic Kernel.

## Возможности

- **Chat Completion**: Полная реализация `IChatCompletionService` с поддержкой streaming
- **Text Generation**: Поддержка `ITextGenerationService`
- **Text Embeddings**: Поддержка `ITextEmbeddingGenerationService` с моделями Embeddings и EmbeddingsGigaR
- **Автоматическое управление токенами**: Встроенное обновление access token
- **Поддержка нескольких моделей**: GigaChat, GigaChat-Plus, GigaChat-Pro и др.

## Установка

```bash
dotnet add package GigaChat.SemanticKernel
```

## Быстрый старт

```csharp
using Microsoft.SemanticKernel;
using GigaChat.SemanticKernel;

var builder = Kernel.CreateBuilder();

// Настройка GigaChat с вашим ключом авторизации
builder.AddGigaChatChatCompletion(
    authorizationKey: "your_authorization_key_here",
    modelId: "GigaChat" // По умолчанию: GigaChat
);

var kernel = builder.Build();

// Пример чата
var chat = kernel.GetRequiredService<IChatCompletionService>();
var history = new ChatHistory();
history.AddUserMessage("Привет! Расскажи о себе.");

var response = await chat.GetChatMessageContentsAsync(history);
Console.WriteLine(response[0].Content);
```

## Аутентификация

1. Получите ключ авторизации на [портале разработчиков GigaChat](https://developers.sber.ru/portal/products/gigachat-api)
2. Используйте user-secrets для безопасного хранения:

```bash
dotnet user-secrets set GigaChat:Token YOUR_AUTHORIZATION_KEY
```

3. Загрузите в коде:

```csharp
var authKey = builder.Configuration["GigaChat:Token"];
builder.AddGigaChatChatCompletion(authKey);
```

## Расширенная настройка

### Streaming ответов

```csharp
var chat = kernel.GetRequiredService<IChatCompletionService>();
var history = new ChatHistory();
history.AddUserMessage("Напиши длинную историю");

await foreach (var chunk in chat.GetStreamingChatMessageContentsAsync(history))
{
    Console.Write(chunk.Content);
}
```

### Text Embeddings

```csharp
builder.AddGigaChatTextEmbeddingGeneration(
    authorizationKey: authKey,
    modelId: "Embeddings" // или "EmbeddingsGigaR" для более продвинутой модели
);

var embeddings = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
var vectors = await embeddings.GenerateEmbeddingsAsync(new[] { "Текст для векторизации" });
```

### Использование нескольких моделей

```csharp
// Основная модель для чата
builder.AddGigaChatChatCompletion(authKey, "GigaChat", serviceId: "chat");

// Продвинутая модель для сложных задач
builder.AddGigaChatChatCompletion(authKey, "GigaChat-Pro", serviceId: "pro");

// Embeddings
builder.AddGigaChatTextEmbeddingGeneration(authKey, "Embeddings", serviceId: "embeddings");
```

### Настройка параметров генерации

```csharp
var settings = new PromptExecutionSettings
{
    ExtensionData = new Dictionary<string, object>
    {
        ["temperature"] = 0.7,
        ["top_p"] = 0.9,
        ["max_tokens"] = 2000,
        ["repetition_penalty"] = 1.1
    }
};

var response = await chat.GetChatMessageContentsAsync(history, settings);
```

## Поддерживаемые модели

### Модели для генерации
| Model ID | Описание |
|----------|----------|
| `GigaChat` | Базовая модель |
| `GigaChat-Plus` | Улучшенная версия |
| `GigaChat-Pro` | Наиболее продвинутая модель |

### Модели для эмбеддингов
| Model ID | Описание | Размерность |
|----------|----------|-------------|
| `Embeddings` | Базовая модель эмбеддингов | 1024 |
| `EmbeddingsGigaR` | Продвинутая модель с большим контекстом | 1024 |

## API Reference

Все endpoints следуют официальной [документации GigaChat API](https://developers.sber.ru/docs/ru/gigachat/api/reference/rest/gigachat-api).

### Основные методы

- `AddGigaChatChatCompletion()` - Добавляет сервис chat completion
- `AddGigaChatTextGeneration()` - Добавляет сервис text generation  
- `AddGigaChatTextEmbeddingGeneration()` - Добавляет сервис для embeddings
- `AddGigaChat()` - Добавляет chat completion и text generation вместе

## Примеры использования

Полный пример RAG-приложения с использованием GigaChat доступен в папке `ChatApp.Rag.GigaChat`.

## Ограничения

- Требуется .NET 8+
- Access token обновляется автоматически (действителен 30 минут)
- Поддерживается chat completion, text generation и embeddings

## Полезные ссылки

- [Документация GigaChat API](https://developers.sber.ru/docs/ru/gigachat/api/reference/rest/gigachat-api)
- [Быстрый старт для физических лиц](https://developers.sber.ru/docs/ru/gigachat/individuals-quickstart)
- [Быстрый старт для юридических лиц](https://developers.sber.ru/docs/ru/gigachat/legal-quickstart)
- [Модели GigaChat](https://developers.sber.ru/docs/ru/gigachat/models)

## Лицензия

MIT License - см. [LICENSE](LICENSE) для деталей.


## Contributing

Pull requests are welcome! For major changes, please open an issue first.

## License

MIT
