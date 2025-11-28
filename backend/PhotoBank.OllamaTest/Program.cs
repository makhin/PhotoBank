using OllamaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImageCaptioning
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Подключение к Ollama
                var uri = new Uri("http://localhost:11434");
                var ollama = new OllamaApiClient(uri);

                // Выбор модели
                ollama.SelectedModel = "qwen2.5vl:7b";
                //ollama.SelectedModel = "granite3.2-vision";
                // ollama.SelectedModel = "minicpm-v";
                // Альтернативы: "llava:7b", "minicpm-v", "qwen2.5vl:7b"

                // Создание чата
                var chat = new Chat(ollama);

                // Путь к каталогу с изображениями
                string directoryPath = @"c:\temp\test"; // Укажите путь к каталогу

                // Получаем все файлы изображений
                var imageExtensions = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif", "*.bmp" };
                var imageFiles = new List<string>();

                foreach (var extension in imageExtensions)
                {
                    imageFiles.AddRange(Directory.GetFiles(directoryPath, extension, SearchOption.TopDirectoryOnly));
                }

                Console.WriteLine($"Найдено изображений: {imageFiles.Count}\n");

                // Список для хранения времен обработки
                var processingTimes = new List<TimeSpan>();

                // Общий таймер
                var totalStopwatch = Stopwatch.StartNew();

                // Промпт для описания
                //string prompt = "Make short (one sentence) caption for this image. Do not write: 'This image is...'";
                //string prompt = "Tell me between 5 and 15 tags for this image. Just comma separated words. Do not repeat the same word.";
                //string prompt = "Classify this image is adult content, is racy content.";
                const string prompt = @"You are an image analysis service used inside a photo management system.

                                    Your job:
                                    - Analyze the image.
                                    - Generate:
                                      1) A concise caption in English.
                                      2) A list of high-level tags (one or two words) that best describe objects, scenes, activities and attributes in the image. Each tag should have confidence level 0..1. So it should be an array of objects with tag and confidence. Maximum 10 tags, not more!!!.
                                      3.1) Is nsfw: true/false.
                                      3.2) Is racy: true/false.
                                      4) Two dominant colors in the image.
                                    - Focus on the most prominent and relevant aspects of the image.

                                    Return a single JSON object:
                                    {
                                      ""caption"": ""caption:string"",
                                      ""tags"": [{""string"", ""float""}, {""string"", ""float""}, ""...""],
                                      ""is_nsfw"": ""boolean"",
                                      ""is_racy"": ""boolean"",
                                      ""dominant_colors"": [""color1"", ""color2""]
                                    }

                                    Rules:
                                    - Only JSON, no comments.
                                    - Tags must be lowercase English.
                                    - Do not include duplicates (plural, singular).
                                    - Do not invent tags that are not visually grounded in the image.";

                // Обработка каждого изображения
                int currentImage = 0;
                foreach (var imagePath in imageFiles)
                {
                    currentImage++;
                    Console.WriteLine($"\n{'=', 80}");
                    Console.WriteLine($"[{currentImage}/{imageFiles.Count}] Обрабатываю: {Path.GetFileName(imagePath)}");
                    Console.WriteLine(new string('=', 80));

                    try
                    {
                        // Таймер для текущего изображения
                        var imageStopwatch = Stopwatch.StartNew();

                        // Чтение изображения
                        byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);

                        // Подготовка изображения для API
                        var images = new List<IEnumerable<byte>> { imageBytes };

                        // Отправка запроса и получение ответа
                        Console.WriteLine("Анализирую изображение...\n");
                        Console.Write("Результат: ");

                        await foreach (var token in chat.SendAsync(
                                           message: prompt,
                                           imagesAsBytes: images,
                                           CancellationToken.None))
                        {
                            Console.Write(token);
                        }

                        imageStopwatch.Stop();
                        processingTimes.Add(imageStopwatch.Elapsed);

                        Console.WriteLine($"\n\nВремя обработки: {imageStopwatch.Elapsed.TotalSeconds:F2} сек\n");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при обработке {Path.GetFileName(imagePath)}: {ex.Message}");
                    }
                }

                totalStopwatch.Stop();

                Console.WriteLine("\n\n" + new string('=', 80));
                Console.WriteLine("СТАТИСТИКА");
                Console.WriteLine(new string('=', 80));
                Console.WriteLine($"Всего обработано изображений: {processingTimes.Count}");
                Console.WriteLine($"Общее время: {totalStopwatch.Elapsed.TotalSeconds:F2} сек ({totalStopwatch.Elapsed:hh\\:mm\\:ss})");

                if (processingTimes.Count > 0)
                {
                    var averageTime = processingTimes.Average(t => t.TotalSeconds);
                    var minTime = processingTimes.Min(t => t.TotalSeconds);
                    var maxTime = processingTimes.Max(t => t.TotalSeconds);

                    Console.WriteLine($"Среднее время на изображение: {averageTime:F2} сек");
                    Console.WriteLine($"Минимальное время: {minTime:F2} сек");
                    Console.WriteLine($"Максимальное время: {maxTime:F2} сек");
                }

                Console.WriteLine("\nВсе изображения обработаны!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }
    }
}