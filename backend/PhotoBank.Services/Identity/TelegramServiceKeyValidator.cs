using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;

namespace PhotoBank.Services.Identity;

public sealed class TelegramServiceKeyValidator(IConfiguration configuration) : ITelegramServiceKeyValidator
{
    public ServiceKeyValidationResult Validate(string? presentedKey)
    {
        var configuredKey = configuration["Auth:Telegram:ServiceKey"];
        if (string.IsNullOrWhiteSpace(configuredKey) || !string.Equals(presentedKey, configuredKey, StringComparison.Ordinal))
        {
            return ServiceKeyValidationResult.Failure(
                new ProblemInfo(
                    StatusCodes.Status401Unauthorized,
                    "Unauthorized",
                    "Invalid service key"));
        }

        return ServiceKeyValidationResult.Success();
    }
}
