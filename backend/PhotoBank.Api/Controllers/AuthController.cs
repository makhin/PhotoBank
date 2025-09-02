using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Api;
using PhotoBank.AccessControl;
using PhotoBank.ViewModel.Dto;
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;

namespace PhotoBank.Api.Controllers;

[Route("[controller]")]
[ApiController]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    RoleManager<IdentityRole> roleManager,
    ITokenService tokenService,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await signInManager.PasswordSignInAsync(request.Email, request.Password, request.RememberMe, false);
        if (!result.Succeeded)
            return BadRequest();

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return BadRequest();

        var claims = await userManager.GetClaimsAsync(user);
        var roleNames = await userManager.GetRolesAsync(user);
        foreach (var name in roleNames)
        {
            var role = await roleManager.FindByNameAsync(name);
            if (role == null)
                continue;
            var roleClaims = await roleManager.GetClaimsAsync(role);
            claims = claims.Concat(roleClaims).ToList();
        }

        var token = tokenService.CreateToken(user, request.RememberMe, claims);
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
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            TelegramUserId = user.TelegramUserId,
            TelegramSendTimeUtc = user.TelegramSendTimeUtc
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

        user.PhoneNumber = dto.PhoneNumber ?? user.PhoneNumber;
        user.TelegramUserId = dto.TelegramUserId;
        user.TelegramSendTimeUtc = dto.TelegramSendTimeUtc ?? user.TelegramSendTimeUtc;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok();
    }

    public record TelegramExchangeRequest(long TelegramUserId, string? Username);
    public record TelegramExchangeResponse(string AccessToken, int ExpiresIn);

    [HttpPost("telegram/exchange")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TelegramExchangeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> TelegramExchange([FromBody] TelegramExchangeRequest req)
    {
        var configuredKey = configuration["Auth:Telegram:ServiceKey"];
        var presentedKey = Request.Headers["X-Service-Key"].ToString();
        if (string.IsNullOrWhiteSpace(configuredKey) || presentedKey != configuredKey)
            return Unauthorized(Problem(title: "Unauthorized", statusCode: 401, detail: "Invalid service key"));

        var user = await userManager.Users.FirstOrDefaultAsync(u => u.TelegramUserId == req.TelegramUserId);
        if (user is null)
            return StatusCode(StatusCodes.Status403Forbidden,
                Problem(title: "Telegram not linked", statusCode: 403, detail: "Ask admin to link your Telegram"));

        var token = tokenService.CreateToken(user, rememberMe: false);
        var expiresIn = 3600;

        return Ok(new TelegramExchangeResponse(token, expiresIn));
    }

    [HttpGet("roles")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserRoles()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
            return NotFound();

        var names = await userManager.GetRolesAsync(user);
        var roles = names.Select(name => new RoleDto { Name = name });

        return Ok(roles);
    }

    [HttpGet("debug/effective-access")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetEffective([FromServices] IEffectiveAccessProvider eff, [FromServices] IHttpContextAccessor http)
    {
        var userId = http.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var data = await eff.GetAsync(userId, http.HttpContext!.User);
        return Ok(data);
    }
}

