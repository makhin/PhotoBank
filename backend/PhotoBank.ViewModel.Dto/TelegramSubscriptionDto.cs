using System;

namespace PhotoBank.ViewModel.Dto;

public class TelegramSubscriptionDto
{
    public required long TelegramUserId { get; init; }

    public required TimeSpan TelegramSendTimeUtc { get; init; }
}
