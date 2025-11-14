using System;
using System.Collections.Generic;

namespace PhotoBank.ViewModel.Dto;

public class UserDto : IHasId<Guid>
{
    public required Guid Id { get; set; }
    public required string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? TelegramUserId { get; set; }
    public TimeSpan? TelegramSendTimeUtc { get; set; }
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<UserAccessProfileAssignmentDto> AccessProfiles { get; init; } =
        Array.Empty<UserAccessProfileAssignmentDto>();
}
