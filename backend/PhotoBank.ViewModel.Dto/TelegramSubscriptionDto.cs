using System;

namespace PhotoBank.ViewModel.Dto;

public class TelegramSubscriptionDto
{
    public required string TelegramUserId { get; init; } = string.Empty;

    public required TimeSpan TelegramSendTimeUtc { get; init; }
}
