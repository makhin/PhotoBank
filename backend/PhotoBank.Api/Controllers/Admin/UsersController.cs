using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.Models;
using PhotoBank.ViewModel.Dto;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Collections.Generic;

namespace PhotoBank.Api.Controllers.Admin;

[Route("admin/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAllAsync([FromQuery] UsersQuery query)
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
            .ToListAsync();

        var result = users.Select(user => new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            TelegramUserId = user.TelegramUserId,
            TelegramSendTimeUtc = user.TelegramSendTimeUtc
        });
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateUserDto dto)
    {
        var user = new ApplicationUser { UserName = dto.Email, Email = dto.Email, PhoneNumber = dto.PhoneNumber };
        var result = await userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(error => string.Equals(error.Code, nameof(IdentityErrorDescriber.DuplicateUserName), StringComparison.OrdinalIgnoreCase)
                                           || string.Equals(error.Code, nameof(IdentityErrorDescriber.DuplicateEmail), StringComparison.OrdinalIgnoreCase)))
            {
                return Conflict(result.Errors);
            }

            return BadRequest(result.Errors);
        }

        if (dto.Roles?.Any() == true)
        {
            foreach (var role in dto.Roles.Distinct())
                if (await roleManager.RoleExistsAsync(role))
                    await userManager.AddToRoleAsync(user, role);
        }

        var created = new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber,
            TelegramUserId = user.TelegramUserId,
            TelegramSendTimeUtc = user.TelegramSendTimeUtc
        };
        return CreatedAtAction(nameof(GetAllAsync), new { id = user.Id }, created);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateAsync(string id, [FromBody] UpdateUserDto dto)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        user.PhoneNumber = dto.PhoneNumber;
        if (dto.TelegramUserId.HasValue)
        {
            user.TelegramUserId = dto.TelegramUserId;
        }
        user.TelegramSendTimeUtc = dto.TelegramSendTimeUtc;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        var r = await userManager.DeleteAsync(user);
        if (!r.Succeeded) return BadRequest(r.Errors);
        return NoContent();
    }

    [HttpPost("{id}/reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPasswordAsync(string id, [FromBody] ResetPasswordDto dto)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var r = await userManager.ResetPasswordAsync(user, token, dto.NewPassword);
        if (!r.Succeeded) return BadRequest(r.Errors);
        return NoContent();
    }

    [HttpPut("{id}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetRolesAsync(string id, [FromBody] SetRolesDto dto)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var current = await userManager.GetRolesAsync(user);
        var toRemove = current.Except(dto.Roles).ToArray();
        var toAdd = dto.Roles.Except(current).ToArray();

        if (toRemove.Length > 0)
        {
            var rr = await userManager.RemoveFromRolesAsync(user, toRemove);
            if (!rr.Succeeded) return BadRequest(rr.Errors);
        }
        foreach (var role in toAdd)
            if (await roleManager.RoleExistsAsync(role))
            {
                var ar = await userManager.AddToRoleAsync(user, role);
                if (!ar.Succeeded) return BadRequest(ar.Errors);
            }

        return NoContent();
    }
}

public sealed class UsersQuery : IValidatableObject
{
    public const string SortEmail = "email";
    public const string SortPhone = "phone";
    public const string SortTelegram = "telegram";

    private const int DefaultLimit = 50;
    private const int MaxLimit = 200;
    private static readonly HashSet<string> AllowedSorts = new(StringComparer.OrdinalIgnoreCase)
    {
        SortEmail,
        $"-{SortEmail}",
        SortPhone,
        $"-{SortPhone}",
        SortTelegram,
        $"-{SortTelegram}"
    };

    [Range(1, MaxLimit)]
    public int Limit { get; init; } = DefaultLimit;

    [Range(0, int.MaxValue)]
    public int Offset { get; init; }

    public string? Sort { get; init; } = SortEmail;

    public string? Search { get; init; }

    public bool? HasTelegram { get; init; }

    private string SortOrDefault => string.IsNullOrWhiteSpace(Sort) ? SortEmail : Sort;

    public string SortField
    {
        get
        {
            var value = SortOrDefault;
            return (value.StartsWith("-", StringComparison.Ordinal) ? value[1..] : value).ToLowerInvariant();
        }
    }

    public bool SortDescending => SortOrDefault.StartsWith("-", StringComparison.Ordinal);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!AllowedSorts.Contains(SortOrDefault))
        {
            yield return new ValidationResult($"Sort '{Sort}' is not supported.", new[] { nameof(Sort) });
        }
    }
}
