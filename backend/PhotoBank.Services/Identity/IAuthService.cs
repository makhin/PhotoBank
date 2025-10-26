using Microsoft.AspNetCore.Identity;
using PhotoBank.ViewModel.Dto;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoBank.Services.Identity;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);

    Task<IdentityResult> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);

    Task<TelegramSubscriptionsResult> GetTelegramSubscriptionsAsync(string? presentedServiceKey, CancellationToken cancellationToken = default);

    Task<TelegramExchangeResult> ExchangeTelegramAsync(string? telegramUserId, string? username, string? languageCode, string? presentedServiceKey, CancellationToken cancellationToken = default);
}
