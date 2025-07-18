namespace PhotoBank.ViewModel.Dto;

public class UserWithClaimsDto
{
    public required string Id { get; set; } = string.Empty;
    public required string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Telegram { get; set; }
    public IEnumerable<ClaimDto> Claims { get; set; } = new List<ClaimDto>();
}
