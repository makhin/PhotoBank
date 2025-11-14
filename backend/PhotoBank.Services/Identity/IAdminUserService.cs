using System;
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

    Task<UpdateUserResult> UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken cancellationToken = default);

    Task<IdentityOperationResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IdentityOperationResult> ResetPasswordAsync(Guid id, ResetPasswordDto dto, CancellationToken cancellationToken = default);

    Task<IdentityOperationResult> SetRolesAsync(Guid id, SetRolesDto dto, CancellationToken cancellationToken = default);
}
