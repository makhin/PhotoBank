using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenService tokenService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await signInManager.PasswordSignInAsync(request.Email, request.Password, false, false);
        if (!result.Succeeded)
            return BadRequest();

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return BadRequest();

        var token = tokenService.CreateToken(user);
        return Ok(new LoginResponseDto { Token = token });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var user = new ApplicationUser { UserName = request.Email, Email = request.Email };
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok();
    }

    [HttpGet("user")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUser()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
            return NotFound();

        return Ok(new UserDto
        {
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Telegram = user.Telegram
        });
    }

    [HttpPut("user")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUser([FromBody] UpdateUserDto dto)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
            return NotFound();

        user.PhoneNumber = dto.PhoneNumber;
        user.Telegram = dto.Telegram;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok();
    }
}

