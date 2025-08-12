using System.Text.Json.Serialization;

namespace PhotoBank.ViewModel.Dto;

[JsonNumberHandling(JsonNumberHandling.Strict)]
public class UpdateUserDto
{
    public string? PhoneNumber { get; init; }
    public string? Telegram { get; init; }
}
