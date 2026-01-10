# Схема зависимостей энричеров PhotoBank

## Граф зависимостей

```mermaid
graph TD
    %% Корневой энричер
    Preview[PreviewEnricher<br/>🖼️ Создание превью]

    %% Подготовка и проверка дубликатов
    Preview --> Duplicate[DuplicateEnricher<br/>🔁 Дубликаты + файлы]

    %% Энричеры базового уровня
    Duplicate --> Adult[AdultEnricher<br/>🔞 Adult контент ONNX]
    Duplicate --> Metadata[MetadataEnricher<br/>📋 EXIF данные]
    Duplicate --> Thumbnail[ThumbnailEnricher<br/>🔲 Миниатюра]
    Duplicate --> UnifiedFace[UnifiedFaceEnricher<br/>👤 Лица unified]

    %% Энричеры анализа
    Adult --> Analyze[AnalyzeEnricher<br/>🔍 Анализ изображения]

    %% Энричеры детального анализа
    Analyze --> Color[ColorEnricher<br/>🎨 Цвета]
    Analyze --> Caption[CaptionEnricher<br/>💬 Описание]
    Analyze --> Tag[TagEnricher<br/>🏷️ Теги]
    Analyze --> Category[CategoryEnricher<br/>📁 Категории]

    %% Унифицированный энричер объектов (зависимость зависит от провайдера)
    Analyze -.-> UnifiedObject[UnifiedObjectPropertyEnricher<br/>📦 Объекты unified]
    Duplicate -.-> UnifiedObject

    %% Стили
    classDef root fill:#4CAF50,stroke:#2E7D32,color:#fff
    classDef level1 fill:#2196F3,stroke:#1565C0,color:#fff
    classDef level2 fill:#FF9800,stroke:#E65100,color:#fff
    classDef level3 fill:#FFB74D,stroke:#E65100,color:#fff
    classDef unified fill:#9C27B0,stroke:#6A1B9A,color:#fff

    class Preview root
    class Duplicate,Metadata,Thumbnail,Adult level1
    class Analyze level2
    class Color,Caption,Tag,Category level3
    class UnifiedFace,UnifiedObject unified
```

> Примечание: `UnifiedObjectPropertyEnricher` зависит от провайдера —
> - **Azure**: зависит от `AnalyzeEnricher` (использует ImageAnalysis)
> - **YOLO ONNX**: зависит от `DuplicateEnricher` (использует PreviewImage)

## Уровни зависимостей

### 🟢 Уровень 0 - Корневой
- **PreviewEnricher** - создает preview изображения из оригинального файла
  - Зависимостей: нет
  - Сервисы: `IImageService` (ImageMagick)
  - Дополнительно: формирует letterbox 640x640 для ONNX моделей

### 🔵 Уровень 1 - Подготовка и базовая обработка
- **DuplicateEnricher** - проверяет дубликаты и подготавливает базовые поля
  - Зависимости: `PreviewEnricher`
  - Сервисы: `IRepository<Photo>`
  - Данные: ImageHash, Name, RelativePath, Files, DuplicatePhotoId

- **AdultEnricher** - проверяет на adult/racy контент с помощью ONNX модели
  - Зависимости: `DuplicateEnricher`
  - Сервисы: `INudeNetDetector` (Local ONNX NudeNet YOLOv8)
  - Данные: AdultScore, RacyScore, IsAdultContent, IsRacyContent

- **MetadataEnricher** - извлекает EXIF метаданные из файла
  - Зависимости: `DuplicateEnricher`
  - Сервисы: `IImageMetadataReaderWrapper` (MetadataExtractor)
  - Извлекает: Дата съемки, GPS, Camera info

- **ThumbnailEnricher** - генерирует миниатюру 50x50px
  - Зависимости: `DuplicateEnricher`
  - Сервисы: Smartcrop + ImageMagick

- **UnifiedFaceEnricher** ✅ - универсальный детектор лиц
  - Зависимости: `DuplicateEnricher`
  - Сервисы: `IUnifiedFaceService` (Azure/AWS/Local провайдеры)
  - Функции: Определение лиц, возраст, пол, эмоции, создание preview лиц

### 🟠 Уровень 2 - Анализ содержимого
- **AnalyzeEnricher** - анализирует изображение через `IImageAnalyzer`
  - Зависимости: `AdultEnricher`
  - Сервисы: `IImageAnalyzer` (Azure/OpenRouter/Ollama и др.)
  - Извлекает: Categories, Description, Tags, Objects, Colors, Adult content

### 🟠 Уровень 3 - Детализация анализа
Все следующие энричеры зависят от **AnalyzeEnricher** и обрабатывают результаты его работы:

