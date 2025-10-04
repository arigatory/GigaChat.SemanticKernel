# Changelog

## [1.2.0] - 2025-10-04

### Added

- **Полная поддержка GigaChat Function Calling API** для RAG и агентов
- Реализован `GigaChatAIChatClient` с циклом обработки function calls (максимум 5 вызовов)
- Добавлена конвертация `AITool` в `AIFunction` для совместимости с Microsoft.Extensions.AI
- Демо приложение `ChatApp.Rag.GigaChat` теперь использует Function Calling для семантического поиска

### Fixed

- **Исправлена ошибка 413 (Request Entity Too Large)** при создании embeddings:
  - Добавлен автоматический батчинг с лимитом 100 элементов на запрос
  - Добавлена обрезка текстов до 500 токенов (~2000 символов)
  - Уменьшен размер чанков PDF с 200 до 100 токенов
- Улучшена обработка ошибок в `GigaChatClient.CreateEmbeddingsAsync` с более информативными сообщениями
- Исправлена работа RAG - теперь GigaChat использует документы через Function Calling вместо общих знаний

### Changed

- `GigaChatAIChatClient` теперь работает напрямую с GigaChat API без Semantic Kernel в runtime
- Добавлено подробное логирование function calls для отладки
- Упрощён `Program.cs` - убрана зависимость от Semantic Kernel Kernel

### Deprecated

- `GigaChatTextEmbeddingGenerationService` помечен как устаревший - рекомендуется использовать `Microsoft.Extensions.AI.IEmbeddingGenerator`
- `AddGigaChatTextEmbeddingGeneration` помечен как устаревший - рекомендуется использовать новый API

### Documentation

- Добавлен `FUNCTION_CALLING_FIX.md` с подробным описанием реализации Function Calling
- Обновлён `FIX_413_ERROR.md` с трёхуровневой защитой от ошибок

## [Unreleased]

## [1.1.0] - 2025-10-04

### Changed

- **BREAKING**: Обновлена зависимость `Microsoft.SemanticKernel` с версии 1.10.0 до **1.61.0**
- Обновлены адаптеры для совместимости с `Microsoft.Extensions.AI` 9.9.1
- Исправлены интерфейсы `IChatClient` и `IEmbeddingGenerator` под новое API

### Fixed

- Исправлена ошибка компиляции в `GigaChatEmbeddingGenerator.cs` - добавлен alias для разрешения конфликта namespaces
- Исправлено использование `ChatResponseUpdate` вместо устаревшего `StreamingChatResponseUpdate`
- Исправлено создание `ChatClientMetadata` и `EmbeddingGeneratorMetadata` под новый конструктор

### Technical

- Библиотека теперь полностью совместима с Semantic Kernel 1.61.0
- Демо-приложение `ChatApp.Rag.GigaChat` использует .NET 9.0
- Основная библиотека остаётся на .NET 8.0 для обратной совместимости

## [1.0.2] - 2025-10-03

### Added

- Полная поддержка Embeddings API GigaChat
- Реализован `GigaChatTextEmbeddingGenerationService` для Semantic Kernel
- Добавлен `GigaChatEmbeddingGenerator` - адаптер для Microsoft.Extensions.AI
- Демо-приложение `ChatApp.Rag.GigaChat` с RAG функциональностью

### Changed

- Обновлена документация с примерами использования embeddings
- Добавлены модели: `Embeddings` и `EmbeddingsGigaR` (размерность 1024)

## [1.0.1] - 2025-10-02

### Added

- Streaming support для chat completions
- Реализован `GetStreamingChatMessageContentsAsync`
- Парсер Server-Sent Events для потоковой передачи

### Fixed

- Исправлена схема OAuth 2.0 аутентификации
- Исправлено получение access token через POST к `/api/v2/oauth`
- Исправлено использование Basic Auth с Authorization Key
- Исправлен парсинг `expires_at` (используются миллисекунды, не секунды)

## [1.0.0] - 2025-10-01

### Added

- Первая версия интеграции GigaChat с Semantic Kernel
- `GigaChatClient` - HTTP клиент для работы с GigaChat API
- `GigaChatChatCompletionService` - реализация IChatCompletionService
- `GigaChatTextGenerationService` - реализация ITextGenerationService
- Extension methods для Dependency Injection
- Поддержка моделей: GigaChat, GigaChat-Plus, GigaChat-Pro
- Полная документация на русском языке

### Technical

- Целевой фреймворк: .NET 8.0
- Зависимости: Microsoft.SemanticKernel 1.10.0
- OAuth 2.0 аутентификация с автоматическим обновлением токена
- Thread-safe token management с SemaphoreSlim
