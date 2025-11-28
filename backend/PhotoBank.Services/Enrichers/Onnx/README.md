# ONNX Object Detection Enricher

## Описание

`OnnxObjectDetectionEnricher` - это enricher для обнаружения объектов на изображениях с использованием ML.NET и ONNX-моделей. Он является альтернативой `ObjectPropertyEnricher`, который использует Azure Computer Vision API.

**Поддерживаемые модели:**
- **YOLOv5** (v7.0+): формат `[1, 25200, 85]` с objectness score
- **YOLOv8**: формат `[1, 84, 8400]` без objectness score (рекомендуется)

Сервис автоматически определяет формат модели и использует соответствующий парсер.

### Преимущества использования ONNX:

- **Локальная обработка**: не требует подключения к облачным сервисам Azure
- **Бесплатность**: нет затрат на API-вызовы
- **Приватность**: данные не покидают вашу инфраструктуру
- **Гибкость**: поддержка YOLOv5 и YOLOv8 моделей с автоматическим определением формата
- **Производительность**: обработка происходит локально без сетевых задержек
- **GPU ускорение**: использует CUDA для значительного ускорения обработки на GPU

## Установка

### 1. Скачивание ONNX модели

Скачайте предобученную YOLO модель в формате ONNX. Поддерживаются **YOLOv5** и **YOLOv8**:

**YOLOv8 (рекомендуется):**
```bash
# Скачайте YOLOv8n (nano - самая легкая модель)
wget https://github.com/ultralytics/assets/releases/download/v0.0.0/yolov8n.onnx

# Или YOLOv8s (small - баланс между скоростью и точностью)
wget https://github.com/ultralytics/assets/releases/download/v0.0.0/yolov8s.onnx
```

**YOLOv5:**
```bash
# Скачайте YOLOv5s
wget https://github.com/ultralytics/yolov5/releases/download/v7.0/yolov5s.onnx

# Или YOLOv5m
wget https://github.com/ultralytics/yolov5/releases/download/v7.0/yolov5m.onnx
```

Альтернативно, вы можете экспортировать модель самостоятельно:

**YOLOv8:**
```python
from ultralytics import YOLO

# Load a model
model = YOLO('yolov8n.pt')

# Export to ONNX
model.export(format='onnx')
```

**YOLOv5:**
```python
import torch

# Load YOLOv5 model
model = torch.hub.load('ultralytics/yolov5', 'yolov5s')

# Export to ONNX
model.export(formats=['onnx'])
```

### 2. Размещение модели

Поместите скачанный файл `.onnx` в доступное место, например:

```
/app/models/yolov8n.onnx
```

### 3. Конфигурация

Добавьте секцию `YoloOnnx` в ваш `appsettings.json`:

```json
{
  "YoloOnnx": {
    "Enabled": true,
    "ModelPath": "/app/models/yolov8n.onnx",
    "ConfidenceThreshold": 0.5,
    "NmsThreshold": 0.45
  }
}
```

#### Параметры конфигурации:

- **Enabled** (bool): Включить/выключить ONNX enricher. Если `false`, будет использоваться `ObjectPropertyEnricher` с Azure Computer Vision
- **ModelPath** (string): Полный путь к ONNX-файлу модели
- **ConfidenceThreshold** (float): Минимальный порог уверенности для детекции объектов (0.0 - 1.0). Рекомендуется 0.5
- **NmsThreshold** (float): Порог для NMS (Non-Maximum Suppression) алгоритма для устранения дублирующихся детекций (0.0 - 1.0). Рекомендуется 0.45

### Class-Aware NMS

Реализация использует **class-aware Non-Maximum Suppression** - NMS применяется **отдельно для каждого класса объектов**:

**Почему это важно:**
- Разные объекты могут физически перекрываться (например, человек на велосипеде, чашка на столе)
- Глобальный NMS удалил бы один из объектов, даже если оба детектированы корректно
- Class-aware NMS сохраняет все корректные детекции разных классов

**Пример:**
```
Детекции:
- person (x=100, y=100, w=50, h=100, conf=0.9)
- bicycle (x=90, y=120, w=80, h=60, conf=0.85)

IoU между person и bicycle = 0.6 (высокое перекрытие)

❌ Глобальный NMS: удалит bicycle (ниже confidence)
✅ Class-aware NMS: сохранит оба объекта (разные классы)
```

NMS применяется только к bbox одного класса, устраняя дубликаты одного и того же объекта, но сохраняя разные объекты.

## Архитектура и потокобезопасность

### ONNX Model Loading с CUDA GPU

Система использует ONNX Runtime напрямую с CUDA execution provider для GPU ускорения:

```csharp
// Конфигурация ONNX Runtime для использования CUDA GPU
var sessionOptions = new SessionOptions();
sessionOptions.AppendExecutionProvider_CUDA(0); // Использовать GPU device 0

// Создание inference session с CUDA
var session = new InferenceSession(modelPath, sessionOptions);
```

