namespace PhotoBank.ViewModel.Dto;

public class CreateUserDto
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public string? PhoneNumber { get; init; }
    public IEnumerable<string>? Roles { get; init; }
}
