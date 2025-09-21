using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using PhotoBank.AccessControl;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;
using PhotoBank.IntegrationTests.Infra;

namespace PhotoBank.IntegrationTests;

[TestFixture]
public class FacesControllerUploadIntegrationTests
{
    private const string AdminRole = "Admin";

    private ApiWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private FacesTestPhotoService _photoService = null!;

    [SetUp]
    public void Setup()
    {
        _photoService = new FacesTestPhotoService();
        var configuration = TestConfiguration.Build(
            "Server=(localdb)\\mssqllocaldb;Database=PhotoBankTests;Trusted_Connection=True;Encrypt=False;");

        _factory = new ApiWebApplicationFactory(
            configuration: configuration,
            configureServices: services =>
            {
                services.RemoveAll<DbContextOptions<PhotoBankDbContext>>();
                services.RemoveAll<PhotoBankDbContext>();
                services.AddDbContext<PhotoBankDbContext>(options =>
                    options.UseInMemoryDatabase($"faces-tests-{Guid.NewGuid():N}"));

                services.RemoveAll<DbContextOptions<AccessControlDbContext>>();
                services.RemoveAll<AccessControlDbContext>();
                services.AddDbContext<AccessControlDbContext>(options =>
                    options.UseInMemoryDatabase($"faces-access-tests-{Guid.NewGuid():N}"));

                services.RemoveAll<IPhotoService>();
                services.AddSingleton<IPhotoService>(_photoService);

                services.AddTestAuthentication(options =>
                {
                    options.SchemeName = "FacesTests";
                    options.ConfigureFallbackPolicy = true;
                });
            });
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task GetFaceImage_WhenFormatUnsupported_ReturnsBadRequest()
    {
        _photoService.SetFaceImageHandler(_ => throw new ArgumentException("Unsupported image format: .tiff"));

        using var request = CreateRequest("/api/faces/42/image");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Contain("json");

        var payload = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);
        document.RootElement.GetProperty("title").GetString().Should().Be("Bad Request");
        document.RootElement.GetProperty("detail").GetString().Should().Contain("Unsupported image format");
    }

    [Test]
    public async Task GetFaceImage_WhenFileTooLarge_ReturnsBadRequest()
    {
        _photoService.SetFaceImageHandler(_ => throw new InvalidOperationException("Face image exceeds the 5 MB limit."));

        using var request = CreateRequest("/api/faces/43/image");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Contain("json");

        var payload = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);
        document.RootElement.GetProperty("title").GetString().Should().Be("Bad Request");
        document.RootElement.GetProperty("detail").GetString().Should().Contain("5 MB limit");
    }

    [Test]
    public async Task GetFaceImage_WhenFaceImageMissing_ReturnsNotFound()
    {
        _photoService.SetFaceImageHandler(_ => Task.FromResult<PhotoPreviewResult?>(null));

        using var request = CreateRequest("/api/faces/44/image");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetFaceImage_WhenMultipleFacesDetected_ReturnsBadRequest()
    {
        _photoService.SetFaceImageHandler(_ => throw new InvalidOperationException("Detected multiple faces in the upload."));

        using var request = CreateRequest("/api/faces/45/image");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Contain("json");

        var payload = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);
        document.RootElement.GetProperty("detail").GetString().Should().Contain("multiple faces");
    }

    private static HttpRequestMessage CreateRequest(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(TestAuthenticationDefaults.UserHeader, "integration-admin");
        request.Headers.Add(TestAuthenticationDefaults.RolesHeader, AdminRole);
        return request;
    }


    private sealed class FacesTestPhotoService : IPhotoService
    {
        private Func<int, Task<PhotoPreviewResult?>> _getFaceImage = _ => Task.FromResult<PhotoPreviewResult?>(null);

        public void SetFaceImageHandler(Func<int, Task<PhotoPreviewResult?>> handler)
        {
            _getFaceImage = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public Task<PhotoPreviewResult?> GetFaceImageAsync(int id) => _getFaceImage(id);

        #region Unused members

        public Task<IEnumerable<PersonDto>> GetAllPersonsAsync() => throw new NotImplementedException();
        public Task<IEnumerable<StorageDto>> GetAllStoragesAsync() => throw new NotImplementedException();
        public Task<IEnumerable<TagDto>> GetAllTagsAsync() => throw new NotImplementedException();
        public Task<IEnumerable<PathDto>> GetAllPathsAsync() => throw new NotImplementedException();
        public Task<IEnumerable<PersonGroupDto>> GetAllPersonGroupsAsync() => throw new NotImplementedException();
        public Task<PersonDto> CreatePersonAsync(string name) => throw new NotImplementedException();
        public Task<PersonDto> UpdatePersonAsync(int personId, string name) => throw new NotImplementedException();
        public Task DeletePersonAsync(int personId) => throw new NotImplementedException();
        public Task<PersonGroupDto> CreatePersonGroupAsync(string name) => throw new NotImplementedException();
        public Task<PersonGroupDto> UpdatePersonGroupAsync(int groupId, string name) => throw new NotImplementedException();
        public Task DeletePersonGroupAsync(int groupId) => throw new NotImplementedException();
        public Task AddPersonToGroupAsync(int groupId, int personId) => throw new NotImplementedException();
        public Task RemovePersonFromGroupAsync(int groupId, int personId) => throw new NotImplementedException();
        public Task<IEnumerable<PersonFaceDto>> GetAllPersonFacesAsync() => throw new NotImplementedException();
        public Task<PersonFaceDto> CreatePersonFaceAsync(PersonFaceDto dto) => throw new NotImplementedException();
        public Task<PersonFaceDto> UpdatePersonFaceAsync(int id, PersonFaceDto dto) => throw new NotImplementedException();
        public Task DeletePersonFaceAsync(int id) => throw new NotImplementedException();
        public Task UpdateFaceAsync(int faceId, int personId) => throw new NotImplementedException();
        public Task<IEnumerable<FaceIdentityDto>> GetFacesAsync(IdentityStatus? status, int? personId) => throw new NotImplementedException();
        public Task UpdateFaceIdentityAsync(int faceId, IdentityStatus identityStatus, int? personId) => throw new NotImplementedException();
        public Task<IEnumerable<PhotoItemDto>> FindDuplicatesAsync(int? id, string? hash, int threshold) => throw new NotImplementedException();
        public Task UploadPhotosAsync(IEnumerable<Microsoft.AspNetCore.Http.IFormFile> files, int storageId, string path) => throw new NotImplementedException();
        public Task<byte[]> GetObjectAsync(string key) => throw new NotImplementedException();
        public Task<PhotoPreviewResult?> GetPhotoPreviewAsync(int id) => throw new NotImplementedException();
        public Task<PhotoPreviewResult?> GetPhotoThumbnailAsync(int id) => throw new NotImplementedException();
        public Task<PageResponse<PhotoItemDto>> GetAllPhotosAsync(FilterDto filter, System.Threading.CancellationToken ct = default) => throw new NotImplementedException();
        public Task<PhotoDto> GetPhotoAsync(int id) => throw new NotImplementedException();
        #endregion
    }
}
