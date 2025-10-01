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
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace PhotoBank.Api.Controllers;

[Route("[controller]")]
[ApiController]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenService tokenService,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        // 1) Находим пользователя
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null)
            return Unauthorized();

        // 2) Проверяем пароль, НО НЕ ВЫЗЫВАЕМ PasswordSignInAsync (он пытается ставить cookie)
        var pwd = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!pwd.Succeeded)
            return Unauthorized();

        var roles = await userManager.GetRolesAsync(user);
        var roleClaims = roles.Select(r => new Claim(ClaimTypes.Role, r));
        var claims = await userManager.GetClaimsAsync(user);
        claims = claims.Concat(roleClaims).ToList();

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

        var roles = await userManager.GetRolesAsync(user);

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            TelegramUserId = user.TelegramUserId,
            TelegramSendTimeUtc = user.TelegramSendTimeUtc,
            Roles = roles.ToArray()
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
        if (dto.TelegramUserId.HasValue)
        {
            user.TelegramUserId = dto.TelegramUserId;
        }
        user.TelegramSendTimeUtc = dto.TelegramSendTimeUtc ?? user.TelegramSendTimeUtc;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok();
    }

    public record TelegramExchangeRequest(long TelegramUserId, string? Username);
    public record TelegramExchangeResponse(string AccessToken, int ExpiresIn);

    [HttpGet("telegram/subscriptions")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<TelegramSubscriptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTelegramSubscriptions()
    {
        var unauthorized = ValidateServiceKey();
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        var subscriptions = await userManager.Users
            .AsNoTracking()
            .Where(u => u.TelegramUserId != null && u.TelegramSendTimeUtc != null)
            .OrderBy(u => u.TelegramSendTimeUtc)
            .Select(u => new TelegramSubscriptionDto
            {
                TelegramUserId = u.TelegramUserId!.Value,
                TelegramSendTimeUtc = u.TelegramSendTimeUtc!.Value
            })
            .ToListAsync();

        return Ok(subscriptions);
    }

    [HttpPost("telegram/exchange")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TelegramExchangeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> TelegramExchange([FromBody] TelegramExchangeRequest req)
    {
        var unauthorized = ValidateServiceKey();
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        var user = await userManager.Users.FirstOrDefaultAsync(u => u.TelegramUserId == req.TelegramUserId);
        if (user is null)
        {
            var forbidden = Problem(
                title: "Telegram not linked",
                statusCode: StatusCodes.Status403Forbidden,
                detail: "Ask admin to link your Telegram");

            if (forbidden is ObjectResult forbiddenObject)
            {
                forbiddenObject.ContentTypes.Add("application/problem+json");
            }

            return forbidden;
        }

        var roles = await userManager.GetRolesAsync(user);
        var roleClaims = roles.Select(role => new Claim(ClaimTypes.Role, role));
        var existingClaims = await userManager.GetClaimsAsync(user);
        var allClaims = existingClaims.Concat(roleClaims).ToList();

        var token = tokenService.CreateToken(user, rememberMe: false, allClaims);
        var expiresIn = 3600;

        return Ok(new TelegramExchangeResponse(token, expiresIn));
    }

    private ActionResult? ValidateServiceKey()
    {
        var configuredKey = configuration["Auth:Telegram:ServiceKey"];
        var presentedKey = Request.Headers["X-Service-Key"].ToString();
        if (string.IsNullOrWhiteSpace(configuredKey) || presentedKey != configuredKey)
        {
            var unauthorized = Problem(
                title: "Unauthorized",
                statusCode: StatusCodes.Status401Unauthorized,
                detail: "Invalid service key");

            if (unauthorized is ObjectResult unauthorizedObject)
            {
                unauthorizedObject.ContentTypes.Add("application/problem+json");
            }

            return unauthorized;
        }

        return null;
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

