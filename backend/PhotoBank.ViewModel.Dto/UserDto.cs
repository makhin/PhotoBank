namespace PhotoBank.ViewModel.Dto;

public class UserDto
{
    public required string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Telegram { get; set; }
}
