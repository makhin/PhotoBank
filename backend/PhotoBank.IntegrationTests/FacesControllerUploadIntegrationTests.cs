using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Minio;
using Moq;
using NUnit.Framework;
using PhotoBank.AccessControl;
using PhotoBank.Api;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.IntegrationTests;

[TestFixture]
public class FacesControllerUploadIntegrationTests
{
    private const string AdminRole = "Admin";
    private const string UserHeader = "X-Test-User";
    private const string RolesHeader = "X-Test-Roles";

    private TestWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private FacesTestPhotoService _photoService = null!;

    [SetUp]
    public void Setup()
    {
        _photoService = new FacesTestPhotoService();
        _factory = new TestWebApplicationFactory(_photoService);
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
        request.Headers.Add(UserHeader, "integration-admin");
        request.Headers.Add(RolesHeader, AdminRole);
        return request;
    }

    private sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly FacesTestPhotoService _photoService;

        public TestWebApplicationFactory(FacesTestPhotoService photoService)
        {
            _photoService = photoService;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(Environments.Development);
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                var overrides = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=PhotoBankTests;Trusted_Connection=True;Encrypt=False;",
                    ["Jwt:Issuer"] = "issuer",
                    ["Jwt:Audience"] = "audience",
                    ["Jwt:Key"] = "super-secret-test-key"
                };
                configBuilder.AddInMemoryCollection(overrides);
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<PhotoBankDbContext>>();
                services.RemoveAll<PhotoBankDbContext>();
                services.AddDbContext<PhotoBankDbContext>(options =>
                    options.UseInMemoryDatabase($"faces-tests-{Guid.NewGuid():N}"));

                services.RemoveAll<DbContextOptions<AccessControlDbContext>>();
                services.RemoveAll<AccessControlDbContext>();
                services.AddDbContext<AccessControlDbContext>(options =>
                    options.UseInMemoryDatabase($"faces-access-tests-{Guid.NewGuid():N}"));

                services.RemoveAll<IMinioClient>();
                services.AddSingleton(Mock.Of<IMinioClient>());

                services.RemoveAll<IPhotoService>();
                services.AddSingleton<IPhotoService>(_photoService);

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    options.DefaultScheme = TestAuthHandler.SchemeName;
                }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

                services.PostConfigure<AuthorizationOptions>(options =>
                {
                    options.FallbackPolicy = new AuthorizationPolicyBuilder()
                        .AddAuthenticationSchemes(TestAuthHandler.SchemeName)
                        .RequireAuthenticatedUser()
                        .Build();
                });
            });
        }
    }

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "FacesTests";

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            System.Text.Encodings.Web.UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(UserHeader, out var userValues))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var user = userValues.ToString();
            if (string.IsNullOrWhiteSpace(user))
            {
                return Task.FromResult(AuthenticateResult.Fail("User header missing"));
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user),
                new(ClaimTypes.Name, user)
            };

            if (Request.Headers.TryGetValue(RolesHeader, out var rolesValues))
            {
                var roles = rolesValues.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
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
