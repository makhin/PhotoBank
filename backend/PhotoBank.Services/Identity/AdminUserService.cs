using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhotoBank.AccessControl;
using PhotoBank.DbContext.Models;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Services.Identity;

public sealed class AdminUserService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    AccessControlDbContext accessControlDbContext) : IAdminUserService
{
    public async Task<IReadOnlyCollection<UserDto>> GetUsersAsync(UsersQuery query, CancellationToken cancellationToken = default)
    {
        var usersQuery = userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchPattern = $"%{query.Search.Trim()}%";
            usersQuery = usersQuery.Where(user =>
                (user.Email != null && EF.Functions.Like(user.Email, searchPattern)) ||
                (user.PhoneNumber != null && EF.Functions.Like(user.PhoneNumber, searchPattern)));
        }

        if (query.HasTelegram is not null)
        {
            usersQuery = query.HasTelegram.Value
                ? usersQuery.Where(user => user.TelegramUserId != null)
                : usersQuery.Where(user => user.TelegramUserId == null);
        }

        usersQuery = query.SortField switch
        {
            UsersQuery.SortEmail => query.SortDescending
                ? usersQuery.OrderByDescending(user => user.Email)
                : usersQuery.OrderBy(user => user.Email),
            UsersQuery.SortPhone => query.SortDescending
                ? usersQuery.OrderByDescending(user => user.PhoneNumber)
                : usersQuery.OrderBy(user => user.PhoneNumber),
            UsersQuery.SortTelegram => query.SortDescending
                ? usersQuery.OrderByDescending(user => user.TelegramUserId)
                : usersQuery.OrderBy(user => user.TelegramUserId),
            _ => usersQuery.OrderBy(user => user.Email)
        };

        var users = await usersQuery
            .Skip(query.Offset)
            .Take(query.Limit)
            .ToListAsync(cancellationToken);

        var assignmentsLookup = await LoadAccessProfileAssignmentsAsync(users, cancellationToken);

        var result = new List<UserDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            assignmentsLookup.TryGetValue(user.Id, out var profileIds);
            result.Add(UserDtoMapper.Map(user, roles, profileIds));
        }

        return result;
    }

    public async Task<CreateUserResult> CreateAsync(CreateUserDto dto, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var user = new ApplicationUser { UserName = dto.Email, Email = dto.Email, PhoneNumber = dto.PhoneNumber };
        var result = await userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(error => string.Equals(error.Code, nameof(IdentityErrorDescriber.DuplicateUserName), StringComparison.OrdinalIgnoreCase)
                                           || string.Equals(error.Code, nameof(IdentityErrorDescriber.DuplicateEmail), StringComparison.OrdinalIgnoreCase)))
            {
                return CreateUserResult.ConflictFailure(result);
            }

            return CreateUserResult.BadRequestFailure(result);
        }

        if (dto.Roles?.Any() == true)
        {
            foreach (var role in dto.Roles.Distinct())
            {
                if (await roleManager.RoleExistsAsync(role))
                {
                    await userManager.AddToRoleAsync(user, role);
                }
            }
        }

        var createdRoles = await userManager.GetRolesAsync(user);
        return CreateUserResult.Success(UserDtoMapper.Map(user, createdRoles));
    }

    public async Task<UpdateUserResult> UpdateAsync(string id, UpdateUserDto dto, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return UpdateUserResult.NotFoundResult();
        }

        user.PhoneNumber = dto.PhoneNumber;

        if (dto.TelegramUserId is not null)
        {
            if (!TelegramUserIdParser.TryParse(dto.TelegramUserId, out var parsedTelegramUserId, out var error))
            {
                return UpdateUserResult.ValidationError(nameof(dto.TelegramUserId), error ?? string.Empty);
            }

            user.TelegramUserId = parsedTelegramUserId;
        }

        user.TelegramSendTimeUtc = dto.TelegramSendTimeUtc;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return UpdateUserResult.IdentityFailure(result);
        }

        return UpdateUserResult.Success();
    }

    public async Task<IdentityOperationResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return IdentityOperationResult.NotFoundResult();
        }

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return IdentityOperationResult.Failure(result);
        }

        return IdentityOperationResult.Success();
    }

    public async Task<IdentityOperationResult> ResetPasswordAsync(string id, ResetPasswordDto dto, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return IdentityOperationResult.NotFoundResult();
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await userManager.ResetPasswordAsync(user, token, dto.NewPassword);
        if (!resetResult.Succeeded)
        {
            return IdentityOperationResult.Failure(resetResult);
        }

        return IdentityOperationResult.Success();
    }

    public async Task<IdentityOperationResult> SetRolesAsync(string id, SetRolesDto dto, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return IdentityOperationResult.NotFoundResult();
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var toRemove = currentRoles.Except(dto.Roles).ToArray();
        var toAdd = dto.Roles.Except(currentRoles).ToArray();

        if (toRemove.Length > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, toRemove);
            if (!removeResult.Succeeded)
            {
                return IdentityOperationResult.Failure(removeResult);
            }
        }

        foreach (var role in toAdd)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            var addResult = await userManager.AddToRoleAsync(user, role);
            if (!addResult.Succeeded)
            {
                return IdentityOperationResult.Failure(addResult);
            }
        }

        return IdentityOperationResult.Success();
    }

    private async Task<Dictionary<string, IReadOnlyCollection<int>>> LoadAccessProfileAssignmentsAsync(
        IReadOnlyCollection<ApplicationUser> users,
        CancellationToken cancellationToken)
    {
        if (users.Count == 0)
        {
            return new Dictionary<string, IReadOnlyCollection<int>>();
        }

        var userIds = users.Select(u => u.Id).ToArray();

        var assignments = await accessControlDbContext.UserAccessProfiles
            .AsNoTracking()
            .Where(link => userIds.Contains(link.UserId))
            .Select(link => new { link.UserId, link.ProfileId })
            .ToListAsync(cancellationToken);

        return assignments
            .GroupBy(x => x.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<int>)group.Select(x => x.ProfileId).ToArray());
    }
}
