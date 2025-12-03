# PhotoBank Console

Консольное приложение для пакетной обработки фотографий в PhotoBank.

## Описание

PhotoBank Console - это инструмент командной строки для:
- Синхронизации и обработки фотографий из хранилищ
- Регистрации персон для распознавания лиц
- Автоматического обогащения фотографий метаданными, миниатюрами, AI-анализом
- Слияния дубликатов фотографий по значению `imageHash`

## Использование

### Основные команды

```bash
# Показать справку
PhotoBank.Console --help

# Только регистрация персон (по умолчанию)
PhotoBank.Console

# Обработка файлов из хранилища с ID 5
PhotoBank.Console --storage 5
PhotoBank.Console -s 5

# Обработка без регистрации персон
PhotoBank.Console --storage 5 --no-register

# Пропустить регистрацию персон
PhotoBank.Console --no-register

# Слить дубликаты по imageHash
PhotoBank.Console merge-duplicates
```

### Опции командной строки

| Опция | Краткая форма | Описание |
|-------|---------------|----------|
| `--storage <id>` | `-s <id>` | ID хранилища для обработки файлов |
| `--no-register` | - | Пропустить шаг регистрации персон |
| `--help` | `-h` | Показать справку |

## Коды возврата

Приложение возвращает следующие коды выхода:

| Код | Значение |
|-----|----------|
| 0 | Успешное выполнение |
| 1 | Общая ошибка приложения |
| 2 | Ошибка конфигурации |
| 3 | Хранилище не найдено |
| 4 | Частичный сбой (некоторые файлы обработаны с ошибками) |
| 130 | Операция отменена пользователем (Ctrl+C) |

## Конфигурация

Настройки приложения хранятся в `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Photobank;..."
  },
  "Processing": {
    "MaxDegreeOfParallelism": 4
  },
  "ComputerVision": {
    "Key": "your-azure-cv-key",
    "Endpoint": "https://your-region.api.cognitive.microsoft.com/"
  },
  "Face": {
    "Key": "your-azure-face-key",
    "Endpoint": "https://your-region.api.cognitive.microsoft.com/"
  },
  "CheckDuplicates": true,
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

### Рекомендации по безопасности

**Не храните секреты в appsettings.json!**

Используйте один из следующих методов:

#### Для разработки: User Secrets

```bash
dotnet user-secrets init
dotnet user-secrets set "ComputerVision:Key" "your-key"
dotnet user-secrets set "Face:Key" "your-key"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"
```

#### Для production: Environment Variables

```bash
export ComputerVision__Key="your-key"
export Face__Key="your-key"
export ConnectionStrings__DefaultConnection="your-connection-string"
```

## Логирование

Логи записываются в:
- **Консоль** - для интерактивного вывода
- **Файлы** - `logs/photobank-YYYYMMDD.log` (ротация ежедневно, хранятся 7 дней)

### Уровни логирования

Можно настроить в `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "PhotoBank": "Debug"
    }
  }
}
```

## Примеры использования

### Обработка нового хранилища

```bash
# 1. Сначала зарегистрировать всех персон
PhotoBank.Console

# 2. Затем обработать файлы из хранилища
PhotoBank.Console --storage 5
```

### Только обработка файлов

```bash
PhotoBank.Console --storage 5 --no-register
```

### Проверка кода возврата в скриптах

```bash
#!/bin/bash
PhotoBank.Console --storage 5
EXIT_CODE=$?

if [ $EXIT_CODE -eq 0 ]; then
    echo "Success!"
elif [ $EXIT_CODE -eq 3 ]; then
    echo "Storage not found!"
elif [ $EXIT_CODE -eq 4 ]; then
    echo "Some files failed to process"
else
    echo "Error occurred: $EXIT_CODE"
fi
```

## Архитектура

Приложение использует:
- **.NET 9.0** - целевая платформа
- **System.CommandLine** - современный парсер аргументов командной строки
- **Serilog** - структурированное логирование
- **Entity Framework Core** - доступ к данным
- **Dependency Injection** - управление зависимостями

### Обогащение фотографий (Enrichment Pipeline)

При обработке каждой фотографии применяются следующие enrichers:
1. **PreviewEnricher** - создание превью
2. **AdultEnricher** - модерация контента
3. **AnalyzeEnricher** - AI-анализ
4. **MetadataEnricher** - извлечение EXIF/метаданных
5. **ThumbnailEnricher** - генерация миниатюр
6. **ColorEnricher** - анализ цвета
7. **CaptionEnricher** - генерация подписей через AI
8. **TagEnricher** - автотегирование
9. **ObjectPropertyEnricher** - распознавание объектов
10. **UnifiedFaceEnricher** - распознавание лиц

## Последние изменения

### v2.0 - Улучшения обработки ошибок и CLI

**Изменения:**
- ✅ Добавлен `System.CommandLine` для улучшенного парсинга аргументов
- ✅ Автоматическая генерация `--help`
- ✅ Полная обработка ошибок с правильными кодами выхода
- ✅ Валидация существования хранилища перед обработкой
- ✅ Улучшенное логирование с ротацией файлов
- ✅ Информативные сообщения об ошибках

**Breaking Changes:**
- Удален класс `ConsoleOptions` (заменен на System.CommandLine)
- Изменена сигнатура `App.RunAsync()` - теперь возвращает `Task<int>`

## Требования

- .NET 9.0 Runtime
- PostgreSQL база данных
- Доступ к Azure Cognitive Services (опционально)
- Доступ к AWS Rekognition (опционально)
- InsightFace API (опционально)

## Разработка

### Сборка проекта

```bash
dotnet build
```

### Запуск в режиме разработки

```bash
dotnet run -- --storage 5
```

### Отладка

В Visual Studio или VS Code установите точки останова и используйте конфигурацию запуска с аргументами.

## Устранение неполадок

### "Storage with ID X not found"

Убедитесь, что хранилище существует в базе данных:
```sql
SELECT * FROM "Storages" WHERE "Id" = X;
```

### "Configuration error"

Проверьте строку подключения к базе данных и ключи API.

### Файлы не обрабатываются

1. Проверьте права доступа к файловой системе
2. Убедитесь, что путь к хранилищу корректен
3. Проверьте логи в `logs/photobank-*.log`

## Лицензия

См. корневой LICENSE файл проекта.
