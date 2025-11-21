using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using PhotoBank.Services.ImageAnalysis;
using PhotoBank.UnitTests.Infrastructure.Logging;

namespace PhotoBank.UnitTests.ImageAnalysis;

[TestFixture]
public class OllamaImageAnalyzerTests
{
    private OllamaImageAnalyzer _analyzer;
    private TestLogger<OllamaImageAnalyzer> _logger;

    [SetUp]
    public void Setup()
    {
        _logger = new TestLogger<OllamaImageAnalyzer>();
        var options = Options.Create(new OllamaOptions
        {
            Endpoint = "http://localhost:11434",
            Model = "test-model"
        });
        _analyzer = new OllamaImageAnalyzer(options, _logger);
    }

    [Test]
    public void Kind_ShouldReturnOllama()
    {
        _analyzer.Kind.Should().Be(ImageAnalyzerKind.Ollama);
    }

    [Test]
    public void ParseResponse_ValidJson_ReturnsCorrectResult()
    {
        // Arrange
        var json = """
            {
                "caption": "A beautiful sunset over the ocean",
                "tags": [
                    {"name": "sunset", "confidence": 0.95},
                    {"name": "ocean", "confidence": 0.88},
                    {"name": "sky", "confidence": 0.82}
                ],
                "is_nsfw": false,
                "is_racy": false,
                "dominant_colors": ["orange", "blue"]
            }
            """;

        // Act
        var result = _analyzer.ParseResponse(json);

        // Assert
        result.Should().NotBeNull();
        result.Description.Should().NotBeNull();
        result.Description!.Captions.Should().ContainSingle()
            .Which.Text.Should().Be("A beautiful sunset over the ocean");

        result.Tags.Should().HaveCount(3);
        result.Tags[0].Name.Should().Be("sunset");
        result.Tags[0].Confidence.Should().BeApproximately(0.95, 0.001);

        result.Adult.Should().NotBeNull();
        result.Adult!.IsAdultContent.Should().BeFalse();
        result.Adult.IsRacyContent.Should().BeFalse();

        result.Color.Should().NotBeNull();
        result.Color!.DominantColors.Should().HaveCount(2);
        result.Color.AccentColor.Should().Be("Orange");
    }

    [Test]
    public void ParseResponse_NsfwContent_SetsAdultFlags()
    {
        // Arrange
        var json = """
            {
                "caption": "Test image",
                "tags": [],
                "is_nsfw": true,
                "is_racy": true,
                "dominant_colors": []
            }
            """;

        // Act
        var result = _analyzer.ParseResponse(json);

        // Assert
        result.Adult.Should().NotBeNull();
        result.Adult!.IsAdultContent.Should().BeTrue();
        result.Adult.AdultScore.Should().BeApproximately(0.9, 0.001);
        result.Adult.IsRacyContent.Should().BeTrue();
        result.Adult.RacyScore.Should().BeApproximately(0.9, 0.001);
    }

    [Test]
    public void ParseResponse_JsonWithMarkdownCodeBlock_ParsesCorrectly()
    {
        // Arrange
        var json = """
            ```json
            {
                "caption": "Test caption",
                "tags": [{"name": "test", "confidence": 0.9}],
                "is_nsfw": false,
                "is_racy": false,
                "dominant_colors": ["red"]
            }
            ```
            """;

        // Act
        var result = _analyzer.ParseResponse(json);

        // Assert
        result.Description.Should().NotBeNull();
        result.Description!.Captions.Should().ContainSingle()
            .Which.Text.Should().Be("Test caption");
    }

    [Test]
    public void ParseResponse_EmptyTags_ReturnsEmptyTagList()
    {
        // Arrange
        var json = """
            {
                "caption": "No tags image",
                "tags": [],
                "is_nsfw": false,
                "is_racy": false,
                "dominant_colors": []
            }
            """;

        // Act
        var result = _analyzer.ParseResponse(json);

        // Assert
        result.Tags.Should().BeEmpty();
    }

    [Test]
    public void ParseResponse_NullCaption_ReturnsNullDescription()
    {
        // Arrange
        var json = """
            {
                "caption": null,
                "tags": [],
                "is_nsfw": false,
                "is_racy": false,
                "dominant_colors": []
            }
            """;

        // Act
        var result = _analyzer.ParseResponse(json);

        // Assert
        result.Description.Should().BeNull();
    }

