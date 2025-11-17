# ONNX Object Detection Enricher

## Описание

`OnnxObjectDetectionEnricher` - это enricher для обнаружения объектов на изображениях с использованием ML.NET и ONNX-моделей. Он является альтернативой `ObjectPropertyEnricher`, который использует Azure Computer Vision API.

### Преимущества использования ONNX:

- **Локальная обработка**: не требует подключения к облачным сервисам Azure
- **Бесплатность**: нет затрат на API-вызовы
- **Приватность**: данные не покидают вашу инфраструктуру
- **Гибкость**: можно использовать любые YOLO-модели (YOLOv5, YOLOv8, и т.д.)
- **Производительность**: обработка происходит локально без сетевых задержек

## Установка

### 1. Скачивание ONNX модели

Скачайте предобученную YOLO модель в формате ONNX. Рекомендуется использовать YOLOv8:

```bash
# Скачайте YOLOv8n (nano - самая легкая модель)
wget https://github.com/ultralytics/assets/releases/download/v0.0.0/yolov8n.onnx

# Или YOLOv8s (small - баланс между скоростью и точностью)
wget https://github.com/ultralytics/assets/releases/download/v0.0.0/yolov8s.onnx
```

Альтернативно, вы можете экспортировать модель самостоятельно:

```python
from ultralytics import YOLO

# Load a model
model = YOLO('yolov8n.pt')

# Export to ONNX
model.export(format='onnx')
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

## Архитектура и потокобезопасность

### Thread-Safety

`YoloOnnxService` использует `PredictionEnginePool` из `Microsoft.Extensions.ML` для обеспечения потокобезопасности при параллельной обработке изображений:

- **PredictionEnginePool** - управляет пулом `PredictionEngine` экземпляров
- **Thread-safe** - безопасен для использования в многопоточных сценариях
- **Optimal performance** - переиспользует экземпляры вместо создания новых для каждого запроса

### Регистрация в DI

- `PredictionEnginePool<YoloImageInput, YoloOutput>` - **Singleton**
- `IYoloOnnxService` / `YoloOnnxService` - **Transient**
- `OnnxObjectDetectionEnricher` - **Transient**

Это обеспечивает оптимальную производительность и безопасное использование в ASP.NET Core приложениях с параллельными запросами.

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

Производительность зависит от выбранной модели:

- **YOLOv8n** (nano): ~10-20 мс на изображение (CPU), наименее точная
- **YOLOv8s** (small): ~20-40 мс на изображение (CPU), хороший баланс
- **YOLOv8m** (medium): ~40-80 мс на изображение (CPU), более точная
- **YOLOv8l** (large): ~80-150 мс на изображение (CPU), наиболее точная

*Время указано ориентировочно для CPU. С GPU производительность значительно выше.*

## Troubleshooting

### Ошибка "Model file not found"

Убедитесь, что путь к модели в конфигурации указан правильно и файл существует.

### Низкая производительность

- Используйте более легкую модель (YOLOv8n вместо YOLOv8m/l)
- Рассмотрите использование GPU для инференса
- Уменьшите размер preview-изображения

### Слишком много/мало детекций

- Увеличьте `ConfidenceThreshold` для уменьшения количества детекций
- Уменьшите `ConfidenceThreshold` для увеличения чувствительности
- Настройте `NmsThreshold` для контроля дублирующихся детекций

## Дополнительная информация

- [ML.NET Documentation](https://docs.microsoft.com/en-us/dotnet/machine-learning/)
- [YOLO Models](https://github.com/ultralytics/ultralytics)
- [ONNX Runtime](https://onnxruntime.ai/)
