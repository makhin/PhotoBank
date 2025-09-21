using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services;
using PhotoBank.UnitTests.Infrastructure.FaceRecognition.Aws;
using DbFace = PhotoBank.DbContext.Models.Face;
using DbPerson = PhotoBank.DbContext.Models.Person;
using DbPersonFace = PhotoBank.DbContext.Models.PersonFace;

namespace PhotoBank.UnitTests;

[TestFixture]
public class FaceServiceAwsTests
{
    private const string PersonGroupId = "my-cicrle-person-group";

    private Mock<AmazonRekognitionClient> _rekognitionClient = null!;
    private Mock<IRepository<DbFace>> _faceRepository = null!;
    private Mock<IRepository<DbPerson>> _personRepository = null!;
    private Mock<IRepository<DbPersonFace>> _personFaceRepository = null!;
    private Mock<IFaceStorageService> _storage = null!;
    private Mock<ILogger<FaceService>> _logger = null!;
    private FaceServiceAws _service = null!;

    [SetUp]
    public void SetUp()
    {
        _rekognitionClient = RekognitionClientMockFactory.Create();

        _faceRepository = new Mock<IRepository<DbFace>>();
        _personRepository = new Mock<IRepository<DbPerson>>();
        _personFaceRepository = new Mock<IRepository<DbPersonFace>>();
        _storage = new Mock<IFaceStorageService>();
        _logger = new Mock<ILogger<FaceService>>();

        _service = new FaceServiceAws(
            _rekognitionClient.Object,
            _faceRepository.Object,
            _personRepository.Object,
            _personFaceRepository.Object,
            _storage.Object,
            _logger.Object);
    }

    [Test]
    public async Task SyncPersonsAsync_ShouldCreateMissingCollectionAndSyncUsers()
    {
        var dbPersons = new List<DbPerson>
        {
            new() { Id = 1 },
            new() { Id = 2 }
        };

        _personRepository
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<DbPerson>(dbPersons));

