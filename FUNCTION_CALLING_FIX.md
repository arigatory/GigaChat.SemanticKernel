# Function Calling Fix

## Проблема
RAG не работал, потому что GigaChat не использовал Function Calling для поиска документов. Вместо этого приложение было изменено на Manual RAG (ручной поиск перед каждым запросом), что не является правильным подходом.

## Решение
Реализована полная поддержка Function Calling для GigaChat на основе примера в `/FunctionCall.GigaChat/`.

## Изменения

### 1. GigaChatAIChatClient.cs - Полная переработка
**Файл**: `/ChatApp.Rag.GigaChat/Services/GigaChatAIChatClient.cs`

Изменения:
- **Убрали зависимость от Semantic Kernel** - теперь напрямую работаем с GigaChat API
- **Добавили OAuth 2.0** - собственная реализация с `_tokenLock` для thread-safety
- **Реализовали цикл Function Calling** - аналогично примеру из `/FunctionCall.GigaChat/GigaChatClient.cs`:
  ```csharp
  while (true) {
      // 1. Отправить запрос в GigaChat
      var response = await SendRequestAsync(request, cancellationToken);
      
      // 2. Проверить, вызывает ли GigaChat функцию
      if (choice.Message.FunctionCall != null && tools?.Any() == true) {
          // 3. Найти функцию
          var tool = tools.FirstOrDefault(t => t.Name == functionName);
          
          // 4. Выполнить функцию
          var functionResult = await CallFunctionAsync(tool, functionArgs);
          
          // 5. Добавить assistant message с function_call
          assistantMessage.AdditionalProperties["function_call"] = ...;
          conversation.Add(assistantMessage);
          
          // 6. Добавить function result message
          functionMessage.AdditionalProperties["role"] = "function";
          functionMessage.AdditionalProperties["name"] = functionName;
          conversation.Add(functionMessage);
          
          // 7. Продолжить цикл
          continue;
      }
      
      // 8. Вернуть финальный ответ
      return new ChatResponse(...);
  }
  ```

- **Конвертация AITool → AIFunction** - для работы с `chatOptions.Tools`
- **GenerateGigaChatFunctions** - преобразование `AIFunction` в формат GigaChat API
- **Logging** - добавлены логи для отладки Function Calling

### 2. Chat.razor - Возврат к Function Calling
**Файл**: `/ChatApp.Rag.GigaChat/Components/Pages/Chat/Chat.razor`

Изменения:
- **Убрали Manual RAG** - удалён код с ручным вызовом `Search.SearchAsync`
- **Восстановили Tools** - `chatOptions.Tools = [AIFunctionFactory.Create(SearchAsync)]`
- **Упростили SystemPrompt** - убрали упоминание о "provided context"
- **Убрали ручную вставку context** - теперь GigaChat сам вызывает функцию поиска

### 3. Program.cs - Упрощение DI
**Файл**: `/ChatApp.Rag.GigaChat/Program.cs`

Изменения:
- **Убрали Semantic Kernel** - теперь не создаём `Kernel` и `IChatCompletionService`
- **Регистрируем GigaChatAIChatClient напрямую**:
  ```csharp
  builder.Services.AddSingleton<IChatClient>(serviceProvider => {
      var logger = serviceProvider.GetRequiredService<ILogger<GigaChatAIChatClient>>();
      return new GigaChatAIChatClient(
          authData: gigaChatToken,
          modelId: "GigaChat",
          logger: logger
      );
  });
  ```

## Архитектура Function Calling

```
User Question
      ↓
Chat.razor → chatOptions.Tools = [SearchAsync]
      ↓
GigaChatAIChatClient.GetResponseCoreAsync()
      ↓
while (true) {
      ↓
  BuildGigaChatRequest(conversation, tools) → GigaChatRequest with "functions" array
      ↓
  SendRequestAsync() → POST https://gigachat.devices.sberbank.ru/api/v1/chat/completions
      ↓
  GigaChatResponse with choice.Message.FunctionCall?
      ↓
  YES: FunctionCall != null
      ↓
      CallFunctionAsync(SearchAsync, arguments)
      ↓
      Add assistant message (with function_call)
      ↓
      Add function result message (role="function")
      ↓
      continue; // повторить цикл
      
  NO: FunctionCall == null
      ↓
      return ChatResponse with final answer
}
```

