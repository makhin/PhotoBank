using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;
using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoBank.Services.Identity;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenService tokenService,
    ITelegramServiceKeyValidator serviceKeyValidator) : IAuthService
{
    public async Task<LoginResult> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(
            u => u.Email == request.Email,
            cancellationToken);

        if (user is null)
        {
            return LoginResult.Unauthorized();
        }

        var passwordResult = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!passwordResult.Succeeded)
        {
            return LoginResult.Unauthorized();
        }

        var token = await CreateTokenAsync(user, request.RememberMe);
        return LoginResult.Success(new LoginResponseDto { Token = token });
    }

    public async Task<IdentityResult> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var user = new ApplicationUser { UserName = request.Email, Email = request.Email };
        return await userManager.CreateAsync(user, request.Password);
    }

    public async Task<TelegramSubscriptionsResult> GetTelegramSubscriptionsAsync(
        string? presentedServiceKey,
        CancellationToken cancellationToken = default)
    {
        var validation = serviceKeyValidator.Validate(presentedServiceKey);
        if (!validation.IsValid)
        {
            return TelegramSubscriptionsResult.Failure(validation.Problem!);
        }

        var subscriptions = await userManager.Users
            .AsNoTracking()
            .Where(u => u.TelegramUserId != null && u.TelegramSendTimeUtc != null)
            .OrderBy(u => u.TelegramSendTimeUtc)
            .Select(u => new TelegramSubscriptionDto
            {
                TelegramUserId = u.TelegramUserId!.Value.ToString(CultureInfo.InvariantCulture),
                TelegramSendTimeUtc = u.TelegramSendTimeUtc!.Value
            })
            .ToListAsync(cancellationToken);

        return TelegramSubscriptionsResult.Success(subscriptions);
    }

    public async Task<TelegramExchangeResult> ExchangeTelegramAsync(
        string? telegramUserId,
        string? username,
        string? presentedServiceKey,
        CancellationToken cancellationToken = default)
    {
        var validation = serviceKeyValidator.Validate(presentedServiceKey);
        if (!validation.IsValid)
        {
            return TelegramExchangeResult.Failure(validation.Problem!);
        }

        _ = username;

        if (!TelegramUserIdParser.TryParse(telegramUserId, out var parsedTelegramUserId, out var error) ||
            parsedTelegramUserId is null)
        {
            var message = string.IsNullOrEmpty(error) ? "Telegram user ID is required." : error!;
            return TelegramExchangeResult.ValidationError(
                new ValidationFailure(nameof(telegramUserId), message));
        }

        var user = await userManager.Users.FirstOrDefaultAsync(
            u => u.TelegramUserId == parsedTelegramUserId.Value,
            cancellationToken);

        if (user is null)
        {
            return TelegramExchangeResult.Failure(new ProblemInfo(
                StatusCodes.Status403Forbidden,
                "Telegram not linked",
                "Ask admin to link your Telegram"));
        }

        var token = await CreateTokenAsync(user, rememberMe: false);
        var expiresIn = 3600;

        return TelegramExchangeResult.Success(new TelegramExchangeResponseDto
        {
            AccessToken = token,
            ExpiresIn = expiresIn
        });
    }

    private async Task<string> CreateTokenAsync(ApplicationUser user, bool rememberMe)
    {
        var roles = await userManager.GetRolesAsync(user);
        var roleClaims = roles.Select(role => new Claim(ClaimTypes.Role, role));
        var userClaims = await userManager.GetClaimsAsync(user);
        var claims = userClaims.Concat(roleClaims).ToList();

        return tokenService.CreateToken(user, rememberMe, claims);
    }
}
