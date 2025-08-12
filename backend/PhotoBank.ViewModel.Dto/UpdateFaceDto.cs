using System.Text.Json.Serialization;

namespace PhotoBank.ViewModel.Dto;

[JsonNumberHandling(JsonNumberHandling.Strict)]
public class UpdateFaceDto
{
    public required int FaceId { get; init; }
    public required int PersonId { get; init; }
}
