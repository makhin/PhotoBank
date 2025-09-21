using FluentAssertions;
using NUnit.Framework;
using PhotoBank.Api;

namespace PhotoBank.UnitTests;

[TestFixture]
public class DomainExceptionTests
{
    [Test]
    public void Constructor_ShouldPreserveProvidedMessage()
    {
        // Arrange
        const string message = "Custom domain error";

        // Act
        var exception = new DomainException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Test]
    public void Constructor_ShouldNotSetInnerException()
    {
        // Arrange & Act
        var exception = new DomainException("message");

        // Assert
        exception.InnerException.Should().BeNull();
    }
}
