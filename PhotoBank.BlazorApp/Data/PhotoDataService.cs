using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using PhotoBank.Dto;
using PhotoBank.Dto.View;
using PhotoBank.Services;

namespace PhotoBank.BlazorApp.Data
{
    public class PhotoDataService : IPhotoDataService
    {
        private readonly HttpClient _httpClient;

        public PhotoDataService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<PhotoDto>> GetAllPhotos()
        {
            return await JsonSerializer.DeserializeAsync<IEnumerable<PhotoDto>>
                (await _httpClient.GetStreamAsync($"api/photo"), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        }

        public async Task<PhotoDto> GetPhotoById(int photoId)
        {
            var streamAsync = await _httpClient.GetStreamAsync($"api/photo/{photoId}");

            return await JsonSerializer.DeserializeAsync<PhotoDto>
                (streamAsync, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}
