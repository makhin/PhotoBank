using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using NUnit.Framework;
using PhotoBank.Services.Search;
using PhotoBank.Services.Translator;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.UnitTests.Services;

[TestFixture]
public class SearchFilterNormalizerTests
{
    [Test]
    public async Task NormalizeAsync_TranslatesAndCachesRussianPhrases()
    {
        var translator = new Mock<ITranslatorService>();
        translator
            .Setup(t => t.TranslateAsync(It.IsAny<TranslateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TranslateRequest req, CancellationToken _) =>
                new TranslateResponse(req.Texts.Select(x => x + "_en").ToArray()));

        var cache = new MemoryCache(new MemoryCacheOptions());
        var referenceDataService = new Mock<ISearchReferenceDataService>();
        referenceDataService
            .Setup(s => s.GetPersonsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PersonDto>());
        referenceDataService
            .Setup(s => s.GetTagsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TagDto>());

        var normalizer = new SearchFilterNormalizer(translator.Object, cache, referenceDataService.Object);

        var normalized1 = await normalizer.NormalizeAsync(new FilterDto { Caption = "Привет" });
        normalized1.Caption.Should().Be("Привет_en");
        translator.Verify(t => t.TranslateAsync(It.IsAny<TranslateRequest>(), It.IsAny<CancellationToken>()), Times.Once);

        var normalized2 = await normalizer.NormalizeAsync(new FilterDto { Caption = "Привет" });
        normalized2.Caption.Should().Be("Привет_en");
        translator.Verify(t => t.TranslateAsync(It.IsAny<TranslateRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task NormalizeAsync_ResolvesNamesFromDictionaries()
    {
        var translator = new Mock<ITranslatorService>();
        translator
            .Setup(t => t.TranslateAsync(It.IsAny<TranslateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TranslateRequest req, CancellationToken _) =>
                new TranslateResponse(req.Texts.Select(x => x == "машина" ? "car" : x).ToArray()));

        var cache = new MemoryCache(new MemoryCacheOptions());

        var persons = new[] { new PersonDto { Id = 1, Name = "John" } };
        var tags = new[] { new TagDto { Id = 10, Name = "car" } };

        var referenceDataService = new Mock<ISearchReferenceDataService>();
        referenceDataService
            .Setup(s => s.GetPersonsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(persons);
        referenceDataService
            .Setup(s => s.GetTagsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tags);

        var normalizer = new SearchFilterNormalizer(translator.Object, cache, referenceDataService.Object);

        var filter = new FilterDto
        {
            PersonNames = new[] { "Jon" },
            TagNames = new[] { "машина" }
        };

        var normalized = await normalizer.NormalizeAsync(filter);

        normalized.Persons.Should().ContainSingle().Which.Should().Be(1);
        normalized.Tags.Should().ContainSingle().Which.Should().Be(10);
        normalized.TagNames![0].Should().Be("car");
    }

}
