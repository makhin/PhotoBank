using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.Models;
using PhotoBank.ViewModel.Dto;
using System.Linq;

namespace PhotoBank.Api.Controllers.Admin;

[Route("admin/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAllAsync()
    {
        var users = await userManager.Users.AsNoTracking().ToListAsync();
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
        if (!result.Succeeded) return BadRequest(result.Errors);

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
        user.TelegramUserId = dto.TelegramUserId;
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
