using System.Diagnostics;
using System.Text.Json;
using PhotoBank.Dto.View;

namespace PhotoBank.MAUI.Blazor.Services
{
    internal class RestService : IRestService
    {

        HttpClient _client;
        JsonSerializerOptions _serializerOptions;

        public QueryResult Photos { get; private set; }

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

        public async Task<QueryResult> GetPhotos()
        {
            Photos = new QueryResult();

            Uri uri = new Uri(string.Format(Constants.RestUrl, "GetPhotos"));
            try
            {
                HttpResponseMessage response = await _client.GetAsync(uri);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    Photos = JsonSerializer.Deserialize<QueryResult>(content, _serializerOptions);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(@"\tERROR {0}", ex.Message);
            }

            return Photos;
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
