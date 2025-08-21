using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.FaceRecognition;
using PhotoBank.Services.FaceRecognition.Abstractions;

namespace PhotoBank.UnitTests.FaceRecognition;

[TestFixture]
public class UnifiedFaceServiceTests
{
    private Mock<IFaceProvider> _provider = null!;
    private Mock<IRepository<Person>> _persons = null!;
    private Mock<IRepository<Face>> _faces = null!;
    private Mock<IRepository<PersonGroupFace>> _links = null!;
    private UnifiedFaceService _service = null!;

    [SetUp]
    public void Setup()
    {
        _provider = new Mock<IFaceProvider>();
        _provider.SetupGet(p => p.Kind).Returns(FaceProviderKind.Local);

        _persons = new Mock<IRepository<Person>>();
        _faces = new Mock<IRepository<Face>>();
        _links = new Mock<IRepository<PersonGroupFace>>();
        var logger = Mock.Of<ILogger<UnifiedFaceService>>();

        _service = new UnifiedFaceService(_provider.Object, _persons.Object, _faces.Object, _links.Object, logger);
    }

    [Test]
    public async Task SyncPersonsAsync_UpsertsAndUpdatesRepository()
    {
        var people = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Provider = null },
            new() { Id = 2, Name = "Bob", Provider = "Local" },
            new() { Id = 3, Name = "Charlie", Provider = "Azure" }
        }.AsQueryable();
        _persons.Setup(r => r.GetAll()).Returns(people);
        _persons.Setup(r => r.UpdateAsync(It.IsAny<Person>(), It.IsAny<System.Linq.Expressions.Expression<System.Func<Person, object>>[]>())).ReturnsAsync(1);

        _provider.Setup(p => p.UpsertPersonsAsync(It.IsAny<IReadOnlyCollection<PersonSyncItem>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, string> { { 1, "ext1" }, { 2, "ext2" } });

        await _service.SyncPersonsAsync();

        _provider.Verify(p => p.UpsertPersonsAsync(It.Is<IReadOnlyCollection<PersonSyncItem>>(c => c.Count == 2 && c.Any(x => x.PersonId == 1) && c.Any(x => x.PersonId == 2)), It.IsAny<CancellationToken>()), Times.Once);
        _persons.Verify(r => r.UpdateAsync(It.Is<Person>(p => p.Id == 1 && p.ExternalId == "ext1" && p.Provider == "Local"), It.IsAny<System.Linq.Expressions.Expression<System.Func<Person, object>>[]>()), Times.Once);
        _persons.Verify(r => r.UpdateAsync(It.Is<Person>(p => p.Id == 2 && p.ExternalId == "ext2" && p.Provider == "Local"), It.IsAny<System.Linq.Expressions.Expression<System.Func<Person, object>>[]>()), Times.Once);
        _persons.Verify(r => r.UpdateAsync(It.Is<Person>(p => p.Id == 3), It.IsAny<System.Linq.Expressions.Expression<System.Func<Person, object>>[]>()), Times.Never);
    }

    [Test]
    public async Task SyncFacesToPersonsAsync_LinksMissingFaces()
    {
        var linkData = new List<PersonGroupFace>
        {
            new() { PersonId = 1, FaceId = 101, ExternalId = null },
            new() { PersonId = 1, FaceId = 102, ExternalId = "have" },
            new() { PersonId = 2, FaceId = 201, ExternalId = null }
        }.AsQueryable();
        _links.Setup(r => r.GetAll()).Returns(linkData);
        _links.Setup(r => r.UpdateAsync(It.IsAny<PersonGroupFace>(), It.IsAny<System.Linq.Expressions.Expression<System.Func<PersonGroupFace, object>>[]>())).ReturnsAsync(1);

        var faceData = new List<Face>
        {
            new() { Id = 101, Image = new byte[] { 1 } },
            new() { Id = 201, Image = new byte[] { 2 } }
        }.AsQueryable();
        _faces.Setup(r => r.GetAll()).Returns(faceData);

        _provider.Setup(p => p.LinkFacesToPersonAsync(1, It.IsAny<IReadOnlyCollection<FaceToLink>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, string> { { 101, "extA" } });
        _provider.Setup(p => p.LinkFacesToPersonAsync(2, It.IsAny<IReadOnlyCollection<FaceToLink>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, string> { { 201, "extB" } });

        await _service.SyncFacesToPersonsAsync();

        _provider.Verify(p => p.LinkFacesToPersonAsync(1, It.Is<IReadOnlyCollection<FaceToLink>>(l => l.Single().FaceId == 101), It.IsAny<CancellationToken>()), Times.Once);
        _provider.Verify(p => p.LinkFacesToPersonAsync(2, It.Is<IReadOnlyCollection<FaceToLink>>(l => l.Single().FaceId == 201), It.IsAny<CancellationToken>()), Times.Once);
        _links.Verify(r => r.UpdateAsync(It.Is<PersonGroupFace>(x => x.PersonId == 1 && x.FaceId == 101 && x.ExternalId == "extA" && x.Provider == "Local"), It.IsAny<System.Linq.Expressions.Expression<System.Func<PersonGroupFace, object>>[]>()), Times.Once);
        _links.Verify(r => r.UpdateAsync(It.Is<PersonGroupFace>(x => x.PersonId == 2 && x.FaceId == 201 && x.ExternalId == "extB" && x.Provider == "Local"), It.IsAny<System.Linq.Expressions.Expression<System.Func<PersonGroupFace, object>>[]>()), Times.Once);
    }

    [Test]
    public async Task DetectFacesAsync_ForwardsToProvider()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var expected = new List<DetectedFaceDto> { new("id", 0.9f, 20, "M") };
        _provider.Setup(p => p.DetectAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var res = await _service.DetectFacesAsync(bytes);

        res.Should().BeEquivalentTo(expected);
        _provider.Verify(p => p.DetectAsync(It.Is<Stream>(s => s is MemoryStream), It.IsAny<CancellationToken>()), Times.Once);
    }
}
