using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using PhotoBank.DependencyInjection;

namespace PhotoBank.UnitTests;

[TestFixture]
public class ComputerVisionOptionsTests
{
    [Test]
    public void Bind_WithValidConfiguration_BindsValuesAndPassesValidation()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ComputerVision:Endpoint"] = "https://computer-vision.example.com",
                ["ComputerVision:Key"] = "super-secret-key"
            })
            .Build();

        var options = configuration.GetSection("ComputerVision").Get<ComputerVisionOptions>()!;

        var validation = () => Validator.ValidateObject(options, new ValidationContext(options), validateAllProperties: true);

        validation.Should().NotThrow();
        options.Endpoint.Should().Be("https://computer-vision.example.com");
        options.Key.Should().Be("super-secret-key");
    }

    [Test]
    public void Bind_WithMissingEndpoint_ThrowsValidationException()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ComputerVision:Key"] = "super-secret-key"
            })
            .Build();

        var options = configuration.GetSection("ComputerVision").Get<ComputerVisionOptions>()!;

        var validation = () => Validator.ValidateObject(options, new ValidationContext(options), validateAllProperties: true);

        validation.Should().Throw<ValidationException>()
            .WithMessage("*Endpoint*");
    }

    [Test]
    public void Bind_WithEmptyKey_ThrowsValidationException()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ComputerVision:Endpoint"] = "https://computer-vision.example.com",
                ["ComputerVision:Key"] = string.Empty
            })
            .Build();

        var options = configuration.GetSection("ComputerVision").Get<ComputerVisionOptions>()!;

        var validation = () => Validator.ValidateObject(options, new ValidationContext(options), validateAllProperties: true);

        validation.Should().Throw<ValidationException>()
            .WithMessage("*Key*");
    }
}
