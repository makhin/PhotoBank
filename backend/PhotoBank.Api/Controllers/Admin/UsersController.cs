using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoBank.Services.Identity;
using PhotoBank.ViewModel.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhotoBank.Api.Controllers.Admin;

[Route("admin/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class UsersController(IAdminUserService adminUserService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAllAsync([FromQuery] UsersQuery query)
    {
        var users = await adminUserService.GetUsersAsync(query);
        return Ok(users);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateUserDto dto)
    {
        var result = await adminUserService.CreateAsync(dto);
        if (result.Conflict)
        {
            return Conflict(result.IdentityResult!.Errors);
        }

        if (!result.Succeeded)
        {
            return BadRequest(result.IdentityResult!.Errors);
        }

        return CreatedAtAction(nameof(GetAllAsync), new { id = result.User!.Id }, result.User);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateAsync(string id, [FromBody] UpdateUserDto dto)
    {
        var result = await adminUserService.UpdateAsync(id, dto);
        if (result.NotFound)
            return NotFound();

        if (result.ValidationFailure is not null)
        {
            ModelState.AddModelError(result.ValidationFailure.FieldName, result.ValidationFailure.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        if (!result.Succeeded && result.IdentityResult is not null)
            return BadRequest(result.IdentityResult.Errors);

        return Ok();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(string id)
    {
        var result = await adminUserService.DeleteAsync(id);
        if (result.NotFound)
            return NotFound();

        if (!result.Succeeded && result.IdentityResult is not null)
            return BadRequest(result.IdentityResult.Errors);

        return NoContent();
    }

    [HttpPost("{id}/reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPasswordAsync(string id, [FromBody] ResetPasswordDto dto)
    {
        var result = await adminUserService.ResetPasswordAsync(id, dto);
        if (result.NotFound)
            return NotFound();

        if (!result.Succeeded && result.IdentityResult is not null)
            return BadRequest(result.IdentityResult.Errors);

        return NoContent();
    }

    [HttpPut("{id}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetRolesAsync(string id, [FromBody] SetRolesDto dto)
    {
        var result = await adminUserService.SetRolesAsync(id, dto);
        if (result.NotFound)
            return NotFound();

        if (!result.Succeeded && result.IdentityResult is not null)
            return BadRequest(result.IdentityResult.Errors);

        return NoContent();
    }
}
