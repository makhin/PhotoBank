using System.Text.Json.Serialization;

namespace PhotoBank.ViewModel.Dto
{
    [JsonNumberHandling(JsonNumberHandling.Strict)]
    public class GeoPointDto
    {
        public required double Latitude { get; init; }
        public required double Longitude { get; init; }
    }
}
