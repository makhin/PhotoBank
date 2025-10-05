using Microsoft.AspNetCore.Identity;
using PhotoBank.ViewModel.Dto;
using System.Collections.Generic;

namespace PhotoBank.Services.Identity;

public sealed record ProblemInfo(int StatusCode, string Title, string Detail);

public sealed record ValidationFailure(string FieldName, string ErrorMessage);

public sealed record LoginResult(bool Succeeded, LoginResponseDto? Response)
{
    public static LoginResult Success(LoginResponseDto response) => new(true, response);

    public static LoginResult Unauthorized() => new(false, null);
}

public sealed record TelegramSubscriptionsResult(
    bool Succeeded,
    IReadOnlyCollection<TelegramSubscriptionDto>? Subscriptions,
    ProblemInfo? Problem)
{
    public static TelegramSubscriptionsResult Success(IReadOnlyCollection<TelegramSubscriptionDto> subscriptions) =>
        new(true, subscriptions, null);

    public static TelegramSubscriptionsResult Failure(ProblemInfo problem) =>
        new(false, null, problem);
}

public sealed record TelegramExchangeResult(
    bool Succeeded,
    TelegramExchangeResponseDto? Response,
    ProblemInfo? Problem,
    ValidationFailure? ValidationFailure)
{
    public static TelegramExchangeResult Success(TelegramExchangeResponseDto response) =>
        new(true, response, null, null);

    public static TelegramExchangeResult Failure(ProblemInfo problem) =>
        new(false, null, problem, null);

    public static TelegramExchangeResult ValidationError(ValidationFailure validationFailure) =>
        new(false, null, null, validationFailure);
}

public sealed record UpdateUserResult(
    bool Succeeded,
    bool NotFound,
    IdentityResult? IdentityResult,
    ValidationFailure? ValidationFailure)
{
    public static UpdateUserResult Success() => new(true, false, null, null);

    public static UpdateUserResult NotFoundResult() => new(false, true, null, null);

    public static UpdateUserResult ValidationError(string fieldName, string message) =>
        new(false, false, null, new ValidationFailure(fieldName, message));

    public static UpdateUserResult IdentityFailure(IdentityResult result) =>
        new(false, false, result, null);
}

public sealed record CreateUserResult(
    bool Succeeded,
    UserDto? User,
    IdentityResult? IdentityResult,
    bool Conflict)
{
    public static CreateUserResult Success(UserDto user) => new(true, user, null, false);

    public static CreateUserResult ConflictFailure(IdentityResult result) => new(false, null, result, true);

    public static CreateUserResult BadRequestFailure(IdentityResult result) => new(false, null, result, false);
}

public sealed record IdentityOperationResult(
    bool Succeeded,
    bool NotFound,
    IdentityResult? IdentityResult)
{
    public static IdentityOperationResult Success() => new(true, false, null);

    public static IdentityOperationResult NotFoundResult() => new(false, true, null);

    public static IdentityOperationResult Failure(IdentityResult result) => new(false, false, result);
}

public sealed record ServiceKeyValidationResult(bool IsValid, ProblemInfo? Problem)
{
    public static ServiceKeyValidationResult Success() => new(true, null);

    public static ServiceKeyValidationResult Failure(ProblemInfo problem) => new(false, problem);
}
