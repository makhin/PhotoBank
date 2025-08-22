using FluentAssertions;
using NUnit.Framework;
using PhotoBank.Services;

namespace PhotoBank.UnitTests;

[TestFixture]
public class LanguageDetectorTests
{
    [TestCase(null, "unknown")]
    [TestCase("", "unknown")]
    [TestCase("!@#", "unknown")]
    [TestCase("hello world", "en")]
    [TestCase("привет мир", "ru")]
    [TestCase("привет world", "ru")]
    [TestCase("hello мир", "en")]
    public void DetectRuEn_ReturnsExpectedLanguage(string? text, string expected)
    {
        var result = LanguageDetector.DetectRuEn(text);

        result.Should().Be(expected);
    }
}
