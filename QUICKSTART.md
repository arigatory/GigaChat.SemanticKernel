# Быстрый старт

## Предварительные требования

- .NET 8 SDK или выше
- Ключ авторизации GigaChat API

## Получение ключа авторизации

1. Зайдите на [портал разработчиков GigaChat](https://developers.sber.ru/portal/products/gigachat-api)
2. Зарегистрируйтесь или войдите в свой аккаунт
3. Создайте новый проект API
4. Скопируйте ключ авторизации (Authorization Key)

Подробнее:
- [Быстрый старт для физических лиц](https://developers.sber.ru/docs/ru/gigachat/individuals-quickstart)
- [Быстрый старт для юридических лиц](https://developers.sber.ru/docs/ru/gigachat/legal-quickstart)

## Использование библиотеки

### 1. Установка пакета

```bash
dotnet add package GigaChat.SemanticKernel
```

### 2. Базовый пример

```csharp
using Microsoft.SemanticKernel;
using GigaChat.SemanticKernel;

// Создайте kernel builder
var builder = Kernel.CreateBuilder();

// Добавьте GigaChat
builder.AddGigaChatChatCompletion(
    authorizationKey: "ваш_ключ_авторизации",
    modelId: "GigaChat"
);

// Создайте kernel
var kernel = builder.Build();

// Используйте chat service
var chat = kernel.GetRequiredService<IChatCompletionService>();
var history = new ChatHistory();
history.AddUserMessage("Привет! Кто ты?");

var response = await chat.GetChatMessageContentsAsync(history);
Console.WriteLine(response[0].Content);
```

### 3. С использованием user secrets (рекомендуется)

```bash
# Установите ключ в user secrets
dotnet user-secrets init
dotnet user-secrets set "GigaChat:Token" "ваш_ключ_авторизации"
```

```csharp
// В коде используйте Configuration
var authKey = builder.Configuration["GigaChat:Token"];
builder.AddGigaChatChatCompletion(authKey, "GigaChat");
```

## Запуск демо-приложения

### 1. Настройка

```bash
cd ChatApp.Rag.GigaChat
dotnet user-secrets set "GigaChat:Token" "ваш_ключ_авторизации"
```

### 2. Запуск

```bash
dotnet run
```

### 3. Откройте браузер

Приложение запустится по адресу `https://localhost:5001` (или другому, указанному в консоли)

### 4. Добавление своих документов

1. Поместите PDF файлы в папку `wwwroot/Data/`
2. Перезапустите приложение
3. Документы будут автоматически проиндексированы

## Примеры использования

### Chat с streaming

```csharp
await foreach (var chunk in chat.GetStreamingChatMessageContentsAsync(history))
{
    Console.Write(chunk.Content);
}
```

### Embeddings

```csharp
builder.AddGigaChatTextEmbeddingGeneration(
    authorizationKey: authKey,
    modelId: "Embeddings" // или "EmbeddingsGigaR"
);

var embeddings = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
var vectors = await embeddings.GenerateEmbeddingsAsync(new[] { "Текст для векторизации" });
```

### Несколько моделей

```csharp
// Основная модель
builder.AddGigaChatChatCompletion(authKey, "GigaChat", serviceId: "basic");

// Продвинутая модель
builder.AddGigaChatChatCompletion(authKey, "GigaChat-Pro", serviceId: "pro");

// Использование
var basicChat = kernel.GetRequiredKeyedService<IChatCompletionService>("basic");
var proChat = kernel.GetRequiredKeyedService<IChatCompletionService>("pro");
```

## Troubleshooting

### Ошибка 401 Unauthorized

- Проверьте правильность ключа авторизации
- Убедитесь, что ключ не истек
- Проверьте scope (GIGACHAT_API_PERS, GIGACHAT_API_B2B, GIGACHAT_API_CORP)

### Ошибка при сборке

```bash
# Очистите и пересоберите
dotnet clean
dotnet build
```

### Проблемы с PDF

- Убедитесь, что PDF файлы не повреждены
- Проверьте размер файлов (максимум 40 МБ для текстовых документов)
- Убедитесь, что в PDF есть текстовое содержимое (не только изображения)

## Полезные ссылки

- [Документация GigaChat API](https://developers.sber.ru/docs/ru/gigachat/api/reference/rest/gigachat-api)
- [Модели GigaChat](https://developers.sber.ru/docs/ru/gigachat/models)
- [Тарифы и оплата](https://developers.sber.ru/docs/ru/gigachat/api/tariffs)
- [Semantic Kernel Docs](https://learn.microsoft.com/en-us/semantic-kernel/)
- [GitHub Repository](https://github.com/arigatory/GigaChat.SemanticKernel)

## Лицензия

MIT License - см. [LICENSE](LICENSE)
