using System;

namespace PhotoBank.ViewModel.Dto;

public class UserDto
{
    public required string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public long? TelegramUserId { get; set; }
    public TimeSpan? TelegramSendTimeUtc { get; set; }
}
