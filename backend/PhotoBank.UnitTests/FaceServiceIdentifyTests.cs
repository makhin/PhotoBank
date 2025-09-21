using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using NetTopologySuite.Geometries;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.DbContext;
using PhotoBank.Repositories;
using PhotoBank.Services;
using DbFace = PhotoBank.DbContext.Models.Face;
using DbPerson = PhotoBank.DbContext.Models.Person;
using DbPersonFace = PhotoBank.DbContext.Models.PersonFace;
using DbPhoto = PhotoBank.DbContext.Models.Photo;
using DbStorage = PhotoBank.DbContext.Models.Storage;
using IdentityStatus = PhotoBank.DbContext.Models.IdentityStatus;

namespace PhotoBank.UnitTests;

[TestFixture]
public class FaceServiceIdentifyTests
{
    private PhotoBankDbContext _dbContext = null!;
    private ServiceProvider _provider = null!;
    private Repository<DbFace> _faceRepository = null!;
    private Repository<DbPerson> _personRepository = null!;
    private Mock<IRepository<DbPersonFace>> _personFaceRepository = null!;
    private Mock<IRepository<DbPhoto>> _photoRepository = null!;
    private Mock<IMinioClient> _minioClient = null!;
    private Mock<IFaceOperations> _faceOperations = null!;
    private Mock<IFaceClient> _faceClient = null!;
    private Mock<IMapper> _mapper = null!;
    private Mock<ILogger<FaceService>> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContext = TestDbFactory.CreateInMemory();
        var services = new ServiceCollection();
        services.AddSingleton(_dbContext);
        _provider = services.BuildServiceProvider();

