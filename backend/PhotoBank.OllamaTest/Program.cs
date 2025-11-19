using OllamaSharp;
using System;
using System.Collections.Generic;
using System.IO;
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
                ollama.SelectedModel = "qwen2.5vl";
                // Альтернативы: "llava:7b", "minicpm-v", "qwen2.5vl:7b"

                // Создание чата
                var chat = new Chat(ollama);

                // Чтение изображения
                string imagePath = "c:\\temp\\test.jpg"; // Укажите путь к файлу
                byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);

                // Подготовка изображения для API
                var images = new List<IEnumerable<byte>> { imageBytes };

                // Промпт для описания
                //string prompt = "Make short (one sentence) caption for this image. Do not write: 'This image is...'";
                //string prompt = "Tell me between 5 and 15 tags for this image. Just comma separated words. Do not repeat the same word.";
                //string prompt = "Classify this image is adult content, is racy content.";
                string prompt = @"You are an image analysis service used inside a photo management system.

                                    Your job:
                                    - Analyze the image.
                                    - Generate:
                                      1) A concise caption in English.
                                      2) A list of 5–15 high-level tags (single words or short phrases) that best describe objects, scenes, activities and attributes in the image. Each tag should have confidence level 0..1.
                                      3.1) Is nsfw: true/false.
                                      3.2) Is racy: true/false.
                                      4) Two dominant colors in the image.
                                    - Focus on the most prominent and relevant aspects of the image.                                    

                                    Return a single JSON object:
                                    {
                                      ""caption"": ""string"",
                                      ""tags"": [{""tag1"", ""confidence1""}, {""tag2"", ""confidence2""}, ""...""],
                                      ""is_nsfw"": ""boolean"",        
                                      ""is_racy"": ""boolean"",
                                      ""dominant_colors"": [""color1"", ""color2""]
                                    }

                                    Rules:
                                    - Only JSON, no comments.
                                    - Tags must be lowercase English.
                                    - Do not include duplicates (plural, singular).
                                    - Do not invent tags that are not visually grounded in the image.";

                // Отправка запроса и получение ответа
                Console.WriteLine("Анализирую изображение...\n");
                Console.Write("Описание: ");

                await foreach (var token in chat.SendAsync(
                                   message: prompt,
                                   imagesAsBytes: images,
                                   CancellationToken.None))
                {
                    Console.Write(token);
                }

                Console.WriteLine("\n\nГотово!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }
    }
}