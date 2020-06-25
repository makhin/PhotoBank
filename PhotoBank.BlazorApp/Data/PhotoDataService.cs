using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using PhotoBank.DbContext.Models;

namespace PhotoBank.BlazorApp.Data
{
    public interface IPhotoDataService
    {
        Task<IEnumerable<Photo>> GetAllPhotos();
        Task<Photo> GetPhotoById(int photoId);
    }

    public class PhotoDataService : IPhotoDataService
    {
        private readonly HttpClient _httpClient;

        public PhotoDataService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<Photo>> GetAllPhotos()
        {
            return await JsonSerializer.DeserializeAsync<IEnumerable<Photo>>
                (await _httpClient.GetStreamAsync($"api/photo"), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        }

        public async Task<Photo> GetPhotoById(int photoId)
        {
            return await JsonSerializer.DeserializeAsync<Photo>
                (await _httpClient.GetStreamAsync($"api/country{photoId}"), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        }
    }
}
