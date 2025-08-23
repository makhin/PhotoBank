using System.Text.Json.Serialization;
using PhotoBank.DbContext.Models;

namespace PhotoBank.ViewModel.Dto;

[JsonNumberHandling(JsonNumberHandling.Strict)]
public class UpdateFaceIdentityDto
{
    public required int FaceId { get; init; }
    public int? PersonId { get; init; }
    public required IdentityStatus IdentityStatus { get; init; }
}
