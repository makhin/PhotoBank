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
        var normalizer = new SearchFilterNormalizer(translator.Object, cache);

        var normalized1 = await normalizer.NormalizeAsync(new FilterDto { Caption = "Привет" });
        normalized1.Caption.Should().Be("Привет_en");
        translator.Verify(t => t.TranslateAsync(It.IsAny<TranslateRequest>(), It.IsAny<CancellationToken>()), Times.Once);

        var normalized2 = await normalizer.NormalizeAsync(new FilterDto { Caption = "Привет" });
        normalized2.Caption.Should().Be("Привет_en");
        translator.Verify(t => t.TranslateAsync(It.IsAny<TranslateRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