    [Test]
    public void ParseResponse_InvalidJson_ThrowsToFailClosed()
    {
        // Arrange
        var json = "not valid json at all";

        // Act & Assert - fail closed to prevent NSFW bypass
        var act = () => _analyzer.ParseResponse(json);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*malformed response*");

        _logger.Entries.Should().ContainSingle()
            .Which.Level.Should().Be(LogLevel.Error);
    }

    [Test]
    public void ParseResponse_MissingFields_UsesDefaults()
    {
        // Arrange
        var json = "{}";

        // Act
        var result = _analyzer.ParseResponse(json);

        // Assert
        result.Should().NotBeNull();
        result.Description.Should().BeNull();
        result.Tags.Should().BeEmpty();
        result.Adult.Should().NotBeNull();
        result.Adult!.IsAdultContent.Should().BeFalse();
        result.Color.Should().NotBeNull();
    }

    [Test]
    public void ParseResponse_TagWithNullName_SkipsTag()
    {
        // Arrange
        var json = """
            {
                "caption": "Test",
                "tags": [
                    {"name": null, "confidence": 0.9},
                    {"name": "valid", "confidence": 0.8},
                    {"name": "", "confidence": 0.7}
                ],
                "is_nsfw": false,
                "is_racy": false,
                "dominant_colors": []
            }
            """;

        // Act
        var result = _analyzer.ParseResponse(json);

        // Assert
        result.Tags.Should().ContainSingle()
            .Which.Name.Should().Be("valid");
    }

    [Test]
    public void ParseResponse_SingleDominantColor_UsesSameForForegroundAndBackground()
    {
        // Arrange
        var json = """
            {
                "caption": "Test",
                "tags": [],
                "is_nsfw": false,
                "is_racy": false,
                "dominant_colors": ["purple"]
            }
            """;

        // Act
        var result = _analyzer.ParseResponse(json);

        // Assert
        result.Color.Should().NotBeNull();
        result.Color!.AccentColor.Should().Be("Purple");
        result.Color.DominantColorBackground.Should().Be("Purple");
        result.Color.DominantColorForeground.Should().Be("Purple");
    }

    [Test]
    public void ParseResponse_TwoDominantColors_SetsForegroundAndBackground()
    {
        // Arrange
        var json = """
            {
                "caption": "Test",
                "tags": [],
                "is_nsfw": false,
                "is_racy": false,
                "dominant_colors": ["white", "black"]
            }
            """;

        // Act
        var result = _analyzer.ParseResponse(json);

        // Assert
        result.Color.Should().NotBeNull();
        result.Color!.AccentColor.Should().Be("White");
        result.Color.DominantColorBackground.Should().Be("White");
        result.Color.DominantColorForeground.Should().Be("Black");
    }

    [Test]
    public void ParseResponse_DominantColors_CapitalizesFirstLetter()
    {
        // Arrange
        var json = """
            {
                "caption": "Test",
                "tags": [],
                "is_nsfw": false,
                "is_racy": false,
                "dominant_colors": ["BLUE", "grey", "Green"]
            }
            """;

        // Act
        var result = _analyzer.ParseResponse(json);

        // Assert
        result.Color.Should().NotBeNull();
        result.Color!.DominantColors.Should().BeEquivalentTo(["Blue", "Grey", "Green"]);
    }

    [Test]
    public void ParseResponse_ConfidenceAsString_ParsesCorrectly()
    {
        // Arrange - some models might return confidence as string
        var json = """
            {
                "caption": "Test",
                "tags": [{"name": "test", "confidence": "0.85"}],
                "is_nsfw": false,
                "is_racy": false,
                "dominant_colors": []
            }
            """;

        // Act
        var result = _analyzer.ParseResponse(json);

        // Assert
        result.Tags.Should().ContainSingle()
            .Which.Confidence.Should().BeApproximately(0.85, 0.001);
    }

    [Test]
    public void ParseResponse_CategoriesAndObjects_AlwaysEmpty()
    {
        // Arrange
        var json = """
            {
                "caption": "Test",
                "tags": [],
                "is_nsfw": false,
                "is_racy": false,
                "dominant_colors": []
            }
            """;

        // Act
        var result = _analyzer.ParseResponse(json);

        // Assert
        result.Categories.Should().BeEmpty();
        result.Objects.Should().BeEmpty();
    }
}
