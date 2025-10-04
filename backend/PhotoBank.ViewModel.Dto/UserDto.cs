using System;
using System.Collections.Generic;

namespace PhotoBank.ViewModel.Dto;

public class UserDto : IHasId<string>
{
    public required string Id { get; set; } = string.Empty;
    public required string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? TelegramUserId { get; set; }
    public TimeSpan? TelegramSendTimeUtc { get; set; }
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
}
