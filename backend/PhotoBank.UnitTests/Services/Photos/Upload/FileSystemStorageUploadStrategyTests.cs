using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Photos.Upload;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoBank.UnitTests.Services.Photos.Upload;

[TestFixture]
public class FileSystemStorageUploadStrategyTests
{
    private static IFormFile CreateFormFile(byte[] content, string fileName)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName);
    }

    [Test]
    public void CanHandle_ReturnsFalse_ForObjectStorage()
    {
        var strategy = new FileSystemStorageUploadStrategy(
            new MockFileSystem(),
            new UploadNameResolver(),
            NullLogger<FileSystemStorageUploadStrategy>.Instance);

        var storage = new Storage { Folder = "s3://bucket/path" };

        strategy.CanHandle(storage).Should().BeFalse();
    }

    [Test]
    public async Task UploadAsync_WritesFileToTargetDirectory()
    {
        var fileSystem = new MockFileSystem();
        var strategy = new FileSystemStorageUploadStrategy(
            fileSystem,
            new UploadNameResolver(),
            NullLogger<FileSystemStorageUploadStrategy>.Instance);

        var storage = new Storage { Id = 1, Folder = "/storage" };
        var file = CreateFormFile([1, 2, 3], "photo.jpg");

        await strategy.UploadAsync(storage, [file], "sub", CancellationToken.None);

        fileSystem.FileExists("/storage/sub/photo.jpg").Should().BeTrue();
    }

    [Test]
    public async Task UploadAsync_SkipsDuplicateWithSameLength()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/storage/photo.jpg", new MockFileData([1, 2, 3]) }
        });

        var strategy = new FileSystemStorageUploadStrategy(
            fileSystem,
            new UploadNameResolver(),
            NullLogger<FileSystemStorageUploadStrategy>.Instance);

        var storage = new Storage { Id = 2, Folder = "/storage" };
        var file = CreateFormFile([1, 2, 3], "photo.jpg");

        await strategy.UploadAsync(storage, [file], null, CancellationToken.None);

        fileSystem.AllFiles.Should().ContainSingle().Which.Should().Be(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"C:\storage\photo.jpg" : "/storage/photo.jpg");
    }

    [Test]
    public async Task UploadAsync_RenamesFileWhenSizeDiffers()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/storage/photo.jpg", new MockFileData([1, 2, 3]) }
        });

        var strategy = new FileSystemStorageUploadStrategy(
            fileSystem,
            new UploadNameResolver(),
            NullLogger<FileSystemStorageUploadStrategy>.Instance);

        var storage = new Storage { Id = 3, Folder = "/storage" };
        var file = CreateFormFile([4, 5], "photo.jpg");

        await strategy.UploadAsync(storage, [file], null, CancellationToken.None);

        fileSystem.FileExists("/storage/photo_1.jpg").Should().BeTrue();
        var newFile = fileSystem.GetFile("/storage/photo_1.jpg");
        newFile.Contents.Length.Should().Be(2);
    }
}
