using PhotoBank.ViewModel.Dto;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoBank.Services.Identity;

public interface IUserProfileService
{
    Task<UserDto?> GetCurrentUserAsync(ClaimsPrincipal claimsPrincipal, CancellationToken cancellationToken = default);

    Task<UpdateUserResult> UpdateCurrentUserAsync(ClaimsPrincipal claimsPrincipal, UpdateUserDto dto, CancellationToken cancellationToken = default);
}