        _faceRepository = new Repository<DbFace>(_provider);
        _personRepository = new Repository<DbPerson>(_provider);
        _personFaceRepository = new Mock<IRepository<DbPersonFace>>();
        _photoRepository = new Mock<IRepository<DbPhoto>>();
        _minioClient = new Mock<IMinioClient>();
        _faceOperations = new Mock<IFaceOperations>();
        _faceClient = new Mock<IFaceClient>();
        _faceClient.SetupGet(c => c.Face).Returns(_faceOperations.Object);
        _mapper = new Mock<IMapper>();
        _logger = new Mock<ILogger<FaceService>>();
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
        _provider.Dispose();
    }

    [Test]
    public async Task GroupIdentifyAsync_WhenServiceReturnsNoResults_MarksFaceAsNotIdentified()
    {
        var faceId = Guid.NewGuid();
        var face = await SeedFaceAsync(storageId: 9, identityStatus: IdentityStatus.ForReprocessing, s3Key: "face1");

        SetupMinioReturning(new byte[] { 1, 2, 3 });
        SetupDetection(new List<DetectedFace> { new DetectedFace { FaceId = faceId } });
        SetupIdentify(new List<IdentifyResult>());

        var service = CreateService();
        await service.GroupIdentifyAsync();

        var updatedFace = await _dbContext.Faces.SingleAsync(f => f.Id == face.Id);
        updatedFace.IdentityStatus.Should().Be(IdentityStatus.NotIdentified);
        updatedFace.IdentifiedWithConfidence.Should().Be(0);
        updatedFace.PersonId.Should().BeNull();
    }

    [Test]
    public async Task GroupIdentifyAsync_WhenResultHasNoCandidates_MarksFaceAsNotIdentified()
    {
        var faceId = Guid.NewGuid();
        var face = await SeedFaceAsync(storageId: 9, identityStatus: IdentityStatus.ForReprocessing, s3Key: "face2");

        SetupMinioReturning(new byte[] { 4, 5, 6 });
        SetupDetection(new List<DetectedFace> { new DetectedFace { FaceId = faceId } });
        SetupIdentify(new List<IdentifyResult>
        {
            new IdentifyResult(faceId: faceId, candidates: new List<IdentifyCandidate>())
        });

        var service = CreateService();
        await service.GroupIdentifyAsync();

        var updatedFace = await _dbContext.Faces.SingleAsync(f => f.Id == face.Id);
        updatedFace.IdentityStatus.Should().Be(IdentityStatus.NotIdentified);
        updatedFace.PersonId.Should().BeNull();
    }

    [Test]
    public async Task GroupIdentifyAsync_WhenCandidatesPresent_SelectsBestCandidateByConfidence()
    {
        var bestPersonId = Guid.NewGuid();
        var otherPersonId = Guid.NewGuid();

        var personWithBestScore = new DbPerson { Id = 1, Name = "Alice", ExternalGuid = bestPersonId, DateOfBirth = null };
        var otherPerson = new DbPerson { Id = 2, Name = "Bob", ExternalGuid = otherPersonId, DateOfBirth = null };
        _dbContext.Persons.AddRange(personWithBestScore, otherPerson);

        var face = await SeedFaceAsync(storageId: 9, identityStatus: IdentityStatus.ForReprocessing, s3Key: "face3", takenDate: null);

        await _dbContext.SaveChangesAsync();

        var detectionFaceId = Guid.NewGuid();
        SetupMinioReturning(new byte[] { 7, 8, 9 });
        SetupDetection(new List<DetectedFace> { new DetectedFace { FaceId = detectionFaceId } });
        SetupIdentify(new List<IdentifyResult>
        {
            new IdentifyResult(faceId: detectionFaceId, candidates: new List<IdentifyCandidate>
            {
                new IdentifyCandidate { PersonId = otherPersonId, Confidence = 0.7 },
                new IdentifyCandidate { PersonId = bestPersonId, Confidence = 0.95 }
            })
        });

        var service = CreateService();
        await service.GroupIdentifyAsync();

        var updatedFace = await _dbContext.Faces.Include(f => f.Person).SingleAsync(f => f.Id == face.Id);
        updatedFace.IdentityStatus.Should().Be(IdentityStatus.Identified);
        updatedFace.Person.Should().NotBeNull();
        updatedFace.Person!.ExternalGuid.Should().Be(bestPersonId);
        updatedFace.IdentifiedWithConfidence.Should().Be(0.95);
    }

    private FaceService CreateService()
    {
        return new FaceService(
            _faceClient.Object,
            _faceRepository,
            _personRepository,
            _personFaceRepository.Object,
            _photoRepository.Object,
            _minioClient.Object,
            _mapper.Object,
            _logger.Object);
    }

    private async Task<DbFace> SeedFaceAsync(int storageId, IdentityStatus identityStatus, string s3Key, DateTime? takenDate = null)
    {
        var photoId = 1 + _dbContext.Photos.Count();
        var storage = await _dbContext.Storages.FindAsync(storageId);
        if (storage == null)
        {
            storage = new DbStorage
            {
                Id = storageId,
                Name = $"storage-{storageId}",
                Folder = "folder"
            };
            _dbContext.Storages.Add(storage);
        }

        var photo = new DbPhoto
        {
            Id = photoId,
            Name = $"photo-{photoId}",
            StorageId = storageId,
            Storage = storage,
            TakenDate = takenDate,
            AccentColor = string.Empty,
            DominantColorBackground = string.Empty,
            DominantColorForeground = string.Empty,
            DominantColors = string.Empty,
            S3Key_Preview = string.Empty,
            S3ETag_Preview = string.Empty,
            Sha256_Preview = string.Empty,
            S3Key_Thumbnail = string.Empty,
            S3ETag_Thumbnail = string.Empty,
            Sha256_Thumbnail = string.Empty,
            ImageHash = string.Empty,
            RelativePath = string.Empty
        };

        var face = new DbFace
        {
            Id = 1 + _dbContext.Faces.Count(),
            Photo = photo,
            PhotoId = photo.Id,
            S3Key_Image = s3Key,
            IdentityStatus = identityStatus,
            Rectangle = new Point(0, 0),
            S3ETag_Image = string.Empty,
            Sha256_Image = string.Empty,
            FaceAttributes = string.Empty
        };

        _dbContext.Photos.Add(photo);
        _dbContext.Faces.Add(face);
        await _dbContext.SaveChangesAsync();

        _dbContext.Entry(photo).State = EntityState.Detached;
        _dbContext.Entry(face).State = EntityState.Detached;
        if (_dbContext.Entry(storage).State != EntityState.Detached)
        {
            _dbContext.Entry(storage).State = EntityState.Detached;
        }
        return face;
    }

    private void SetupDetection(IList<DetectedFace> faces)
    {
        _faceOperations
            .Setup(m => m.DetectWithStreamWithHttpMessagesAsync(
                It.IsAny<Stream>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<IList<FaceAttributeType?>>(),
                It.IsAny<string>(),
                It.IsAny<bool?>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, List<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<IList<DetectedFace>> { Body = faces });
    }

    private void SetupIdentify(IList<IdentifyResult> results)
    {
        _faceOperations
            .Setup(m => m.IdentifyWithHttpMessagesAsync(
                It.IsAny<IList<Guid?>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<double?>(),
                It.IsAny<Dictionary<string, List<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<IList<IdentifyResult>> { Body = results });
    }

    private void SetupMinioReturning(byte[] data)
    {
        _minioClient
            .Setup(m => m.GetObjectAsync(It.IsAny<GetObjectArgs>(), It.IsAny<CancellationToken>()))
            .Callback<GetObjectArgs, CancellationToken>((args, token) =>
            {
                var field = args.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                    .FirstOrDefault(f => typeof(Delegate).IsAssignableFrom(f.FieldType));
                var del = field?.GetValue(args) as Delegate;
                using var stream = new MemoryStream(data);
                del?.DynamicInvoke(stream, CancellationToken.None);
            })
            .ReturnsAsync((ObjectStat)Activator.CreateInstance(typeof(ObjectStat), nonPublic: true)!);
    }
}