## GigaChat API Format

### Request
```json
{
  "model": "GigaChat",
  "messages": [
    {"role": "system", "content": "..."},
    {"role": "user", "content": "о чем мои документы?"}
  ],
  "functions": [
    {
      "name": "SearchAsync",
      "description": "Search through ingested documents",
      "parameters": {
        "type": "object",
        "properties": {
          "text": {
            "type": "string",
            "description": "The text to search for"
          },
          "documentIdFilter": {
            "type": "string"
          },
          "maxResults": {
            "type": "integer"
          }
        },
        "required": ["text"]
      }
    }
  ],
  "function_call": "auto"
}
```

### Response with Function Call
```json
{
  "choices": [
    {
      "message": {
        "role": "assistant",
        "content": "",
        "function_call": {
          "name": "SearchAsync",
          "arguments": "{\"text\":\"документы\",\"maxResults\":5}"
        }
      }
    }
  ]
}
```

### Next Request (with function result)
```json
{
  "model": "GigaChat",
  "messages": [
    {"role": "system", "content": "..."},
    {"role": "user", "content": "о чем мои документы?"},
    {
      "role": "assistant",
      "content": "",
      "function_call": {
        "name": "SearchAsync",
        "arguments": "{\"text\":\"документы\",\"maxResults\":5}"
      }
    },
    {
      "role": "function",
      "name": "SearchAsync",
      "content": "[{\"Text\":\"...\",\"PageNumber\":1,\"DocumentId\":\"Emergency_Survival_Kit.pdf\"}]"
    }
  ],
  "functions": [...],
  "function_call": "auto"
}
```

### Final Response
```json
{
  "choices": [
    {
      "message": {
        "role": "assistant",
        "content": "Ваши документы содержат информацию о...\n\n<citation filename='Emergency_Survival_Kit.pdf' page_number='1'>exact quote</citation>"
      }
    }
  ]
}
```

## Ключевые моменты

1. **maxFunctionCalls = 5** - защита от бесконечного цикла
2. **AdditionalProperties** - для хранения `function_call` и `role="function"`
3. **Logging** - важно видеть, когда вызываются функции:
   ```
   Function call requested: SearchAsync with args: {"text":"документы"}
   Function SearchAsync returned: [...]
   ```
4. **Thread-safety** - `SemaphoreSlim _tokenLock` для OAuth токена
5. **Конвертация типов** - `AITool` → `AIFunction` → `GigaChatFunction`

## Тестирование

1. Запустить приложение: `cd ChatApp.Rag.GigaChat && dotnet run`
2. Открыть http://localhost:5010
3. Задать вопрос: "Что есть в моих документах?"
4. В логах должно появиться:
   ```
   info: ChatApp.Rag.GigaChat.Services.GigaChatAIChatClient[0]
         Function call requested: SearchAsync with args: {"text":"..."}
   info: ChatApp.Rag.GigaChat.Services.SemanticSearch[0]
         Searching for: '...'
   info: ChatApp.Rag.GigaChat.Services.GigaChatAIChatClient[0]
         Function SearchAsync returned: [...]
   ```
5. GigaChat должен ответить с конкретной информацией из PDF и цитатами

## Что было неправильно

❌ **Manual RAG в Chat.razor**:
```csharp
// WRONG: Manually call search before sending to GigaChat
var searchResults = await Search.SearchAsync(userText);
var contextMessage = new ChatMessage(ChatRole.System, contextBuilder.ToString());
messages.Add(contextMessage);
messages.Add(userMessage);
```

✅ **Правильно - Function Calling**:
```csharp
// CORRECT: Let GigaChat decide when to search
chatOptions.Tools = [AIFunctionFactory.Create(SearchAsync)];
// GigaChat will call SearchAsync when needed
```

## Результат

- ✅ RAG теперь работает через Function Calling
- ✅ GigaChat сам решает, когда искать документы
- ✅ Поддержка цепочки вызовов функций (до 5)
- ✅ Правильные логи для отладки
- ✅ Thread-safe OAuth 2.0
- ✅ Нет зависимости от Semantic Kernel в runtime (только для компиляции библиотеки)
