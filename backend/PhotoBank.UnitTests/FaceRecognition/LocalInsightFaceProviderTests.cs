using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using PhotoBank.Services.FaceRecognition;
using PhotoBank.Services.FaceRecognition.Abstractions;
using PhotoBank.Services.FaceRecognition.Local;

namespace PhotoBank.UnitTests.FaceRecognition;

[TestFixture]
public class LocalInsightFaceProviderTests
{
    private Mock<ILocalInsightFaceClient> _client = null!;
    private Mock<IFaceEmbeddingRepository> _repo = null!;
    private LocalInsightFaceProvider _provider = null!;

    [SetUp]
    public void Setup()
    {
        _client = new Mock<ILocalInsightFaceClient>();
        _repo = new Mock<IFaceEmbeddingRepository>();
        var opts = Options.Create(new LocalInsightFaceOptions { FaceMatchThreshold = 0.5f, TopK = 2, MaxParallelism = 2 });
        var logger = Mock.Of<ILogger<LocalInsightFaceProvider>>();
        _provider = new LocalInsightFaceProvider(_client.Object, _repo.Object, opts, logger);
    }

    [Test]
    public async Task UpsertPersonsAsync_ReturnsLocalIds()
    {
        var persons = new[]
        {
            new PersonSyncItem(1, "A", null),
            new PersonSyncItem(2, "B", null)
        };

        var res = await _provider.UpsertPersonsAsync(persons, CancellationToken.None);
        res.Should().Contain(new KeyValuePair<int,string>(1,"local:1"));
        res.Should().Contain(new KeyValuePair<int,string>(2,"local:2"));
    }

    [Test]
    public async Task LinkFacesToPersonAsync_EmbedsAndStoresVectors()
    {
        _client.SetupSequence(c => c.EmbedAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LocalEmbedResponse(new float[] { 2, 0 }, new int[] { 2 }, 2, "m", "112x112", null))
            .ReturnsAsync(new LocalEmbedResponse(new float[] { 0, 3 }, new int[] { 2 }, 2, "m", "112x112", null));

        var faces = new List<FaceToLink>
        {
            new(10, () => new MemoryStream(new byte[]{1}), null),
            new(11, () => new MemoryStream(new byte[]{2}), null)
        };

        await _provider.LinkFacesToPersonAsync(5, faces, CancellationToken.None);

        _client.Verify(c => c.EmbedAsync(It.IsAny<Stream>(), false, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _repo.Verify(r => r.UpsertAsync(5, 10, It.Is<float[]>(v => System.Math.Abs(v[0]-1f) < 1e-6 && System.Math.Abs(v[1]) < 1e-6), "m", It.IsAny<CancellationToken>()));
        _repo.Verify(r => r.UpsertAsync(5, 11, It.Is<float[]>(v => System.Math.Abs(v[1]-1f) < 1e-6 && System.Math.Abs(v[0]) < 1e-6), "m", It.IsAny<CancellationToken>()));
    }

    [Test]
    public async Task SearchUsersByImageAsync_ReturnsOrderedMatchesAboveThreshold()
    {
        // Query embedding [1, 1] will be normalized to [0.707, 0.707]
        _client.Setup(c => c.EmbedAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LocalEmbedResponse(new float[] { 1, 1 }, new int[] { 2 }, 2, null, null, null));

        // Mock FindSimilarFacesAsync with SQL-level grouping (DISTINCT ON)
        // TopK=2, so limit is exactly 2 (best face per person)
        // pgvector CosineDistance returns (1 - cosine_similarity), so similarity = 1 - distance
        // Repository returns already grouped and sorted results:
        // - PersonId=2, Distance=0.0 (perfect match) -> similarity = 1 - 0.0 = 1.0
        // - PersonId=1, Distance=0.3 -> similarity = 1 - 0.3 = 0.7
        _repo.Setup(r => r.FindSimilarFacesAsync(It.IsAny<float[]>(), 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(int PersonId, int FaceId, float Distance)>
            {
                (2, 2, 0.0f),           // Best match, similarity=1.0
                (1, 1, 0.3f)            // Good match, similarity=0.7
            });

        var res = await _provider.SearchUsersByImageAsync(new MemoryStream(new byte[]{1}), CancellationToken.None);

        res.Should().HaveCount(2);
        res[0].ProviderPersonId.Should().Be("local:2");
        res[0].Confidence.Should().BeApproximately(1.0f, 0.001f);
        res[1].ProviderPersonId.Should().Be("local:1");
        res[1].Confidence.Should().BeApproximately(0.7f, 0.001f);
    }
}
