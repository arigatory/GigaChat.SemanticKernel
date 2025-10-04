# Troubleshooting RAG приложения

## Проблема: Приложение не использует загруженные PDF

### Симптомы
- Ассистент отвечает общими фразами типа "Уточните вопрос"
- Не ссылается на документы из `/wwwroot/Data`
- Нет цитат из PDF файлов

### Диагностика

#### 1. Проверьте наличие PDF файлов

```bash
ls -la ChatApp.Rag.GigaChat/wwwroot/Data/
```

Должны быть файлы:
- `Example_GPS_Watch.pdf`
- `Example_Emergency_Survival_Kit.pdf`
- Или ваши собственные PDF

#### 2. Проверьте логи при запуске

Запустите приложение и смотрите в консоли:

```bash
cd ChatApp.Rag.GigaChat
dotnet run
```

**Ожидаемые логи при старте:**

```
Starting ingestion from source: PDFDirectorySource:/path/to/Data
Found 0 existing documents in source          # Первый запуск
Found 2 new or modified documents to process
Processing Example_GPS_Watch.pdf
Created 15 chunks for Example_GPS_Watch.pdf    # Количество может отличаться
Successfully indexed 15 chunks for Example_GPS_Watch.pdf
Processing Example_Emergency_Survival_Kit.pdf
Created 12 chunks for Example_Emergency_Survival_Kit.pdf
Successfully indexed 12 chunks for Example_Emergency_Survival_Kit.pdf
Ingestion is up-to-date
```

**Если видите ошибки 413:**
- Это нормально при первой загрузке больших файлов
- Повторный запуск должен обработать файлы успешно

#### 3. Проверьте базу данных

```bash
ls -la ChatApp.Rag.GigaChat/bin/Debug/net9.0/vector-store.db
```

Файл должен существовать и иметь размер > 0 bytes

#### 4. Проверьте логи поиска

Задайте вопрос в чате, например: "расскажи про gps"

**Ожидаемые логи:**

```
Searching for: 'information about gps', documentIdFilter: 'none', maxResults: 5
Found 3 results for search 'information about gps'
Result: DocumentId=Example_GPS_Watch.pdf, Page=1, Text=GPS watch features...
Result: DocumentId=Example_GPS_Watch.pdf, Page=2, Text=Navigation using GPS...
```

**Если результатов 0:**
- Векторы не были созданы
- Проблема с embeddings API
- Размерность векторов не совпадает

### Решения

#### Решение 1: Пересоздать базу данных

```bash
# Остановите приложение
# Удалите старую базу
rm ChatApp.Rag.GigaChat/bin/Debug/net9.0/vector-store.db*

# Перезапустите
cd ChatApp.Rag.GigaChat
dotnet run
```

#### Решение 2: Проверить токен GigaChat

```bash
# Проверьте, что токен установлен
cd ChatApp.Rag.GigaChat
dotnet user-secrets list

# Должно показать:
# GigaChat:Token = YOUR-TOKEN

# Если нет, установите:
dotnet user-secrets set "GigaChat:Token" "YOUR-AUTHORIZATION-KEY"
```

#### Решение 3: Добавить свои PDF файлы

```bash
# Скопируйте свои PDF в папку Data
cp /path/to/your/*.pdf ChatApp.Rag.GigaChat/wwwroot/Data/

# Перезапустите приложение
```

#### Решение 4: Проверить размер PDF файлов

Слишком большие файлы могут вызывать проблемы:
- **Рекомендуется**: < 10 MB на файл
- **Максимум страниц**: ~50-100 страниц

Если файл большой, разбейте его на части.

### Проверка работы

После исправлений задайте конкретный вопрос:

```
Пользователь: "Какие функции есть у GPS часов из документа?"
```

**Правильный ответ должен содержать:**
- Конкретную информацию из PDF
- Цитаты в формате XML: `<citation filename='...' page_number='...'>...</citation>`

**Неправильный ответ:**
- "Уточните вопрос"
- "Не могу найти информацию"
- Общие фразы без ссылки на документ

### Дополнительная диагностика

#### Проверить embeddings напрямую

Создайте тестовый скрипт:

```csharp
using GigaChat.SemanticKernel;
using GigaChat.SemanticKernel.Models;

var token = "YOUR-TOKEN";
var client = new GigaChatClient(token, "Embeddings");

var request = new GigaChatEmbeddingsRequest
{
    Model = "Embeddings",
    Input = new List<string> { "test text" }
};

var response = await client.CreateEmbeddingsAsync(request, CancellationToken.None);
Console.WriteLine($"Got {response.Data.Count} embeddings");
Console.WriteLine($"Vector dimension: {response.Data[0].Embedding.Length}");
```

Ожидается:
```
Got 1 embeddings
Vector dimension: 1024
```

### Известные проблемы

1. **Ошибка 413 при первой загрузке** - исправлена в версии 1.1.1+
2. **Размерность векторов** - должна быть 1024 для GigaChat
3. **Токен истекает через 30 минут** - обновляется автоматически

## Нужна помощь?

Откройте issue на GitHub с:
- Логами приложения
- Описанием проблемы
- Версией .NET и ОС
