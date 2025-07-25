using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace PhotoBank.InsightFaceApiClient
{
    public interface IInsightFaceApiClient
    {
        Task<bool> HealthAsync();
        Task<string> GetPersonsAsync();
        Task<string> RegisterAsync(int personId, Stream fileStream, string fileName = "upload.jpg");
        Task<string> RecognizeAsync(Stream fileStream, string fileName = "upload.jpg");
        Task<string> BatchRecognizeAsync(IEnumerable<(Stream fileStream, string fileName)> files);
    }

    public class InsightFaceApiClient : IInsightFaceApiClient
    {
        private readonly HttpClient _httpClient;

        public InsightFaceApiClient()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5555") };
        }

        public async Task<bool> HealthAsync()
        {
            var response = await _httpClient.GetAsync("/health");
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }

        public async Task<string> GetPersonsAsync()
        {
            var response = await _httpClient.GetAsync("/persons");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> RegisterAsync(int personId, Stream fileStream, string fileName = "upload.jpg")
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            content.Add(fileContent, "file", fileName);

            var response = await _httpClient.PostAsync($"/register?person_id={personId}", content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> RecognizeAsync(Stream fileStream, string fileName = "upload.jpg")
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            content.Add(fileContent, "file", fileName);

            var response = await _httpClient.PostAsync("/recognize", content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> BatchRecognizeAsync(IEnumerable<(Stream fileStream, string fileName)> files)
        {
            using var content = new MultipartFormDataContent();
            foreach (var (stream, name) in files)
            {
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                content.Add(fileContent, "files", name);
            }

            var response = await _httpClient.PostAsync("/batch_recognize", content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
