using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using PhotoBank.DependencyInjection;

namespace PhotoBank.UnitTests;

[TestFixture]
public class FaceApiOptionsTests
{
    [Test]
    public void Bind_WithValidConfiguration_BindsValuesAndPassesValidation()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Face:Endpoint"] = "https://face.example.com",
                ["Face:Key"] = "secret-key"
            })
            .Build();

        var options = configuration.GetSection("Face").Get<FaceApiOptions>()!;

        var validation = () => Validator.ValidateObject(options, new ValidationContext(options), validateAllProperties: true);

        validation.Should().NotThrow();
        options.Endpoint.Should().Be("https://face.example.com");
        options.Key.Should().Be("secret-key");
    }

    [Test]
    public void Bind_WithMissingEndpoint_ThrowsValidationException()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Face:Key"] = "secret-key"
            })
            .Build();

        var options = configuration.GetSection("Face").Get<FaceApiOptions>()!;

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
                ["Face:Endpoint"] = "https://face.example.com",
                ["Face:Key"] = string.Empty
            })
            .Build();

        var options = configuration.GetSection("Face").Get<FaceApiOptions>()!;

        var validation = () => Validator.ValidateObject(options, new ValidationContext(options), validateAllProperties: true);

        validation.Should().Throw<ValidationException>()
            .WithMessage("*Key*");
    }
}