**Важно**: Используйте напрямую `.onnx` файлы (YOLOv5/YOLOv8), **не** `.zip` файлы ML.NET моделей.

**Требования для GPU:**
- NVIDIA GPU с поддержкой CUDA
- Установленный CUDA Toolkit и cuDNN
- ONNX Runtime GPU package (Microsoft.ML.OnnxRuntime.Gpu)

### Thread-Safety

`YoloOnnxService` использует `InferenceSession` из ONNX Runtime для обеспечения потокобезопасности при параллельной обработке изображений:

- **InferenceSession** - потокобезопасная сессия для inference на GPU
- **Thread-safe** - безопасен для использования в многопоточных сценариях
- **GPU Acceleration** - все вычисления выполняются на GPU через CUDA
- **Optimal performance** - сессия создается один раз и переиспользуется для всех запросов

### Регистрация в DI

Enricher регистрируется **условно** в зависимости от конфигурации:

**Если ONNX включен и модель существует:**
```csharp
services.AddSingleton<IYoloOnnxService, YoloOnnxService>();         // Singleton (manages InferenceSession)
services.AddTransient<IObjectDetectionProvider, YoloOnnxObjectDetectionProvider>(); // Transient
services.AddTransient<IEnricher, UnifiedObjectPropertyEnricher>(); // Transient
```

**Если ONNX выключен:**
```csharp
services.AddTransient<IObjectDetectionProvider, AzureObjectDetectionProvider>(); // Fallback to Azure
```

**Важно**:
- `YoloOnnxService` регистрируется как **Singleton**, так как держит `InferenceSession` с CUDA
- `InferenceSession` создается один раз при запуске и переиспользуется для всех запросов
- При остановке приложения `Dispose()` освобождает ресурсы GPU

**Lifetimes:**
- `IYoloOnnxService` / `YoloOnnxService` - **Singleton**
- `IObjectDetectionProvider` / `YoloOnnxObjectDetectionProvider` - **Transient**
- `IEnricher` / `UnifiedObjectPropertyEnricher` - **Transient**

Это обеспечивает оптимальную производительность GPU и безопасное использование в ASP.NET Core приложениях с параллельными запросами.

**Примечание**: Для переключения между ONNX и Azure требуется перезапуск приложения
(регистрация происходит при запуске). Runtime переключение не поддерживается.

### YOLO Tensor Layout

Сервис автоматически определяет формат тензора и использует соответствующий парсер для YOLOv5 или YOLOv8.

#### YOLOv8: Channels-First Layout

**Формат выхода**: `[1, 84, 8400]`
- `1` - batch size
- `84` - каналы (4 bbox координаты + 80 классов, **без objectness score**)
- `8400` - количество предсказаний (anchor boxes)

**Memory layout** (как данные хранятся в памяти):
```
output[0..8399]       = centerX для всех 8400 предсказаний
output[8400..16799]   = centerY для всех 8400 предсказаний
output[16800..25199]  = width для всех 8400 предсказаний
output[25200..33599]  = height для всех 8400 предсказаний
output[33600..42000]  = class0 scores для всех 8400 предсказаний
output[42000..50400]  = class1 scores для всех 8400 предсказаний
...
output[...] = class79 scores для всех 8400 предсказаний
```

**Доступ к данным**:
```csharp
// Для предсказания i и канала c:
var value = output[c * 8400 + i];

// Примеры:
var centerX_pred0 = output[0 * 8400 + 0];   // centerX первого предсказания
var centerY_pred0 = output[1 * 8400 + 0];   // centerY первого предсказания
var class0_pred0 = output[4 * 8400 + 0];    // score класса 0 для первого предсказания
```

Это **channels-first** layout, **НЕ** row-major layout!

#### YOLOv5: Boxes-First Layout

**Формат выхода**: `[1, 25200, 85]`
- `1` - batch size
- `25200` - количество предсказаний (больше, чем в YOLOv8)
- `85` - bbox + objectness + классы (4 + 1 + 80)

**Memory layout** (как данные хранятся в памяти):
```
Каждое предсказание - 85 последовательных значений:
output[0..84]         = первое предсказание [centerX, centerY, w, h, objectness, class0, ..., class79]
output[85..169]       = второе предсказание [centerX, centerY, w, h, objectness, class0, ..., class79]
output[170..254]      = третье предсказание [centerX, centerY, w, h, objectness, class0, ..., class79]
...
```

**Доступ к данным**:
```csharp
// Для предсказания i и поля f:
var value = output[i * 85 + f];

// Примеры:
var centerX_pred0 = output[0 * 85 + 0];    // centerX первого предсказания
var centerY_pred0 = output[0 * 85 + 1];    // centerY первого предсказания
var objectness_pred0 = output[0 * 85 + 4]; // objectness первого предсказания
var class0_pred0 = output[0 * 85 + 5];     // score класса 0 для первого предсказания
```

**Важное отличие**: YOLOv5 использует `objectness * class_score` как финальную уверенность, в то время как YOLOv8 использует только `class_score`.

