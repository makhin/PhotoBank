using System.Text.Json.Serialization;

namespace PhotoBank.ViewModel.Dto;

[JsonNumberHandling(JsonNumberHandling.Strict)]
public class RegisterRequestDto
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}
