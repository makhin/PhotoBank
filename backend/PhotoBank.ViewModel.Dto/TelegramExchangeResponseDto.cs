namespace PhotoBank.ViewModel.Dto;

public sealed class TelegramExchangeResponseDto
{
    public required string AccessToken { get; init; } = string.Empty;

    public required int ExpiresIn { get; init; }
}
