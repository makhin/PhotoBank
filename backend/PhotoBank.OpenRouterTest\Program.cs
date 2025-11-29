using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public class ImageDescriptor
{
    private readonly HttpClient _client;
    private readonly string _apiKey;
    private const string BaseUrl = "https://openrouter.ai/api/v1/chat/completions";

    private const string SystemPrompt = @"Analyze the image and return ONLY a valid JSON object with this exact structure:
{
  ""description"": ""brief description in 1-2 sentences"",
  ""tags"": [
    {""tag"": ""object or action"", ""confidence"": 0.95},
    {""tag"": ""another tag"", ""confidence"": 0.87}
  ],
  ""is_nsfw"": {""value"": false, ""confidence"": 0.99},
  ""is_racy"": {""value"": false, ""confidence"": 0.98},
  ""dominant_colors"": [""#FF5733"", ""#3498DB"", ""#2ECC71""]
}

Rules:
- Tags: 5-15 items, one or two words each, describing objects or actions
- Confidence: 0.0 to 1.0
- Dominant colors: 3-5 hex color codes
- Return ONLY valid JSON, no markdown, no explanations";

    public ImageDescriptor(string apiKey)
    {
        _apiKey = apiKey;
        _client = new HttpClient();
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        _client.DefaultRequestHeaders.Add("HTTP-Referer", "https://yourapp.com");
    }

    public async Task<ImageAnalysisResult> AnalyzeImageAsync(string imagePath)
    {
        try
        {
            var imageBytes = await File.ReadAllBytesAsync(imagePath);
            var base64Image = Convert.ToBase64String(imageBytes);
            var mimeType = GetMimeType(imagePath);

            var requestBody = new
            {
                model = "openai/gpt-4o-mini", // лучше для структурированного JSON
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new
                            {
                                type = "image_url",
                                image_url = new
                                {
                                    url = $"data:{mimeType};base64,{base64Image}"
                                }
                            },
                            new
                            {
                                type = "text",
                                text = SystemPrompt
                            }
                        }
                    }
                },
                max_tokens = 1024,
                temperature = 0.3 // для более стабильного JSON
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync(BaseUrl, content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new ImageAnalysisResult
                {
                    Path = imagePath,
                    Success = false,
                    Error = $"API Error: {response.StatusCode} - {responseText}"
                };
            }

            var apiResponse = JsonSerializer.Deserialize<OpenRouterResponse>(responseText);
            var contentText = apiResponse?.choices?[0]?.message?.content ?? "";

            // Убираем markdown если есть
            contentText = contentText.Trim();
            if (contentText.StartsWith("```json"))
                contentText = contentText.Substring(7);
            if (contentText.StartsWith("```"))
                contentText = contentText.Substring(3);
            if (contentText.EndsWith("```"))
                contentText = contentText.Substring(0, contentText.Length - 3);
            contentText = contentText.Trim();

            var analysis = JsonSerializer.Deserialize<ImageAnalysis>(contentText);

            return new ImageAnalysisResult
            {
                Path = imagePath,
                Analysis = analysis,
                Success = true
            };
        }
        catch (Exception ex)
        {
            return new ImageAnalysisResult
            {
                Path = imagePath,
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<List<ImageAnalysisResult>> BatchProcessAsync(string folderPath, int maxConcurrency = 10, string outputFile = "results.jsonl")
    {
        var extensions = new[] { "*.jpg", "*.jpeg", "*.png", "*.webp" };
        var imagePaths = extensions
            .SelectMany(ext => Directory.GetFiles(folderPath, ext, SearchOption.AllDirectories))
            .ToList();

        Console.WriteLine($"Найдено {imagePaths.Count} изображений");

        // Очищаем файл если существует
        if (File.Exists(outputFile))
            File.Delete(outputFile);

        var results = new List<ImageAnalysisResult>();
        var semaphore = new SemaphoreSlim(maxConcurrency);
        var tasks = new List<Task<ImageAnalysisResult>>();

        foreach (var imagePath in imagePaths)
        {
            await semaphore.WaitAsync();
            
            var task = Task.Run(async () =>
            {
                try
                {
                    return await AnalyzeImageAsync(imagePath);
                }
                finally
                {
                    semaphore.Release();
                }
            });
            
            tasks.Add(task);
        }

        int completed = 0;
        foreach (var task in tasks)
        {
            var result = await task;
            results.Add(result);
            completed++;

            var status = result.Success ? "✓" : "✗";
            Console.WriteLine($"[{completed}/{imagePaths.Count}] {status} {Path.GetFileName(result.Path)}");

            if (!result.Success)
                Console.WriteLine($"  Error: {result.Error}");

            await SaveResultAsync(result, outputFile);
        }

        var successCount = results.Count(r => r.Success);
        Console.WriteLine($"\nГотово: {successCount}/{results.Count} успешно");

        return results;
    }

    private async Task SaveResultAsync(ImageAnalysisResult result, string outputFile)
    {
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions 
        { 
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        await File.AppendAllTextAsync(outputFile, json + Environment.NewLine);
    }

    private string GetMimeType(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLower();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "image/jpeg"
        };
    }
}

public class ImageAnalysisResult
{
    [JsonPropertyName("path")]
    public string Path { get; set; }
    
    [JsonPropertyName("analysis")]
    public ImageAnalysis Analysis { get; set; }
    
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("error")]
    public string Error { get; set; }
}

public class ImageAnalysis
{
    [JsonPropertyName("description")]
    public string Description { get; set; }
    
    [JsonPropertyName("tags")]
    public List<Tag> Tags { get; set; }
    
    [JsonPropertyName("is_nsfw")]
    public Flag IsNsfw { get; set; }
    
    [JsonPropertyName("is_racy")]
    public Flag IsRacy { get; set; }
    
    [JsonPropertyName("dominant_colors")]
    public List<string> DominantColors { get; set; }
}

public class Tag
{
    [JsonPropertyName("tag")]
    public string Name { get; set; }
    
    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
}

public class Flag
{
    [JsonPropertyName("value")]
    public bool Value { get; set; }
    
    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
}

public class OpenRouterResponse
{
    public Choice[] choices { get; set; }
    
    public class Choice
    {
        public Message message { get; set; }
    }
    
    public class Message
    {
        public string content { get; set; }
    }
}

// Использование:
class Program
{
    static async Task Main(string[] args)
    {
        var descriptor = new ImageDescriptor("your_openrouter_api_key");
        
        var results = await descriptor.BatchProcessAsync(
            folderPath: @"C:\path\to\images",
            maxConcurrency: 10,
            outputFile: "analysis_results.jsonl"
        );
        
        // Пример работы с результатами
        foreach (var result in results.Where(r => r.Success))
        {
            Console.WriteLine($"\n{Path.GetFileName(result.Path)}:");
            Console.WriteLine($"  {result.Analysis.Description}");
            Console.WriteLine($"  Tags: {string.Join(", ", result.Analysis.Tags.Select(t => $"{t.Name}({t.Confidence:F2})"))}");
            Console.WriteLine($"  NSFW: {result.Analysis.IsNsfw.Value} ({result.Analysis.IsNsfw.Confidence:F2})");
            Console.WriteLine($"  Colors: {string.Join(", ", result.Analysis.DominantColors)}");
        }
    }
}