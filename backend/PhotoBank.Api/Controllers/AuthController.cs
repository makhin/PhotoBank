using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhotoBank.AccessControl;
using PhotoBank.Services.Identity;
using PhotoBank.ViewModel.Dto;
using System.Collections.Generic;
using System.Security.Claims;

namespace PhotoBank.Api.Controllers;

[Route("[controller]")]
[ApiController]
public class AuthController(
    IAuthService authService,
    IUserProfileService userProfileService)
    : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await authService.LoginAsync(request);
        if (!result.Succeeded || result.Response is null)
        {
            return Unauthorized();
        }

        return Ok(result.Response);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var result = await authService.RegisterAsync(request);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok();
    }

    [HttpGet("user")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUser()
    {
        var user = await userProfileService.GetCurrentUserAsync(User);
        if (user is null)
            return NotFound();

        return Ok(user);
    }

    [HttpPut("user")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUser([FromBody] UpdateUserDto dto)
    {
        var result = await userProfileService.UpdateCurrentUserAsync(User, dto);
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

    public record TelegramExchangeRequest(string TelegramUserId, string? Username, string? LanguageCode);

    [HttpGet("telegram/subscriptions")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<TelegramSubscriptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTelegramSubscriptions()
    {
        var result = await authService.GetTelegramSubscriptionsAsync(Request.Headers["X-Service-Key"].ToString());
        if (!result.Succeeded)
        {
            return Problem(result.Problem!.Detail, statusCode: result.Problem.StatusCode, title: result.Problem.Title);
        }

        return Ok(result.Subscriptions);
    }

    [HttpPost("telegram/exchange")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TelegramExchangeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> TelegramExchange([FromBody] TelegramExchangeRequest req)
    {
        var result = await authService.ExchangeTelegramAsync(
            req.TelegramUserId,
            req.Username,
            req.LanguageCode,
            Request.Headers["X-Service-Key"].ToString());

        if (result.Problem is not null)
        {
            return Problem(result.Problem.Detail, statusCode: result.Problem.StatusCode, title: result.Problem.Title);
        }

        if (result.ValidationFailure is not null)
        {
            ModelState.AddModelError(result.ValidationFailure.FieldName, result.ValidationFailure.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        return Ok(result.Response);
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

