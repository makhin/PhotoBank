# Инструкция по запуску InsightFace на Windows с Nvidia GPU

Данное руководство описывает процесс настройки и запуска сервиса InsightFace для распознавания лиц на ноутбуке под управлением Windows с видеокартой Nvidia.

## Содержание
- [Предварительные требования](#предварительные-требования)
- [Вариант 1: Запуск через Docker (рекомендуется)](#вариант-1-запуск-через-docker-рекомендуется)
- [Вариант 2: Нативный запуск в Windows](#вариант-2-нативный-запуск-в-windows)
- [Проверка работоспособности](#проверка-работоспособности)
- [Решение проблем](#решение-проблем)

---

## Предварительные требования

### Аппаратные требования
- **GPU**: Nvidia GPU с поддержкой CUDA 12.x (архитектура от Pascal и новее)
  - Минимум 4 GB VRAM (рекомендуется 6+ GB)
  - Примеры: GTX 1060, GTX 1070, RTX 2060, RTX 3050, RTX 4050 и выше
- **RAM**: Минимум 8 GB (рекомендуется 16 GB)
- **Свободное место**: 10+ GB для Docker образов и моделей

### Проверка совместимости GPU

Откройте PowerShell или CMD и выполните:
```powershell
nvidia-smi
```

Вы должны увидеть информацию о вашей видеокарте и версию CUDA. Пример:
```
+-----------------------------------------------------------------------------+
| NVIDIA-SMI 545.84       Driver Version: 545.84       CUDA Version: 12.3     |
|-------------------------------+----------------------+----------------------+
| GPU  Name            TCC/WDDM | Bus-Id        Disp.A | Volatile Uncorr. ECC |
|   0  NVIDIA GeForce RTX 3060  | 00000000:01:00.0 Off |                  N/A |
+-------------------------------+----------------------+----------------------+
```

Если команда не найдена, установите драйверы Nvidia (см. ниже).

---

## Вариант 1: Запуск через Docker (рекомендуется)

Docker обеспечивает изолированное окружение и упрощает развертывание. Этот вариант рекомендуется для большинства пользователей.

### Шаг 1: Установка NVIDIA драйверов

1. Скачайте последние драйверы с официального сайта:
   - Перейдите на https://www.nvidia.com/Download/index.aspx
   - Выберите вашу видеокарту
   - Скачайте и установите **Game Ready Driver** или **Studio Driver**

2. После установки перезагрузите компьютер

3. Проверьте установку:
   ```powershell
   nvidia-smi
   ```

**Важно**: Требуется версия драйвера **450.80.02 или новее** для поддержки CUDA 12.x

### Шаг 2: Установка WSL2 (Windows Subsystem for Linux)

Docker Desktop для Windows требует WSL2 для GPU acceleration.

1. Откройте PowerShell от имени администратора и выполните:
   ```powershell
   wsl --install
   ```

2. Перезагрузите компьютер

3. После перезагрузки откройте PowerShell и проверьте:
   ```powershell
   wsl --status
   ```

4. Убедитесь, что WSL2 используется по умолчанию:
   ```powershell
   wsl --set-default-version 2
   ```

5. Установите Ubuntu (если не установлена автоматически):
   ```powershell
   wsl --install -d Ubuntu-22.04
   ```

### Шаг 3: Установка CUDA Toolkit в WSL2

1. Откройте Ubuntu в WSL2:
   ```powershell
   wsl
   ```

2. Установите CUDA Toolkit:
   ```bash
   # Удалите старые ключи (если есть)
   sudo apt-key del 7fa2af80

   # Добавьте новый репозиторий CUDA 12.6
   wget https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2204/x86_64/cuda-keyring_1.1-1_all.deb
   sudo dpkg -i cuda-keyring_1.1-1_all.deb

   # Обновите пакеты
   sudo apt-get update

   # Установите CUDA Toolkit
   sudo apt-get install -y cuda-toolkit-12-6
   ```

3. Проверьте установку:
   ```bash
   nvidia-smi
   nvcc --version
   ```

### Шаг 4: Установка Docker Desktop

1. Скачайте Docker Desktop для Windows:
   - https://www.docker.com/products/docker-desktop

2. Запустите установщик и следуйте инструкциям

3. **Важно**: При установке убедитесь, что включены опции:
   - ✅ Use WSL 2 instead of Hyper-V
   - ✅ Install required Windows components for WSL 2

4. После установки перезагрузите компьютер

5. Запустите Docker Desktop

6. Откройте Settings → General и убедитесь:
   - ✅ Use the WSL 2 based engine

7. Перейдите в Settings → Resources → WSL Integration:
   - ✅ Enable integration with my default WSL distro
   - ✅ Ubuntu-22.04 (или ваш дистрибутив)

### Шаг 5: Установка NVIDIA Container Toolkit

NVIDIA Container Toolkit позволяет Docker контейнерам использовать GPU.

1. Откройте Ubuntu в WSL2:
   ```powershell
   wsl
   ```

2. Установите NVIDIA Container Toolkit:
   ```bash
   # Настройте репозиторий
   distribution=$(. /etc/os-release;echo $ID$VERSION_ID)
   curl -fsSL https://nvidia.github.io/libnvidia-container/gpgkey | sudo gpg --dearmor -o /usr/share/keyrings/nvidia-container-toolkit-keyring.gpg
   curl -s -L https://nvidia.github.io/libnvidia-container/$distribution/libnvidia-container.list | \
       sed 's#deb https://#deb [signed-by=/usr/share/keyrings/nvidia-container-toolkit-keyring.gpg] https://#g' | \
       sudo tee /etc/apt/sources.list.d/nvidia-container-toolkit.list

   # Установите пакет
   sudo apt-get update
   sudo apt-get install -y nvidia-container-toolkit

   # Настройте Docker для использования NVIDIA runtime
   sudo nvidia-ctk runtime configure --runtime=docker

   # Перезапустите Docker (из Windows PowerShell)
   ```

3. В Windows PowerShell перезапустите Docker:
   ```powershell
   # Через Docker Desktop UI: Settings → Restart
   # Или через командную строку (от администратора):
   net stop com.docker.service
   net start com.docker.service
   ```

4. Проверьте GPU доступ в Docker:
   ```bash
   # В WSL2
   docker run --rm --gpus all nvidia/cuda:12.6.0-base-ubuntu22.04 nvidia-smi
   ```

   Вы должны увидеть информацию о вашей GPU.

### Шаг 6: Сборка и запуск InsightFace сервиса

1. Откройте WSL2 и перейдите в директорию проекта:
   ```bash
   # Если PhotoBank находится на диске C:\Users\YourName\PhotoBank
   cd /mnt/c/Users/YourName/PhotoBank/insightface

   # Или если проект в WSL
   cd ~/PhotoBank/insightface
   ```

2. Соберите GPU-образ:
   ```bash
   docker build -f Dockerfile.gpu -t insightface-gpu:latest .
   ```

   Это займет 5-10 минут при первой сборке.

3. Запустите контейнер:
   ```bash
   docker run -d \
     --name insightface-gpu \
     --gpus all \
     -p 5555:5555 \
     --memory=4g \
     --restart unless-stopped \
     insightface-gpu:latest
   ```

4. Проверьте логи:
   ```bash
   docker logs -f insightface-gpu
   ```

   Вы должны увидеть:
   ```
   INFO:     Started server process [1]
   INFO:     Waiting for application startup.
   INFO:     Application startup complete.
   INFO:     Uvicorn running on http://0.0.0.0:5555
   ```

### Шаг 7: Использование docker-compose (альтернатива)

Если вы предпочитаете docker-compose, создайте файл `docker-compose.gpu.yml`:

```yaml
version: '3.8'

services:
  insightface-gpu:
    build:
      context: .
      dockerfile: Dockerfile.gpu
    container_name: insightface-gpu
    ports:
      - "5555:5555"
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: 1
              capabilities: [gpu]
    environment:
      - CUDA_VISIBLE_DEVICES=0
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "python", "-c", "import requests; requests.get('http://localhost:5555/health', timeout=5)"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 60s
```

Запуск:
```bash
docker-compose -f docker-compose.gpu.yml up -d
```

---

## Вариант 2: Нативный запуск в Windows

Если вы хотите запустить сервис напрямую в Windows без Docker (для разработки или отладки).

### Шаг 1: Установка CUDA Toolkit для Windows

1. Скачайте CUDA Toolkit 12.6:
   - https://developer.nvidia.com/cuda-downloads
   - Выберите: Windows → x86_64 → 11 → exe (local)

2. Запустите установщик и выберите **Custom installation**

3. Убедитесь, что выбраны компоненты:
   - ✅ CUDA → Development
   - ✅ CUDA → Runtime
   - ✅ CUDA → Documentation (опционально)

4. Установите по умолчанию в `C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v12.6`

5. Проверьте переменные окружения (должны быть добавлены автоматически):
   ```
   CUDA_PATH=C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v12.6
   ```

   PATH должен содержать:
   ```
   C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v12.6\bin
   C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v12.6\libnvidia
   ```

### Шаг 2: Установка cuDNN

1. Скачайте cuDNN 9.x для CUDA 12.6:
   - https://developer.nvidia.com/cudnn
   - Требуется регистрация NVIDIA Developer

2. Распакуйте архив

3. Скопируйте файлы в директорию CUDA:
   ```
   cudnn-windows-x86_64-9.x.x.x\bin\*.dll     → C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v12.6\bin
   cudnn-windows-x86_64-9.x.x.x\include\*.h   → C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v12.6\include
   cudnn-windows-x86_64-9.x.x.x\lib\x64\*.lib → C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v12.6\lib\x64
   ```

### Шаг 3: Установка Python 3.11

1. Скачайте Python 3.11:
   - https://www.python.org/downloads/
   - Версия 3.11.x (например, 3.11.9)

2. При установке:
   - ✅ Add Python 3.11 to PATH
   - Выберите "Customize installation"
   - ✅ pip
   - ✅ py launcher

3. Проверьте установку:
   ```powershell
   python --version
   # Python 3.11.9

   pip --version
   ```

### Шаг 4: Установка зависимостей Python

1. Откройте PowerShell и перейдите в директорию insightface:
   ```powershell
   cd C:\Users\YourName\PhotoBank\insightface
   ```

2. Создайте виртуальное окружение:
   ```powershell
   python -m venv venv
   .\venv\Scripts\Activate.ps1
   ```

   Если получите ошибку "running scripts is disabled", выполните:
   ```powershell
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
   ```

3. Обновите pip:
   ```powershell
   python -m pip install --upgrade pip setuptools wheel
   ```

4. Установите `onnxruntime-gpu` (для GPU):
   ```powershell
   pip install onnxruntime-gpu==1.19.2
   ```

   **Важно**: Проверьте совместимость:
   - onnxruntime-gpu 1.19.x требует CUDA 12.x и cuDNN 9.x
   - Если у вас CUDA 11.x, используйте `onnxruntime-gpu==1.16.3`

5. Установите остальные зависимости:
   ```powershell
   # Создайте файл requirements-gpu.txt
   @"
   fastapi>=0.115.0,<0.116.0
   uvicorn[standard]>=0.32.0,<0.33.0
   python-multipart>=0.0.9,<0.1.0
   pydantic>=2.0.0,<3.0.0
   python-dotenv>=1.0.0,<2.0.0
   numpy>=1.24.0,<2.0.0
   insightface>=0.7.3,<0.8.0
   scikit-learn>=1.3.0,<2.0.0
   hsemotion>=0.2.0,<0.3.0
   opencv-python>=4.8.0,<5.0.0
   requests>=2.31.0,<3.0.0
   "@ | Out-File -FilePath requirements-gpu.txt -Encoding UTF8

   pip install -r requirements-gpu.txt
   ```

6. Проверьте, что onnxruntime видит GPU:
   ```powershell
   python -c "import onnxruntime as ort; print(ort.get_available_providers())"
   ```

   Вывод должен содержать `'CUDAExecutionProvider'`:
   ```
   ['CUDAExecutionProvider', 'CPUExecutionProvider']
   ```

### Шаг 5: Запуск сервиса

1. Убедитесь, что вы в директории insightface с активированным venv:
   ```powershell
   cd C:\Users\YourName\PhotoBank\insightface
   .\venv\Scripts\Activate.ps1
   ```

2. Запустите сервис:
   ```powershell
   python -m uvicorn app.main:app --host 0.0.0.0 --port 5555 --reload
   ```

   Для production (без --reload):
   ```powershell
   python -m uvicorn app.main:app --host 0.0.0.0 --port 5555 --workers 1
   ```

   **Важно**: Для GPU используйте только `--workers 1` (модель занимает всю VRAM).

3. Сервис будет доступен по адресу: http://localhost:5555

### Шаг 6: Создание Windows Service (опционально)

Для автоматического запуска при старте Windows можно использовать NSSM (Non-Sucking Service Manager).

1. Скачайте NSSM:
   - https://nssm.cc/download

2. Распакуйте и откройте PowerShell от администратора:
   ```powershell
   cd C:\nssm-2.24\win64

   .\nssm install InsightFaceAPI
   ```

3. В открывшемся окне заполните:
   - Path: `C:\Users\YourName\PhotoBank\insightface\venv\Scripts\python.exe`
   - Startup directory: `C:\Users\YourName\PhotoBank\insightface`
   - Arguments: `-m uvicorn app.main:app --host 0.0.0.0 --port 5555`

4. Запустите сервис:
   ```powershell
   .\nssm start InsightFaceAPI
   ```

---

## Проверка работоспособности

### Базовая проверка

1. Откройте браузер и перейдите по адресу:
   ```
   http://localhost:5555/docs
   ```

   Вы должны увидеть Swagger UI с документацией API.

2. Проверьте health endpoint:
   ```powershell
   curl http://localhost:5555/health
   ```

   Ответ:
   ```json
   {"status":"healthy"}
   ```

### Тест детекции лица

Создайте тестовый скрипт `test_api.py`:

```python
import requests

# Замените на путь к вашему изображению
image_path = "test_photo.jpg"

# Тест /detect endpoint
with open(image_path, 'rb') as f:
    files = {'file': f}
    response = requests.post(
        'http://localhost:5555/detect?include_embeddings=true',
        files=files
    )

print("Status Code:", response.status_code)
print("Response:", response.json())

# Проверка наличия GPU
if response.status_code == 200:
    data = response.json()
    if data['faces']:
        print(f"\nНайдено лиц: {len(data['faces'])}")
        face = data['faces'][0]
        print(f"Пол: {face['gender']}")
        print(f"Возраст: {face['age']}")
        print(f"Эмоция: {face['emotion']}")
        print(f"Размерность эмбеддинга: {face.get('embedding_dim', 'N/A')}")
```

Запустите:
```powershell
python test_api.py
```

### Мониторинг использования GPU

В отдельном окне PowerShell запустите мониторинг:
```powershell
nvidia-smi -l 1
```

Во время обработки запросов вы должны видеть:
- Загрузку GPU (GPU-Util)
- Использование памяти (Memory-Usage)
- Процесс python.exe или uvicorn

Пример:
```
+-----------------------------------------------------------------------------+
| Processes:                                                                  |
|  GPU   GI   CI        PID   Type   Process name                  GPU Memory |
|        ID   ID                                                   Usage      |
|=============================================================================|
|    0   N/A  N/A     12345      C   ...python.exe                   1024MiB |
+-----------------------------------------------------------------------------+
```

### Benchmark производительности

Замерьте скорость обработки:

```powershell
# Установите curl-format.txt
@"
    time_namelookup:  %{time_namelookup}s\n
       time_connect:  %{time_connect}s\n
    time_appconnect:  %{time_appconnect}s\n
   time_pretransfer:  %{time_pretransfer}s\n
      time_redirect:  %{time_redirect}s\n
 time_starttransfer:  %{time_starttransfer}s\n
                    ----------\n
         time_total:  %{time_total}s\n
"@ | Out-File -FilePath curl-format.txt -Encoding ASCII

# Тест
curl -w "@curl-format.txt" -X POST http://localhost:5555/detect -F "file=@test_photo.jpg" -o response.json -s
```

Ожидаемые результаты для GPU:
- **time_total**: 15-30ms (первый запрос может быть медленнее из-за прогрева)

Для сравнения с CPU временно отключите GPU:
```powershell
$env:CUDA_VISIBLE_DEVICES = "-1"
python -m uvicorn app.main:app --host 0.0.0.0 --port 5555
```

Ожидаемые результаты для CPU:
- **time_total**: 50-100ms

---

## Решение проблем

### Проблема: Docker не видит GPU

**Симптомы:**
```
docker: Error response from daemon: could not select device driver "" with capabilities: [[gpu]].
```

**Решение:**

1. Убедитесь, что NVIDIA Container Toolkit установлен в WSL2:
   ```bash
   wsl
   nvidia-ctk --version
   ```

2. Проверьте конфигурацию Docker:
   ```bash
   sudo nvidia-ctk runtime configure --runtime=docker
   cat /etc/docker/daemon.json
   ```

   Должно содержать:
   ```json
   {
     "runtimes": {
       "nvidia": {
         "path": "nvidia-container-runtime",
         "runtimeArgs": []
       }
     }
   }
   ```

3. Перезапустите Docker Desktop

4. Проверьте еще раз:
   ```bash
   docker run --rm --gpus all nvidia/cuda:12.6.0-base-ubuntu22.04 nvidia-smi
   ```

### Проблема: onnxruntime-gpu не использует GPU

**Симптомы:**
```python
ort.get_available_providers()
# ['CPUExecutionProvider']  # Нет CUDAExecutionProvider!
```

**Решение:**

1. Проверьте версию CUDA:
   ```powershell
   nvcc --version
   nvidia-smi
   ```

2. Убедитесь в совместимости onnxruntime-gpu:
   - CUDA 12.x → onnxruntime-gpu >= 1.17.0
   - CUDA 11.x → onnxruntime-gpu 1.16.x

3. Переустановите правильную версию:
   ```powershell
   pip uninstall onnxruntime onnxruntime-gpu
   pip install onnxruntime-gpu==1.19.2
   ```

4. Проверьте cuDNN:
   - Скачайте и установите cuDNN 9.x для CUDA 12.x
   - Скопируйте DLL файлы в `C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v12.6\bin`

5. Проверьте PATH:
   ```powershell
   $env:PATH
   ```
   Должен содержать пути к CUDA bin и libnvidia.

### Проблема: Модели не загружаются

**Симптомы:**
```
Error: Failed to download model
```

**Решение:**

1. InsightFace автоматически скачивает модели при первом запуске
2. Модели сохраняются в `~/.insightface/models/`
3. Если скачивание не удается (firewall, proxy), скачайте вручную:

   ```powershell
   # Создайте директорию
   mkdir $env:USERPROFILE\.insightface\models\antelopev2

   # Скачайте модели с:
   # https://github.com/deepinsight/insightface/releases/tag/v0.7
   ```

4. Если используется корпоративный proxy:
   ```powershell
   $env:HTTP_PROXY = "http://proxy.company.com:8080"
   $env:HTTPS_PROXY = "http://proxy.company.com:8080"
   ```

### Проблема: Out of Memory (OOM) на GPU

**Симптомы:**
```
CUDA error: out of memory
```

**Решение:**

1. Закройте другие приложения, использующие GPU (игры, 3D редакторы)

2. Проверьте загрузку памяти:
   ```powershell
   nvidia-smi
   ```

3. Уменьшите количество workers до 1:
   ```powershell
   python -m uvicorn app.main:app --host 0.0.0.0 --port 5555 --workers 1
   ```

4. Если используется маленькая GPU (4 GB), рассмотрите использование CPU версии

### Проблема: Медленная обработка на GPU

**Симптомы:**
- GPU используется, но время обработки ~50ms (как на CPU)

**Решение:**

1. Проверьте, что действительно используется GPU:
   ```powershell
   nvidia-smi -l 1
   ```
   Во время запросов должна быть загрузка GPU.

2. Убедитесь, что установлен `onnxruntime-gpu`, а не `onnxruntime`:
   ```powershell
   pip list | findstr onnxruntime
   # Должно быть: onnxruntime-gpu  1.19.2
   ```

3. Первый запрос всегда медленнее (инициализация CUDA). Второй и последующие должны быть быстрее.

4. Проверьте temperature throttling:
   ```powershell
   nvidia-smi
   ```
   Если температура >80°C, GPU может снижать частоту. Улучшите охлаждение.

### Проблема: WSL2 не запускается

**Симптомы:**
```
The virtual machine could not be started because a required feature is not installed.
```

**Решение:**

1. Включите виртуализацию в BIOS (VT-x для Intel, AMD-V для AMD)

2. Включите Windows Features:
   ```powershell
   # От администратора
   dism.exe /online /enable-feature /featurename:Microsoft-Windows-Subsystem-Linux /all /norestart
   dism.exe /online /enable-feature /featurename:VirtualMachinePlatform /all /norestart
   ```

3. Перезагрузите компьютер

4. Установите WSL2 kernel update:
   - https://aka.ms/wsl2kernel

### Проблема: Порт 5555 занят

**Симптомы:**
```
ERROR: [Errno 10048] error while attempting to bind on address ('0.0.0.0', 5555)
```

**Решение:**

1. Найдите процесс, использующий порт:
   ```powershell
   netstat -ano | findstr :5555
   ```

2. Завершите процесс (замените PID):
   ```powershell
   taskkill /PID <PID> /F
   ```

3. Или измените порт в команде запуска:
   ```powershell
   python -m uvicorn app.main:app --host 0.0.0.0 --port 5556
   ```

---

## Производительность и оптимизация

### Ожидаемая производительность

| Конфигурация | Время обработки (1 лицо) | Throughput | VRAM |
|--------------|-------------------------|-----------|------|
| RTX 4060 (GPU) | 10-15ms | 60-80 req/s | ~1.2 GB |
| RTX 3060 (GPU) | 15-20ms | 50-60 req/s | ~1.2 GB |
| RTX 2060 (GPU) | 20-30ms | 35-50 req/s | ~1.2 GB |
| **GTX 1070 (GPU)** | **20-30ms** | **35-50 req/s** | **~1.2 GB** |
| GTX 1660 (GPU) | 25-35ms | 30-40 req/s | ~1.2 GB |
| CPU i7-12700 | 60-80ms | 12-15 req/s | N/A |

### Оптимизация для максимальной производительности

1. **Отключите Windows Defender для директории проекта** (снижает overhead на I/O):
   - Windows Security → Virus & threat protection → Exclusions → Add folder
   - Добавьте `C:\Users\YourName\PhotoBank`

2. **Включите Hardware-accelerated GPU scheduling**:
   - Settings → System → Display → Graphics settings
   - ✅ Hardware-accelerated GPU scheduling
   - Перезагрузка

3. **Используйте High Performance режим**:
   - Settings → System → Power & sleep → Additional power settings
   - High performance

4. **Закройте фоновые приложения**:
   - Особенно браузеры с аппаратным ускорением (Chrome, Edge)
   - Игровые лаунчеры (Steam, Epic Games)

5. **Мониторинг температуры**:
   - Используйте MSI Afterburner или HWMonitor
   - Держите температуру GPU <75°C для стабильности

---

## Интеграция с PhotoBank

После запуска InsightFace сервиса, необходимо настроить PhotoBank backend для его использования.

### Конфигурация backend

Отредактируйте `backend/PhotoBank.Api/appsettings.json`:

```json
{
  "FaceProvider": {
    "Default": "Local"
  },
  "LocalInsightFace": {
    "BaseUrl": "http://localhost:5555",
    "MaxParallelism": 6,
    "Model": "buffalo_l",
    "FaceMatchThreshold": 0.45,
    "TopK": 10
  }
}
```

**Важно**:
- Если сервис запущен в Docker на WSL2, используйте `http://host.docker.internal:5555`
- Если backend тоже в Docker, добавьте в docker-compose сеть

### Проверка подключения

Запустите PhotoBank backend и проверьте логи:
```
INFO: Successfully connected to LocalInsightFace at http://localhost:5555
INFO: Face provider: Local (InsightFace)
```

---

## Дополнительные ресурсы

- **InsightFace GitHub**: https://github.com/deepinsight/insightface
- **ONNX Runtime GPU**: https://onnxruntime.ai/docs/execution-providers/CUDA-ExecutionProvider.html
- **NVIDIA CUDA Documentation**: https://docs.nvidia.com/cuda/
- **Docker GPU Support**: https://docs.docker.com/config/containers/resource_constraints/#gpu
- **WSL2 CUDA**: https://docs.nvidia.com/cuda/wsl-user-guide/index.html

---

## Заключение

После выполнения инструкций вы получите:
- ✅ Работающий InsightFace сервис с GPU ускорением
- ✅ Производительность 15-30ms на запрос (в 3-5 раз быстрее CPU)
- ✅ REST API на порту 5555
- ✅ Swagger документацию на http://localhost:5555/docs
- ✅ Готовность к интеграции с PhotoBank

При возникновении проблем обращайтесь к разделу "Решение проблем" или создавайте issue в репозитории проекта.
