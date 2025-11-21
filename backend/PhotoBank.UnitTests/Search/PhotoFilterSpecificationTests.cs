using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NUnit.Framework;
using PhotoBank.AccessControl;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Search;
using PhotoBank.UnitTests;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.UnitTests.Search;

[TestFixture]
public class PhotoFilterSpecificationTests
{
    private PhotoBankDbContext _context = null!;
    private PhotoFilterSpecification _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbFactory.CreateInMemory();
        _sut = new PhotoFilterSpecification(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task Build_WithFlagsAndDateFilters_ReturnsMatchingPhoto()
    {
        var storage = new Storage { Id = 1, Name = "s1", Folder = "f1" };
        var matching = CreatePhoto(1, storage, "match", new DateTime(2024, 1, 1));
        matching.IsBW = true;
        matching.IsAdultContent = true;
        matching.IsRacyContent = true;

        var other = CreatePhoto(2, storage, "other", new DateTime(2024, 2, 1));
        other.IsBW = false;
        other.IsAdultContent = false;
        other.IsRacyContent = false;

        _context.Storages.Add(storage);
        _context.Photos.AddRange(matching, other);
        await _context.SaveChangesAsync();

        var filter = new FilterDto
        {
            IsBW = true,
            IsAdultContent = true,
            IsRacyContent = true,
            TakenDateFrom = new DateTime(2024, 1, 1),
            TakenDateTo = new DateTime(2024, 1, 1)
        };

        var result = await _sut.Build(filter, new DummyCurrentUser())
            .OrderBy(p => p.Id)
            .ToListAsync();

        result.Should().ContainSingle()
            .Which.Id.Should().Be(matching.Id);
    }

    [Test]
    public async Task Build_WithStoragesAndRelativePath_FiltersByBoth()
    {
        var storage1 = new Storage { Id = 100, Name = "s100", Folder = "f100" };
        var storage2 = new Storage { Id = 200, Name = "s200", Folder = "f200" };
        var matching = CreatePhoto(101, storage1, "match");
        matching.RelativePath = "album/2024";

        var samePathDifferentStorage = CreatePhoto(102, storage2, "different-storage");
        samePathDifferentStorage.RelativePath = "album/2024";

        var otherPath = CreatePhoto(103, storage1, "other-path");
        otherPath.RelativePath = "album/2023";

        _context.Storages.AddRange(storage1, storage2);
        _context.Photos.AddRange(matching, samePathDifferentStorage, otherPath);
        await _context.SaveChangesAsync();

        var filter = new FilterDto
        {
            Storages = new[] { storage1.Id },
            RelativePath = "album/2024"
        };

        var result = await _sut.Build(filter, new DummyCurrentUser())
            .Select(p => p.Id)
            .ToListAsync();

        result.Should().Equal(matching.Id);
    }

    [Test]
    public async Task Build_WithPersonsAndTags_ReturnsPhotosContainingAll()
    {
        var storage = new Storage { Id = 300, Name = "s300", Folder = "f300" };
        var person1 = new Person { Id = 1, Name = "Alice" };
        var person2 = new Person { Id = 2, Name = "Bob" };
        var tag1 = new Tag { Id = 10, Name = "Nature", Hint = string.Empty, PhotoTags = new List<PhotoTag>() };
        var tag2 = new Tag { Id = 20, Name = "Family", Hint = string.Empty, PhotoTags = new List<PhotoTag>() };

        var matching = CreatePhoto(301, storage, "match");
        var other = CreatePhoto(302, storage, "other");

        _context.Storages.Add(storage);
        _context.Persons.AddRange(person1, person2);
        _context.Tags.AddRange(tag1, tag2);
        _context.Photos.AddRange(matching, other);
        await _context.SaveChangesAsync();

        var matchingFaces = new[]
        {
            new Face
            {
                Id = 1,
                Photo = matching,
                PhotoId = matching.Id,
                Person = person1,
                PersonId = person1.Id,
                Rectangle = new Point(0, 0),
                S3Key_Image = "k1",
                S3ETag_Image = "e1",
                Sha256_Image = "s1",
                FaceAttributes = "{}"
            },
            new Face
            {
                Id = 2,
                Photo = matching,
                PhotoId = matching.Id,
                Person = person2,
                PersonId = person2.Id,
                Rectangle = new Point(0, 0),
                S3Key_Image = "k2",
                S3ETag_Image = "e2",
                Sha256_Image = "s2",
                FaceAttributes = "{}"
            }
        };

        var otherFace = new Face
        {
            Id = 3,
            Photo = other,
            PhotoId = other.Id,
            Person = person1,
            PersonId = person1.Id,
            Rectangle = new Point(0, 0),
            S3Key_Image = "k3",
            S3ETag_Image = "e3",
            Sha256_Image = "s3",
            FaceAttributes = "{}"
        };

        _context.Faces.AddRange(matchingFaces);
        _context.Faces.Add(otherFace);

        var matchingTags = new[]
        {
            new PhotoTag { Photo = matching, PhotoId = matching.Id, Tag = tag1, TagId = tag1.Id },
            new PhotoTag { Photo = matching, PhotoId = matching.Id, Tag = tag2, TagId = tag2.Id }
        };
        var otherTag = new PhotoTag { Photo = other, PhotoId = other.Id, Tag = tag1, TagId = tag1.Id };

        _context.PhotoTags.AddRange(matchingTags);
        _context.PhotoTags.Add(otherTag);
        await _context.SaveChangesAsync();

        var filter = new FilterDto
        {
            Persons = new[] { person1.Id, person2.Id },
            Tags = new[] { tag1.Id, tag2.Id }
        };

        var result = await _sut.Build(filter, new DummyCurrentUser())
            .Select(p => p.Id)
            .ToListAsync();

        result.Should().Equal(matching.Id);
    }

    [Test]
    public async Task Build_AppliesAclForNonAdminUsers()
    {
        var storageAllowed = new Storage { Id = 400, Name = "allowed", Folder = "fa" };
        var storageDenied = new Storage { Id = 401, Name = "denied", Folder = "fd" };

        var allowedPhoto = CreatePhoto(401, storageAllowed, "allowed-photo");

        var deniedPhoto = CreatePhoto(402, storageDenied, "denied-photo");

        _context.Storages.AddRange(storageAllowed, storageDenied);
        _context.Photos.AddRange(allowedPhoto, deniedPhoto);
        await _context.SaveChangesAsync();

        var user = new TestCurrentUser(storageAllowed.Id);

        var result = await _sut.Build(new FilterDto(), user)
            .Select(p => p.Id)
            .ToListAsync();

        result.Should().Equal(allowedPhoto.Id);
    }

    [Test]
    public async Task Build_RespectsMultipleAclDateRanges()
    {
        var storage = new Storage { Id = 410, Name = "allowed-dates", Folder = "fd" };
        var januaryPhoto = CreatePhoto(411, storage, "jan-photo", new DateTime(2010, 1, 15));
        var decemberPhoto = CreatePhoto(412, storage, "dec-photo", new DateTime(2011, 12, 20));
        var outsidePhoto = CreatePhoto(413, storage, "outside", new DateTime(2011, 6, 1));

        _context.Storages.Add(storage);
        _context.Photos.AddRange(januaryPhoto, decemberPhoto, outsidePhoto);
        await _context.SaveChangesAsync();

        var user = new TestCurrentUser(
            storage.Id,
            new[]
            {
                (new DateOnly(2010, 1, 1), new DateOnly(2010, 1, 31)),
                (new DateOnly(2011, 12, 1), new DateOnly(2011, 12, 31))
            });

        var result = await _sut.Build(new FilterDto(), user)
            .OrderBy(p => p.Id)
            .Select(p => p.Id)
            .ToListAsync();

        result.Should().Equal(januaryPhoto.Id, decemberPhoto.Id);
    }

    [Test]
    public async Task Build_AllowsPhotosWithoutTakenDate_WhenAclHasDateRanges()
    {
        var storage = new Storage { Id = 420, Name = "allowed-null-date", Folder = "fd" };
        var withoutDate = CreatePhoto(421, storage, "no-date");
        var insideRange = CreatePhoto(422, storage, "inside", new DateTime(2012, 5, 10));
        var outsideRange = CreatePhoto(423, storage, "outside", new DateTime(2013, 5, 10));

        _context.Storages.Add(storage);
        _context.Photos.AddRange(withoutDate, insideRange, outsideRange);
        await _context.SaveChangesAsync();

        var user = new TestCurrentUser(
            storage.Id,
            new[]
            {
                (new DateOnly(2012, 1, 1), new DateOnly(2012, 12, 31))
            });

        var result = await _sut.Build(new FilterDto(), user)
            .OrderBy(p => p.Id)
            .Select(p => p.Id)
            .ToListAsync();

        result.Should().Equal(withoutDate.Id, insideRange.Id);
    }

    [Test]
    public void Build_WithCaption_AddsILikeExpression()
    {
        var storage = new Storage { Id = 500, Name = "s500", Folder = "f500" };
        var photo = CreatePhoto(501, storage, "captioned");
        photo.Captions.Add(new Caption { Id = 1, Text = "Sunset" });

        _context.Storages.Add(storage);
        _context.Photos.Add(photo);
        _context.SaveChanges();

        var filter = new FilterDto { Caption = "sun" };

        var query = _sut.Build(filter, new DummyCurrentUser());

        query.Expression.ToString().Should().Contain("ToTsVector");
    }

    private static Photo CreatePhoto(int id, Storage storage, string name, DateTime? takenDate = null)
    {
        return new Photo
        {
            Id = id,
            Storage = storage,
            StorageId = storage.Id,
            Name = name,
            TakenDate = takenDate,
            AccentColor = string.Empty,
            DominantColorBackground = string.Empty,
            DominantColorForeground = string.Empty,
            DominantColors = string.Empty,
            ImageHash = string.Empty,
            RelativePath = string.Empty,
            S3Key_Preview = string.Empty,
            S3ETag_Preview = string.Empty,
            Sha256_Preview = string.Empty,
            S3Key_Thumbnail = string.Empty,
            S3ETag_Thumbnail = string.Empty,
            Sha256_Thumbnail = string.Empty,
            Captions = new List<Caption>(),
            PhotoTags = new List<PhotoTag>(),
            Faces = new List<Face>(),
            Files = new List<File>()
        };
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public TestCurrentUser(int storageId, IEnumerable<(DateOnly From, DateOnly To)>? ranges = null)
        {
            AllowedStorageIds = new HashSet<int> { storageId };
            AllowedPersonGroupIds = new HashSet<int>();
            AllowedDateRanges = ranges?.ToArray() ?? Array.Empty<(DateOnly From, DateOnly To)>();
        }

        public Guid UserId => Guid.Empty;

        public bool IsAdmin => false;

        public IReadOnlySet<int> AllowedStorageIds { get; }

        public IReadOnlySet<int> AllowedPersonGroupIds { get; }

        public IReadOnlyList<(DateOnly From, DateOnly To)> AllowedDateRanges { get; }

        public bool CanSeeNsfw => true;
    }
}

