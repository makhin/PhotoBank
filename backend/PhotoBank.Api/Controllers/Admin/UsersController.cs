using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.Models;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Api.Controllers.Admin;

[Route("api/admin/[controller]")]
[ApiController]
[Authorize]
public class UsersController(UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserWithClaimsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserWithClaimsDto>>> GetAllAsync()
    {
        var users = await userManager.Users.AsNoTracking().ToListAsync();
        var result = new List<UserWithClaimsDto>();
        foreach (var user in users)
        {
            var claims = await userManager.GetClaimsAsync(user);
            result.Add(new UserWithClaimsDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                TelegramUserId = user.TelegramUserId,
                TelegramSendTimeUtc = user.TelegramSendTimeUtc,
                Claims = claims.Select(c => new ClaimDto { Type = c.Type, Value = c.Value })
            });
        }
        return Ok(result);
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

    [HttpPut("{id}/claims")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetClaimsAsync(string id, [FromBody] IEnumerable<ClaimDto> claimsDto)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        var existing = await userManager.GetClaimsAsync(user);
        var removeResult = await userManager.RemoveClaimsAsync(user, existing);
        if (!removeResult.Succeeded)
            return BadRequest(removeResult.Errors);

        var addResult = await userManager.AddClaimsAsync(user, claimsDto.Select(c => new Claim(c.Type, c.Value)));
        if (!addResult.Succeeded)
            return BadRequest(addResult.Errors);

        return Ok();
    }
}
