using System.Diagnostics;
using System.Text;
using System.Text.Json;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.MAUI.Blazor.Services
{
    internal class RestService : IRestService
    {
        HttpClient _client;
        JsonSerializerOptions _serializerOptions;

        public QueryResult result { get; private set; }

        public RestService()
        {
#if DEBUG
            HttpClientHandler insecureHandler = GetInsecureHandler();
            _client = new HttpClient(insecureHandler);
#else
            _client = new HttpClient();
#endif
            _serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        public async Task<QueryResult> GetPhotos(FilterDto request)
        {
            result = new QueryResult();

            Uri uri = new Uri(string.Format(Constants.RestUrl, "GetPhotos"));

            // Assuming you need to pass a FilterDto object, a string, and two nullable integers            
            string json = JsonSerializer.Serialize(request, _serializerOptions);
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await _client.PostAsync(uri, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    result = JsonSerializer.Deserialize<QueryResult>(responseContent, _serializerOptions);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(@"\tERROR {0}", ex.Message);
            }

            return result;
        }

        public async Task<PhotoDto> GetPhoto(int id)
        {
            PhotoDto photo = null;

            var p = $"GetPhoto?id={id}";

            Uri uri = new Uri(string.Format(Constants.RestUrl, p));
            try
            {
                HttpResponseMessage response = await _client.GetAsync(uri);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    photo = JsonSerializer.Deserialize<PhotoDto>(content, _serializerOptions);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(@"\tERROR {0}", ex.Message);
            }

            return photo;
        }
        public async Task<IEnumerable<StorageDto>> GetStorages()
        {
            IEnumerable<StorageDto> storages = null;

            Uri uri = new Uri(string.Format(Constants.RestUrl, "GetStorages"));
            try
            {
                HttpResponseMessage response = await _client.GetAsync(uri);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    storages = JsonSerializer.Deserialize<IEnumerable<StorageDto>>(content, _serializerOptions);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(@"\tERROR {0}", ex.Message);
            }

            return storages;
        }
        public async Task<IEnumerable<PathDto>> GetPaths()
        {
            IEnumerable<PathDto> paths = null;

            Uri uri = new Uri(string.Format(Constants.RestUrl, "GetPaths"));
            try
            {
                HttpResponseMessage response = await _client.GetAsync(uri);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    paths = JsonSerializer.Deserialize<IEnumerable<PathDto>>(content, _serializerOptions);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(@"\tERROR {0}", ex.Message);
            }

            return paths;
        }

        public async Task<IEnumerable<PersonDto>> GetPersons()
        {
            IEnumerable<PersonDto> persons = null;

            Uri uri = new Uri(string.Format(Constants.RestUrl, "GetPersons"));
            try
            {
                HttpResponseMessage response = await _client.GetAsync(uri);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    persons = JsonSerializer.Deserialize<IEnumerable<PersonDto>>(content, _serializerOptions);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(@"\tERROR {0}", ex.Message);
            }

            return persons;
        }

        public async Task<IEnumerable<TagDto>> GetTags()
        {
            IEnumerable<TagDto> tags = null;

            Uri uri = new Uri(string.Format(Constants.RestUrl, "GetTags"));
            try
            {
                HttpResponseMessage response = await _client.GetAsync(uri);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    tags = JsonSerializer.Deserialize<IEnumerable<TagDto>>(content, _serializerOptions);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(@"\tERROR {0}", ex.Message);
            }

            return tags;
        }

        private HttpClientHandler GetInsecureHandler()
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                if (cert != null && cert.Issuer.Equals("CN=localhost"))
                    return true;
                return errors == System.Net.Security.SslPolicyErrors.None;
            };
            return handler;
        }
    }
}