#### Автоматическое определение формата

Сервис определяет формат по размеру выходного массива:
- `705,600` элементов (84 × 8,400) → YOLOv8 channels-first
- `2,142,000` элементов (25,200 × 85) → YOLOv5 boxes-first

Если формат не распознан, выбрасывается исключение `NotSupportedException`.

## Использование

После настройки enricher будет автоматически использоваться в pipeline обработки изображений вместо Azure-based `ObjectPropertyEnricher`.

### Поддерживаемые классы объектов

ONNX enricher использует модели, обученные на датасете COCO, который включает 80 классов объектов:

- Люди: person
- Транспорт: bicycle, car, motorcycle, airplane, bus, train, truck, boat
- Животные: bird, cat, dog, horse, sheep, cow, elephant, bear, zebra, giraffe
- Предметы быта: chair, couch, bed, dining table, toilet, tv, laptop, mouse, remote, keyboard, cell phone
- Кухня: bottle, wine glass, cup, fork, knife, spoon, bowl, banana, apple, sandwich, orange, broccoli, carrot, pizza, donut, cake
- И многое другое...

Полный список классов см. в `CocoClassNames.Names`.

## Производительность

Производительность зависит от выбранной модели и версии YOLO. **Все модели используют CUDA GPU ускорение.**

### YOLOv8 (рекомендуется для баланса скорость/точность)
- **YOLOv8n** (nano): ~2-5 мс на изображение (GPU), 8400 предсказаний
- **YOLOv8s** (small): ~5-10 мс на изображение (GPU), хороший баланс
- **YOLOv8m** (medium): ~10-20 мс на изображение (GPU), более точная
- **YOLOv8l** (large): ~20-35 мс на изображение (GPU), наиболее точная

### YOLOv5 (больше предсказаний, медленнее)
- **YOLOv5n** (nano): ~3-7 мс на изображение (GPU), 25200 предсказаний
- **YOLOv5s** (small): ~7-15 мс на изображение (GPU), хороший баланс
- **YOLOv5m** (medium): ~15-30 мс на изображение (GPU), более точная
- **YOLOv5l** (large): ~30-50 мс на изображение (GPU), наиболее точная

**Примечание**: YOLOv5 генерирует 25200 предсказаний против 8400 у YOLOv8, что делает его медленнее, но потенциально более чувствительным к мелким объектам.

**GPU Acceleration**: Все вычисления выполняются на NVIDIA GPU через CUDA. Производительность зависит от модели GPU (RTX 3060/3070/3080/3090/4090 и выше рекомендуются для оптимальной производительности).

## Troubleshooting

### Ошибка "Model file not found"

Убедитесь, что путь к модели в конфигурации указан правильно и файл существует.

### Ошибка "Unsupported YOLO output format"

Эта ошибка означает, что модель генерирует выходной тензор неподдерживаемого формата.

**Поддерживаемые форматы:**
- YOLOv5: `[1, 25200, 85]` = 2,142,000 элементов
- YOLOv8: `[1, 84, 8400]` = 705,600 элементов

**Возможные причины:**
- Используется модель YOLO другой версии (YOLOv3, YOLOv4, YOLOv6, YOLOv7)
- Модель обучена на другом количестве классов (не 80 COCO классов)
- Модель экспортирована с нестандартными параметрами

**Решение:**
- Используйте официальные модели YOLOv5 или YOLOv8 с COCO классами
- При экспорте используйте стандартные параметры

### Низкая производительность

- Используйте более легкую модель (YOLOv8n вместо YOLOv8m/l, YOLOv5s вместо YOLOv5m/l)
- YOLOv8 быстрее YOLOv5 при сопоставимой точности (меньше предсказаний)
- Убедитесь, что CUDA работает корректно (проверьте драйверы NVIDIA)
- Проверьте, что используется ONNX Runtime GPU package
- Уменьшите размер preview-изображения

### Ошибка при загрузке CUDA

Если вы получаете ошибку при инициализации CUDA:

- Убедитесь, что установлены драйверы NVIDIA GPU
- Убедитесь, что установлен CUDA Toolkit (версия совместимая с ONNX Runtime)
- Убедитесь, что установлен cuDNN
- Проверьте, что используется Microsoft.ML.OnnxRuntime.Gpu package (не CPU версия)
- Проверьте доступность GPU: `nvidia-smi`

### Слишком много/мало детекций

- Увеличьте `ConfidenceThreshold` для уменьшения количества детекций
- Уменьшите `ConfidenceThreshold` для увеличения чувствительности
- Настройте `NmsThreshold` для контроля дублирующихся детекций
- YOLOv5 более чувствителен к мелким объектам (25200 предсказаний vs 8400)

## Дополнительная информация

- [ML.NET Documentation](https://docs.microsoft.com/en-us/dotnet/machine-learning/)
- [YOLO Models](https://github.com/ultralytics/ultralytics)
- [ONNX Runtime](https://onnxruntime.ai/)
