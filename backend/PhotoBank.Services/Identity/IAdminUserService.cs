using Microsoft.AspNetCore.Identity;
using PhotoBank.ViewModel.Dto;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoBank.Services.Identity;

public interface IAdminUserService
{
    Task<IReadOnlyCollection<UserDto>> GetUsersAsync(UsersQuery query, CancellationToken cancellationToken = default);

    Task<CreateUserResult> CreateAsync(CreateUserDto dto, CancellationToken cancellationToken = default);

    Task<UpdateUserResult> UpdateAsync(string id, UpdateUserDto dto, CancellationToken cancellationToken = default);

    Task<IdentityOperationResult> DeleteAsync(string id, CancellationToken cancellationToken = default);

    Task<IdentityOperationResult> ResetPasswordAsync(string id, ResetPasswordDto dto, CancellationToken cancellationToken = default);

    Task<IdentityOperationResult> SetRolesAsync(string id, SetRolesDto dto, CancellationToken cancellationToken = default);
}
