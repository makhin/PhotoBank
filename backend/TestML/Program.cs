using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== ONNX Runtime GPU Test ===\n");

        // Проверка доступных провайдеров
        Console.WriteLine("Available providers:");
        foreach (var provider in OrtEnv.Instance().GetAvailableProviders())
        {
            Console.WriteLine($"  - {provider}");
        }
        Console.WriteLine();

        // Настройка сессии для GPU
        var sessionOptions = new SessionOptions();
        sessionOptions.AppendExecutionProvider_CUDA(0); // 0 = device ID
        sessionOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;

        Console.WriteLine("Downloading model...");

        // Используем простую модель squeezenet (5 MB)
        string modelPath = "squeezenet.onnx";
        if (!File.Exists(modelPath))
        {
            using var client = new HttpClient();
            var bytes = client.GetByteArrayAsync(
                "https://github.com/onnx/models/raw/main/validated/vision/classification/squeezenet/model/squeezenet1.1-7.onnx"
            ).Result;
            File.WriteAllBytes(modelPath, bytes);
        }

        Console.WriteLine("Loading model on GPU...");
        using var session = new InferenceSession(modelPath, sessionOptions);

        Console.WriteLine($"Model loaded successfully!");
        Console.WriteLine($"Input: {session.InputMetadata.First().Key}");
        Console.WriteLine($"Output: {session.OutputMetadata.First().Key}\n");

        // Создаем тестовый тензор (1, 3, 224, 224) - batch, channels, height, width
        var inputName = session.InputMetadata.First().Key;
        var dimensions = new[] { 1, 3, 224, 224 };
        var inputData = new DenseTensor<float>(dimensions);

        // Заполняем случайными значениями
        var random = new Random(42);
        for (int i = 0; i < inputData.Length; i++)
            inputData.SetValue(i, (float)random.NextDouble());

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(inputName, inputData)
        };

        Console.WriteLine("Running inference on GPU...");
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Прогреваем (первый запуск медленнее)
        using (var results = session.Run(inputs)) { }

        // Реальный тест
        sw.Restart();
        for (int i = 0; i < 10; i++)
        {
            using var results = session.Run(inputs);
        }
        sw.Stop();

        Console.WriteLine($"\n✓ Success! 10 inferences in {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"  Average: {sw.ElapsedMilliseconds / 10.0:F2}ms per inference");
        Console.WriteLine("\nGPU is working correctly!");
    }
}