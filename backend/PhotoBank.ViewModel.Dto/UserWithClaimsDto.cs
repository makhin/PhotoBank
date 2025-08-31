using System;

namespace PhotoBank.ViewModel.Dto;

public class UserWithClaimsDto : IHasId<string>
{
    public required string Id { get; set; } = string.Empty;
    public required string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public long? TelegramUserId { get; set; }
    public TimeSpan? TelegramSendTimeUtc { get; set; }
    public IEnumerable<ClaimDto> Claims { get; set; } = new List<ClaimDto>();
}
