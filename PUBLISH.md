# Публикация NuGet пакета GigaChat.SemanticKernel

## Версия 1.2.0

Пакет успешно собран: `/Users/ivan/Work/GigaChat.SemanticKernel/bin/Release/GigaChat.SemanticKernel.1.2.0.nupkg`

## Шаги для публикации

### 1. Получить API ключ NuGet.org

1. Войдите на https://www.nuget.org/
2. Перейдите в Account → API Keys
3. Создайте новый API ключ или используйте существующий
4. Установите права: **Push** для пакета `GigaChat.SemanticKernel`
5. Скопируйте ключ (он показывается только один раз!)

### 2. Опубликовать пакет

```bash
# Перейдите в папку с проектом
cd /Users/ivan/Work/GigaChat.SemanticKernel

# Опубликуйте пакет (замените YOUR_API_KEY на ваш ключ)
dotnet nuget push bin/Release/GigaChat.SemanticKernel.1.2.0.nupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

### 3. Проверить публикацию

После публикации пакет появится на https://www.nuget.org/packages/GigaChat.SemanticKernel через несколько минут.

### 4. Альтернатива: Локальное тестирование

Если вы хотите протестировать пакет локально перед публикацией:

```bash
# Создайте локальную папку для NuGet пакетов
mkdir -p ~/local-nuget

# Скопируйте пакет
cp bin/Release/GigaChat.SemanticKernel.1.2.0.nupkg ~/local-nuget/

# В другом проекте добавьте источник
dotnet nuget add source ~/local-nuget --name LocalNuGet

# Установите пакет
dotnet add package GigaChat.SemanticKernel --version 1.2.0
```

## Что нового в версии 1.2.0

### Добавлено
- **Полная поддержка GigaChat Function Calling API** для RAG и агентов
- Реализован `GigaChatAIChatClient` с циклом обработки function calls (максимум 5 вызовов)
- Добавлена конвертация `AITool` в `AIFunction` для совместимости с Microsoft.Extensions.AI

### Исправлено
- **Исправлена ошибка 413** при создании embeddings:
  - Автоматический батчинг с лимитом 100 элементов
  - Обрезка текстов до 500 токенов (~2000 символов)
  - Уменьшен размер чанков PDF с 200 до 100 токенов
- Исправлена работа RAG - GigaChat теперь использует документы через Function Calling

### Изменено
- `GigaChatAIChatClient` теперь работает напрямую с GigaChat API
- Добавлено подробное логирование function calls
- Упрощён `Program.cs`

## Проверка перед публикацией

✅ Версия обновлена: 1.1.0 → 1.2.0  
✅ CHANGELOG.md обновлён  
✅ Release Notes добавлены в .csproj  
✅ README.md скопирован в src/  
✅ Пакет собран: GigaChat.SemanticKernel.1.2.0.nupkg (24KB)  
✅ Все тесты пройдены  
✅ Demo app работает с Function Calling  

## После публикации

1. Создайте Git tag:
```bash
git tag -a v1.2.0 -m "Release version 1.2.0 - Function Calling support"
git push origin v1.2.0
```

2. Создайте GitHub Release:
   - Перейдите на https://github.com/arigatory/GigaChat.SemanticKernel/releases/new
   - Tag: v1.2.0
   - Title: "v1.2.0 - Function Calling Support"
   - Description: Скопируйте из CHANGELOG.md раздел [1.2.0]
   - Прикрепите файл: GigaChat.SemanticKernel.1.2.0.nupkg

3. Обновите README.md в GitHub с примерами Function Calling

## Troubleshooting

### Ошибка: Package already exists
Если пакет с такой версией уже опубликован, нужно увеличить версию в .csproj и пересобрать.

### Ошибка: API key is invalid
Проверьте, что:
- API ключ скопирован полностью
- У ключа есть права Push для этого пакета
- Ключ не истёк

### Ошибка: Package validation failed
Проверьте:
- README.md существует в src/
- Все зависимости корректны
- PackageReleaseNotes заполнен
