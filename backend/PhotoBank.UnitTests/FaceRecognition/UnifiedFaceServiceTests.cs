using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
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
        await using var ctx = TestDbFactory.CreateInMemory();

        var services = new ServiceCollection();
        services.AddSingleton(ctx);
        var sp = services.BuildServiceProvider();

        var personsRepo = new Repository<Person>(sp);
        var facesRepo = new Repository<Face>(sp);
        var linksRepo = new Repository<PersonGroupFace>(sp);

        var provider = new Mock<IFaceProvider>(MockBehavior.Strict);
        provider.SetupGet(p => p.Kind).Returns(FaceProviderKind.Local);
        provider
            .Setup(p => p.UpsertPersonsAsync(It.IsAny<IReadOnlyCollection<PersonSyncItem>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, string> { { 1, "ext1" } });

        var service = new UnifiedFaceService(provider.Object, personsRepo, facesRepo, linksRepo, Mock.Of<ILogger<UnifiedFaceService>>());

        ctx.Persons.AddRange(
            new Person { Id = 1, Name = "Alice", ExternalId = null, Provider = null },
            new Person { Id = 2, Name = "Bob", ExternalId = "ext-bob", Provider = "Other" }
        );
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        await service.SyncPersonsAsync();

        provider.Verify(p => p.UpsertPersonsAsync(It.Is<IReadOnlyCollection<PersonSyncItem>>(c => c.Single().PersonId == 1), It.IsAny<CancellationToken>()), Times.Once);

        var all = await ctx.Persons.AsNoTracking().ToListAsync();

        Assert.That(all.Single(p => p.Id == 1).Provider, Is.EqualTo(provider.Object.Kind.ToString()));
        Assert.That(all.Single(p => p.Id == 1).ExternalId, Is.EqualTo("ext1"));
        Assert.That(all.Single(p => p.Id == 2).Provider, Is.EqualTo("Other"));
        Assert.That(all.Single(p => p.Id == 2).ExternalId, Is.EqualTo("ext-bob"));
    }

    [Test]
    public async Task SyncFacesToPersonsAsync_LinksMissingFaces()
    {
        await using var ctx = TestDbFactory.CreateInMemory();

        var services = new ServiceCollection();
        services.AddSingleton(ctx);
        var sp = services.BuildServiceProvider();

        var personsRepo = new Repository<Person>(sp);
        var facesRepo = new Repository<Face>(sp);
        var linksRepo = new Repository<PersonGroupFace>(sp);

        var provider = new Mock<IFaceProvider>(MockBehavior.Strict);
        provider.SetupGet(p => p.Kind).Returns(FaceProviderKind.Local);
        provider.Setup(p => p.LinkFacesToPersonAsync(1, It.IsAny<IReadOnlyCollection<FaceToLink>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, string> { { 101, "extA" } });

        var service = new UnifiedFaceService(provider.Object, personsRepo, facesRepo, linksRepo, Mock.Of<ILogger<UnifiedFaceService>>());

        ctx.Persons.Add(new Person { Id = 1, Name = "Alice" });

        ctx.Faces.AddRange(
            new Face { Id = 101, PhotoId = 0, Image = new byte[] { 1 }, Rectangle = new Point(0, 0), S3Key_Image = string.Empty, S3ETag_Image = string.Empty, Sha256_Image = string.Empty, FaceAttributes = string.Empty },
            new Face { Id = 102, PhotoId = 0, Image = new byte[] { 2 }, Rectangle = new Point(0, 0), S3Key_Image = string.Empty, S3ETag_Image = string.Empty, Sha256_Image = string.Empty, FaceAttributes = string.Empty }
        );

        var linkMissing = new PersonGroupFace { Id = -1, PersonId = 1, FaceId = 101, ExternalId = null, Provider = null };
        var linkExists  = new PersonGroupFace { Id = -2, PersonId = 1, FaceId = 102, ExternalId = "have", Provider = "Local" };
        ctx.Set<PersonGroupFace>().AddRange(linkMissing, linkExists);

        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        await service.SyncFacesToPersonsAsync();

        provider.Verify(p => p.LinkFacesToPersonAsync(1, It.Is<IReadOnlyCollection<FaceToLink>>(l => l.Single().FaceId == 101), It.IsAny<CancellationToken>()), Times.Once);

        var link101 = await ctx.Set<PersonGroupFace>().AsNoTracking().SingleAsync(l => l.PersonId == 1 && l.FaceId == 101);

        Assert.That(link101.ExternalId, Is.EqualTo("extA"));
        Assert.That(link101.Provider, Is.EqualTo(provider.Object.Kind.ToString()));
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
