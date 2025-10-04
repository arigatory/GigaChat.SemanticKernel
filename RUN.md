# Как запустить демо-приложение ChatApp.Rag.GigaChat

## Предварительные требования

- .NET 9.0 SDK
- GigaChat Authorization Key (токен авторизации)

## 1. Получите токен авторизации GigaChat

Зарегистрируйтесь и получите токен (authorization key):

- **Для физических лиц**: <https://developers.sber.ru/docs/ru/gigachat/individuals-quickstart>
- **Для юридических лиц**: <https://developers.sber.ru/docs/ru/gigachat/legal-quickstart>

После регистрации вы получите **Authorization Key** (не путайте с Access Token!).

## 2. Настройте токен

Перейдите в папку демо-приложения и сохраните токен в user-secrets:

```bash
cd ChatApp.Rag.GigaChat
dotnet user-secrets set "GigaChat:Token" "ВАШ-AUTHORIZATION-KEY"
```

## 3. Соберите решение

```bash
cd /Users/ivan/Work/GigaChat.SemanticKernel
dotnet build
```

Убедитесь, что сборка прошла успешно:

- ✅ `GigaChat.SemanticKernel` - NuGet библиотека собрана
- ✅ `ChatApp.Rag.GigaChat` - демо-приложение собрано

## 4. Запустите приложение

```bash
cd ChatApp.Rag.GigaChat
dotnet run
```

Или из корня решения:

```bash
dotnet run --project ChatApp.Rag.GigaChat/ChatApp.Rag.GigaChat.csproj
```

## 5. Откройте в браузере

После запуска откройте URL, который появится в консоли (обычно <https://localhost:5001>)

## 6. Используйте RAG возможности

### Загрузите документы

1. Перейдите на вкладку "Documents"
2. Загрузите PDF файлы через интерфейс
3. Документы будут автоматически разбиты на фрагменты и проиндексированы

### Задавайте вопросы

1. Перейдите на вкладку "Chat"
2. Задайте вопрос о загруженных документах
3. Система найдёт релевантные фрагменты и передаст их в GigaChat для формирования ответа

## Технические детали

- **Версия Semantic Kernel**: 1.61.0
- **Версия Microsoft.Extensions.AI**: 9.9.1
- **Первый запуск**: Создаётся база данных SQLite для векторного хранилища
- **Размерность векторов**: 1024 (модель Embeddings)
- **Модель чата по умолчанию**: GigaChat
- **Token lifetime**: 30 минут (автоматически обновляется)

## Устранение неполадок

### Ошибка 401 Unauthorized

- Убедитесь, что токен сохранён правильно
- Проверьте, что используете Authorization Key, а не Access Token
- Токен действителен и не истёк

### Ошибка сборки

```bash
# Очистите и пересоберите
dotnet clean
dotnet restore
dotnet build
```

### База данных не создаётся

Убедитесь, что установлено расширение SQLite для векторного поиска:

- Проект использует `Microsoft.SemanticKernel.Connectors.Sqlite`
- База создаётся автоматически при первом запуске

### Несовместимость версий

Проект требует:

- `Microsoft.SemanticKernel` 1.61.0
- `Microsoft.Extensions.AI` 9.9.1
- .NET 9.0 для демо-приложения
- .NET 8.0 для библиотеки

## Дополнительная информация

Подробнее о конфигурации и использовании читайте в:

- [README.md](README.md) - Основная документация пакета
- [QUICKSTART.md](QUICKSTART.md) - Быстрый старт
- [ChatApp.Rag.GigaChat/README.md](ChatApp.Rag.GigaChat/README.md) - Документация демо-приложения

