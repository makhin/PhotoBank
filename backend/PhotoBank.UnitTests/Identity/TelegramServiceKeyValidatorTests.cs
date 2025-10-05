using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using PhotoBank.Services.Identity;
using System.Collections.Generic;

namespace PhotoBank.UnitTests.Identity;

[TestFixture]
public class TelegramServiceKeyValidatorTests
{
    [Test]
    public void Validate_InvalidKey_ReturnsProblem()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:Telegram:ServiceKey"] = "expected"
            })
            .Build();

        var validator = new TelegramServiceKeyValidator(configuration);

        var result = validator.Validate("wrong");

        result.IsValid.Should().BeFalse();
        result.Problem.Should().NotBeNull();
        result.Problem!.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Test]
    public void Validate_CorrectKey_IsValid()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:Telegram:ServiceKey"] = "expected"
            })
            .Build();

        var validator = new TelegramServiceKeyValidator(configuration);

        var result = validator.Validate("expected");

        result.IsValid.Should().BeTrue();
        result.Problem.Should().BeNull();
    }
}
