#!/bin/bash

echo "🔍 Диагностика RAG приложения"
echo "=============================="
echo ""

echo "1. Проверка PDF файлов в Data/"
ls -lh ChatApp.Rag.GigaChat/wwwroot/Data/*.pdf
echo ""

echo "2. Проверка базы данных"
if [ -f "ChatApp.Rag.GigaChat/bin/Debug/net9.0/vector-store.db" ]; then
    ls -lh ChatApp.Rag.GigaChat/bin/Debug/net9.0/vector-store.db
    echo "✅ База данных существует"
else
    echo "❌ База данных НЕ существует - нужно запустить приложение"
fi
echo ""

echo "3. Удаление старой базы (если нужно пересоздать)"
read -p "Удалить базу данных и пересоздать? (y/N): " answer
if [ "$answer" = "y" ] || [ "$answer" = "Y" ]; then
    rm -f ChatApp.Rag.GigaChat/bin/Debug/net9.0/vector-store.db*
    echo "✅ База данных удалена"
fi
echo ""

echo "4. Запуск приложения с подробным логированием"
echo "Смотрите на логи при запуске - должны увидеть индексацию PDF"
echo "Нажмите Ctrl+C для остановки"
echo ""

cd ChatApp.Rag.GigaChat
dotnet run
