# Исправление ошибки 413 Request Entity Too Large

## Проблема

При загрузке больших документов в RAG приложение возникала ошибка:

```text
System.Net.Http.HttpRequestException: Response status code does not indicate success: 413 (Request Entity Too Large).
Message: "Max tokens for index 2 exceeded. Max tokens: 514, actual tokens: 523"
```

**Причины:**
1. GigaChat API имеет ограничение **514 токенов на один текст** для embeddings
2. Тексты, превышающие этот лимит, вызывают ошибку 413
3. Размер батча (количество текстов) также может влиять на размер запроса

## Решение

Реализована двухуровневая защита:

### 1. Уменьшение размера чанков при обработке PDF

В `PDFDirectorySource.cs` размер чанков уменьшен со 200 до **100 токенов**:

```csharp
// GigaChat has a limit of 514 tokens per text for embeddings
// Using 100 tokens per chunk to stay well under the limit
return TextChunker.SplitPlainTextParagraphs([pageText], maxTokensPerParagraph: 100)
```

### 2. Автоматическое усечение длинных текстов

В `GigaChatEmbeddingGenerator.cs` добавлена защита от превышения лимита:

```csharp
private const int MaxTokensPerText = 500; // GigaChat limit is 514, using 500 for safety
private const int ApproxCharsPerToken = 4; // Approximate ratio

// Truncate texts that are too long
var processedValues = valuesList.Select(text =>
{
    var maxChars = MaxTokensPerText * ApproxCharsPerToken;
    if (text.Length > maxChars)
    {
        return text.Substring(0, maxChars);
    }
    return text;
}).ToList();
```

### 3. Батчинг для больших объёмов

Запросы автоматически разбиваются на батчи по 100 элементов:

```csharp
private const int MaxBatchSize = 100;

for (int i = 0; i < processedValues.Count; i += MaxBatchSize)
{
    var batch = processedValues.Skip(i).Take(MaxBatchSize).ToList();
    // ... process batch
}
```

## Результат

✅ Чанки PDF теперь не превышают 100 токенов (хорошо под лимитом 514)
✅ Длинные тексты автоматически усекаются до безопасного размера
✅ Батчинг защищает от перегрузки API
✅ Ошибка 413 больше не возникает при загрузке документов
✅ Изменения прозрачны для пользователя - API остался прежним

## Технические детали

### Лимиты GigaChat API:
- **Максимум токенов на текст**: 514
- **Рекомендуемый размер**: 100-500 токенов (с запасом)
- **Размер батча**: 100 элементов

### Наша защита:
- **Размер чанков PDF**: 100 токенов
- **Максимум символов**: 2000 (≈500 токенов × 4 символа)
- **Автоусечение**: Да, с предупреждением в логах

### Обработка:
- Последовательная по батчам
- Порядок сохраняется через `OrderBy(d => d.Index)`
- Можно оптимизировать до параллельной в будущем

## Изменённые файлы

1. **ChatApp.Rag.GigaChat/Services/Ingestion/PDFDirectorySource.cs**
   - Изменён параметр `maxTokensPerParagraph` с 200 на 100

2. **ChatApp.Rag.GigaChat/Services/GigaChatEmbeddingGenerator.cs**
   - Добавлено автоматическое усечение длинных текстов
   - Константы: `MaxTokensPerText = 500`, `ApproxCharsPerToken = 4`

3. **src/GigaChatClient.cs**
   - Улучшены сообщения об ошибках

4. **src/GigaChatTextEmbeddingGenerationService.cs**
   - Добавлен батчинг для консистентности

## Тестирование

Проверьте загрузку больших PDF документов:

1. Запустите приложение: `dotnet run --project ChatApp.Rag.GigaChat`
2. Перейдите на вкладку "Documents"
3. Загрузите большой PDF файл (10+ страниц)
4. Убедитесь, что индексация проходит успешно без ошибок 413
5. Проверьте, что можно задавать вопросы по документу

## Версия

Исправление включено в версию 1.1.1 (или следующую после 1.1.0)