        _rekognitionClient
            .Setup(c => c.ListCollectionsAsync(
                It.Is<ListCollectionsRequest>(r => r.MaxResults == 1000),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RekognitionResponseBuilder.Collections("other-collection"))
            .Verifiable();

        _rekognitionClient
            .Setup(c => c.CreateCollectionAsync(
                It.Is<CreateCollectionRequest>(r => r.CollectionId == PersonGroupId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RekognitionResponseBuilder.CollectionCreated())
            .Verifiable();

        _rekognitionClient
            .Setup(c => c.ListUsersAsync(
                It.Is<ListUsersRequest>(r => r.CollectionId == PersonGroupId && r.MaxResults == 500),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RekognitionResponseBuilder.Users("2", "3"));

        _rekognitionClient
            .Setup(c => c.CreateUserAsync(
                It.Is<CreateUserRequest>(r => r.CollectionId == PersonGroupId && r.UserId == "1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RekognitionResponseBuilder.UserCreated())
            .Verifiable();

        _rekognitionClient
            .Setup(c => c.DeleteUserAsync(
                It.Is<DeleteUserRequest>(r => r.CollectionId == PersonGroupId && r.UserId == "3"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RekognitionResponseBuilder.UserDeleted())
            .Verifiable();

        await _service.SyncPersonsAsync();

        _rekognitionClient.VerifyAll();
        _rekognitionClient.Verify(c => c.ListUsersAsync(
            It.Is<ListUsersRequest>(r => r.CollectionId == PersonGroupId && r.MaxResults == 500),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SyncFacesToPersonAsync_ShouldIndexAndAssociateNewFaces()
    {
        var personFace = new DbPersonFace
        {
            PersonId = 10,
            FaceId = 5,
            ExternalGuid = Guid.Empty
        };

        var dbPersonFaces = new List<DbPersonFace> { personFace };

        _personFaceRepository
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<DbPersonFace>(dbPersonFaces));

        _faceRepository
            .Setup(r => r.GetAsync(personFace.FaceId))
            .ReturnsAsync(new DbFace { Id = personFace.FaceId, S3Key_Image = "key" });

        _storage
            .Setup(s => s.OpenReadStreamAsync(It.IsAny<DbFace>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(new byte[] { 1, 2, 3 }));

        _rekognitionClient
            .Setup(c => c.ListFacesAsync(
                It.Is<ListFacesRequest>(r => r.CollectionId == PersonGroupId && r.UserId == personFace.PersonId.ToString()),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RekognitionResponseBuilder.Faces());

        var indexedFaceId = Guid.NewGuid().ToString();

        _rekognitionClient
            .Setup(c => c.IndexFacesAsync(
                It.Is<IndexFacesRequest>(r =>
                    r.CollectionId == PersonGroupId &&
                    r.DetectionAttributes.Contains("ALL") &&
                    r.MaxFaces == 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RekognitionResponseBuilder.IndexedFaces(indexedFaceId));

        _rekognitionClient
            .Setup(c => c.AssociateFacesAsync(
                It.Is<AssociateFacesRequest>(r =>
                    r.CollectionId == PersonGroupId &&
                    r.UserId == personFace.PersonId.ToString() &&
                    r.FaceIds.Single() == indexedFaceId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RekognitionResponseBuilder.AssociatedFaces(indexedFaceId));

        _personFaceRepository
            .Setup(r => r.UpdateAsync(
                It.Is<DbPersonFace>(pf => pf.ExternalGuid == Guid.Parse(indexedFaceId)),
                It.IsAny<Expression<Func<DbPersonFace, object>>[]>()))
            .ReturnsAsync(1)
            .Verifiable();

        await _service.SyncFacesToPersonAsync();

        _rekognitionClient.Verify(c => c.IndexFacesAsync(
            It.IsAny<IndexFacesRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _rekognitionClient.Verify(c => c.AssociateFacesAsync(
            It.IsAny<AssociateFacesRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _personFaceRepository.Verify();
        dbPersonFaces.Single().ExternalGuid.Should().Be(Guid.Parse(indexedFaceId));
    }

    [Test]
    public async Task SyncFacesToPersonAsync_WhenFaceAlreadyIndexed_ShouldSkipIndexing()
    {
        var existingGuid = Guid.NewGuid();
        var personFace = new DbPersonFace
        {
            PersonId = 42,
            FaceId = 7,
            ExternalGuid = existingGuid
        };

        _personFaceRepository
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<DbPersonFace>(new[] { personFace }));

        _rekognitionClient
            .Setup(c => c.ListFacesAsync(
                It.Is<ListFacesRequest>(r => r.CollectionId == PersonGroupId && r.UserId == personFace.PersonId.ToString()),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RekognitionResponseBuilder.Faces(existingGuid.ToString()));

        await _service.SyncFacesToPersonAsync();

        _rekognitionClient.Verify(c => c.IndexFacesAsync(
            It.IsAny<IndexFacesRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);

        _personFaceRepository.Verify(r => r.UpdateAsync(
            It.IsAny<DbPersonFace>(),
            It.IsAny<Expression<Func<DbPersonFace, object>>[]>()), Times.Never);
    }

    [Test]
    public async Task DetectFacesAsync_WhenNoFacesFound_ShouldReturnEmptyListAndUseAllAttributes()
    {
        DetectFacesRequest? capturedRequest = null;

        _rekognitionClient
            .Setup(c => c.DetectFacesAsync(
                It.IsAny<DetectFacesRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback((DetectFacesRequest request, CancellationToken _) => capturedRequest = request)
            .ReturnsAsync(new DetectFacesResponse
            {
                FaceDetails = new List<FaceDetail>()
            });

        var result = await _service.DetectFacesAsync(new byte[] { 1, 2, 3 });

        result.Should().NotBeNull().And.BeEmpty();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Attributes.Should().Contain("ALL");
        capturedRequest.Image.Bytes.Should().BeOfType<MemoryStream>();
    }

    [Test]
    public async Task DetectFacesAsync_WhenFacesFound_ShouldReturnDetails()
    {
        var expectedFaces = new List<FaceDetail> { new() };

        _rekognitionClient
            .Setup(c => c.DetectFacesAsync(
                It.IsAny<DetectFacesRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RekognitionResponseBuilder.DetectedFaces((IList<FaceDetail>)expectedFaces));

        var result = await _service.DetectFacesAsync(new byte[] { 4, 5 });

        result.Should().BeEquivalentTo(expectedFaces);
    }

    [Test]
    public async Task SearchUsersByImageAsync_WhenMatchesFound_ShouldReturnMatchesAndUseCollection()
    {
        SearchUsersByImageRequest? capturedRequest = null;
        var matches = new List<UserMatch> { new() };

        _rekognitionClient
            .Setup(c => c.SearchUsersByImageAsync(
                It.IsAny<SearchUsersByImageRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback((SearchUsersByImageRequest request, CancellationToken _) => capturedRequest = request)
            .ReturnsAsync(RekognitionResponseBuilder.UserMatches(matches));

        var result = await _service.SearchUsersByImageAsync(new byte[] { 9, 9 });

        result.Should().BeSameAs(matches);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.CollectionId.Should().Be(PersonGroupId);
        capturedRequest.Image.Bytes.Should().BeOfType<MemoryStream>();
    }

    [Test]
    public async Task SearchUsersByImageAsync_WhenNoMatchesFound_ShouldReturnEmptyList()
    {
        _rekognitionClient
            .Setup(c => c.SearchUsersByImageAsync(
                It.IsAny<SearchUsersByImageRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RekognitionResponseBuilder.UserMatches());

        var result = await _service.SearchUsersByImageAsync(new byte[] { 2, 1 });

        result.Should().NotBeNull().And.BeEmpty();
    }

    private sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable)
            : base(enumerable)
        {
        }

        public TestAsyncEnumerable(Expression expression)
            : base(expression)
        {
        }

        IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken)
            => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    private sealed class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public T Current => _inner.Current;

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync()
            => new(_inner.MoveNext());
    }

    private sealed class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        public TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
            => new TestAsyncEnumerable<TEntity>(expression);

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            => new TestAsyncEnumerable<TElement>(expression);

        public object Execute(Expression expression)
            => _inner.Execute(expression);

        public TResult Execute<TResult>(Expression expression)
            => _inner.Execute<TResult>(expression);

        public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
            => new TestAsyncEnumerable<TResult>(expression);

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
            => Execute<TResult>(expression);
    }
}
