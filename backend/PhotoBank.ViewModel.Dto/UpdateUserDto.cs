using System;

namespace PhotoBank.ViewModel.Dto;

public class UpdateUserDto
{
    public string? PhoneNumber { get; init; }
    public string? TelegramUserId { get; init; }
    public TimeSpan? TelegramSendTimeUtc { get; init; }
}
