using Microsoft.AspNetCore.Identity;
using PhotoBank.DbContext.Models;
using PhotoBank.ViewModel.Dto;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoBank.Services.Identity;

public sealed class UserProfileService(UserManager<ApplicationUser> userManager) : IUserProfileService
{
    public async Task<UserDto?> GetCurrentUserAsync(ClaimsPrincipal claimsPrincipal, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var user = await userManager.GetUserAsync(claimsPrincipal);
        if (user is null)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);
        return UserDtoMapper.Map(user, roles);
    }

    public async Task<UpdateUserResult> UpdateCurrentUserAsync(ClaimsPrincipal claimsPrincipal, UpdateUserDto dto, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var user = await userManager.GetUserAsync(claimsPrincipal);
        if (user is null)
        {
            return UpdateUserResult.NotFoundResult();
        }

        user.PhoneNumber = dto.PhoneNumber ?? user.PhoneNumber;

        if (dto.TelegramUserId is not null)
        {
            if (!TelegramUserIdParser.TryParse(dto.TelegramUserId, out var parsedTelegramUserId, out var error))
            {
                return UpdateUserResult.ValidationError(nameof(dto.TelegramUserId), error ?? string.Empty);
            }

            user.TelegramUserId = parsedTelegramUserId;
        }

        user.TelegramSendTimeUtc = dto.TelegramSendTimeUtc ?? user.TelegramSendTimeUtc;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return UpdateUserResult.IdentityFailure(result);
        }

        return UpdateUserResult.Success();
    }
}