- **ColorEnricher** - извлекает информацию о цветах
  - Данные: IsBW, AccentColor, DominantColors

- **CaptionEnricher** - извлекает описания изображения
  - Данные: Captions с confidence scores

- **TagEnricher** - создает/связывает теги
  - Базовый класс: `BaseLookupEnricher<Tag, PhotoTag>`
  - База данных: `IRepository<Tag>`

- **CategoryEnricher** - создает/связывает категории
  - Базовый класс: `BaseLookupEnricher<Category, PhotoCategory>`
  - База данных: `IRepository<Category>`

### 🟣 Уровень 1/2 - Унифицированные объекты
- **UnifiedObjectPropertyEnricher** - детекция объектов через провайдер
  - Зависимости:
    - Azure: `AnalyzeEnricher`
    - YOLO ONNX: `DuplicateEnricher`
  - Сервисы: `IObjectDetectionProvider`
  - База данных: `IRepository<PropertyName>`

## Базовые классы

```mermaid
classDiagram
    class IEnricher {
        <<interface>>
        +EnricherType EnricherType
        +EnrichAsync(photo, source, token) Task
    }

    class IOrderDependent {
        <<interface>>
        +Type[] Dependencies
    }

    class BaseLookupEnricher~TModel,TLink~ {
        <<abstract>>
        +Dependencies: [AnalyzeEnricher]
        +EnrichAsync(photo, source, token) Task
        #GetItemsFromSource(source) IEnumerable
        #CreateModel(item) TModel
        #CreateLink(photo, model, item) TLink
    }

    IEnricher --|> IOrderDependent
    BaseLookupEnricher ..|> IEnricher

    TagEnricher --|> BaseLookupEnricher
    CategoryEnricher --|> BaseLookupEnricher

    class TagEnricher {
        +EnricherType: Tag
    }

    class CategoryEnricher {
        +EnricherType: Category
    }
```

## Порядок выполнения

Enrichment Pipeline использует топологическую сортировку для определения порядка выполнения:

1. **PreviewEnricher** (корневой)
2. **DuplicateEnricher**
3. **Параллельно (после DuplicateEnricher):**
   - AdultEnricher (ONNX)
   - MetadataEnricher
   - ThumbnailEnricher
   - UnifiedFaceEnricher
   - UnifiedObjectPropertyEnricher (если провайдер YOLO ONNX)
4. **После AdultEnricher:**
   - AnalyzeEnricher
5. **После AnalyzeEnricher (параллельно):**
   - ColorEnricher
   - CaptionEnricher
   - TagEnricher
   - CategoryEnricher
   - UnifiedObjectPropertyEnricher (если провайдер Azure)

## Статистика

- **Всего энричеров:** 12
- **Уровней зависимостей:** 4
- **Внешних сервисов:** 5 (ImageMagick, MetadataExtractor, Smartcrop, ONNX, ImageAnalyzer)
- **Репозиториев БД:** 4 (Photo, Tag, Category, PropertyName)
- **Поддержка параллелизма:** Да (`RunBatchAsync`)

## Компоненты оркестрации

### EnrichmentPipeline
- Выполняет энричеры в правильном порядке
- Топологическая сортировка зависимостей
- Обработка ошибок (continue-on-error)
- Параллельная пакетная обработка

### EnricherDependencyResolver
- Алгоритм: Топологическая сортировка Кана
- Обнаружение циклических зависимостей
- Валидация отсутствующих зависимостей

### ActiveEnricherProvider
- Загружает активные энричеры из БД
- Позволяет включать/выключать энричеры без изменения кода

## Использование в проекте

```csharp
// Запуск всех активных энричеров
await enrichmentPipeline.RunAsync(photo, sourceData, cancellationToken);

// Запуск конкретных энричеров
var enrichers = new[] {
    typeof(PreviewEnricher),
    typeof(AnalyzeEnricher)
};
await enrichmentPipeline.RunAsync(photo, sourceData, enrichers, cancellationToken);

// Пакетная обработка
var items = photos.Zip(sources, (p, s) => (p, s));
await enrichmentPipeline.RunBatchAsync(items, cancellationToken);
```

## Ссылки на код

- Интерфейсы: `backend/PhotoBank.Services/Enrichers/IEnricher.cs:1`
- Базовый класс: `backend/PhotoBank.Services/Enrichers/BaseLookupEnricher.cs:1`
- Pipeline: `backend/PhotoBank.Services/Enrichment/EnrichmentPipeline.cs:1`
- Resolver: `backend/PhotoBank.Services/Enrichment/EnricherDependencyResolver.cs:1`
- Регистрация: `backend/PhotoBank.DependencyInjection/AddPhotobankConsoleExtensions.cs:1`
