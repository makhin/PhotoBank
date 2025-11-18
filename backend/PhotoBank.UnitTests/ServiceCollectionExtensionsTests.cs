using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Rekognition;
using FluentValidation;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using NUnit.Framework;
using PhotoBank.AccessControl;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.DependencyInjection;
using PhotoBank.Repositories;
using PhotoBank.Services;
using PhotoBank.Services.Api;
using PhotoBank.Services.Enrichment;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Enrichers.Services;
using PhotoBank.Services.Events;
using PhotoBank.Services.FaceRecognition;
using PhotoBank.Services.FaceRecognition.Aws;
using PhotoBank.Services.FaceRecognition.Azure;
using PhotoBank.Services.FaceRecognition.Local;
using PhotoBank.Services.FaceRecognition.Abstractions;
using PhotoBank.Services.Photos;
using PhotoBank.Services.Internal;
using PhotoBank.Services.Models;
using PhotoBank.Services.Recognition;
using PhotoBank.Services.Search;
using PhotoBank.Services.Translator;
using Minio;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PhotoBank.UnitTests;

[TestFixture]
public class ServiceCollectionExtensionsTests
{
    [Test]
    public void AddPhotobankApi_RegistersCoreServicesWithExpectedLifetimes()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "issuer",
                ["Jwt:Audience"] = "audience",
                ["Jwt:Key"] = new string('k', 64)
            })
            .Build();

        services.AddPhotobankApi(configuration);

        AssertSingletonRegistration<ITokenService, TokenService>(services);
        AssertSingletonRegistration<IImageService, ImageService>(services);
        AssertScopedRegistration<IEffectiveAccessProvider, EffectiveAccessProvider>(services);
        AssertScopedRegistration<IAccessProfileService, AccessProfileService>(services);

        var accessorDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICurrentUserAccessor));
        accessorDescriptor.Should().NotBeNull("ICurrentUserAccessor must be registered");
        accessorDescriptor!.Lifetime.ToString().Should().Be("Scoped");

        services.Should().Contain(d => d.ServiceType == typeof(IHttpContextAccessor) && d.Lifetime.ToString() == "Singleton");

        AssertNoDuplicateRegistrations(services,
            typeof(ITokenService),
            typeof(IImageService),
            typeof(IEffectiveAccessProvider),
            typeof(IAccessProfileService),
            typeof(ICurrentUserAccessor));
    }

    [Test]
    public void AddPhotobankCors_DefinesAllowAllPolicy()
    {
        var services = new ServiceCollection();

        services.AddPhotobankCors();

        using var provider = services.BuildServiceProvider();
        var corsOptions = provider.GetRequiredService<IOptions<CorsOptions>>().Value;
        var policy = corsOptions.GetPolicy("AllowAll");
        policy.Should().NotBeNull();
        policy!.SupportsCredentials.Should().BeTrue();
        policy.Headers.Should().Contain("*");
        policy.Methods.Should().Contain("*");
    }

    [Test]
    public void AddPhotobankMvc_ConfiguresOptionsAndValidation()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(local);Database=photobank;Trusted_Connection=True;"
            })
            .Build();

        services.AddPhotobankMvc(configuration);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IConfigureOptions<RouteOptions>>();

        var routeOptions = provider.GetRequiredService<IOptions<RouteOptions>>().Value;
        routeOptions.LowercaseUrls.Should().BeTrue();

        var behavior = provider.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;
        behavior.InvalidModelStateResponseFactory.Should().NotBeNull();

        var validators = services.Count(d => d.ServiceType == typeof(IValidatorFactory));
        validators.Should().BeGreaterThan(0);
    }

    [Test]
    public void AddPhotobankSwagger_RegistersSwaggerWithCustomConfig()
    {
        var services = new ServiceCollection();
        var configureInvocations = 0;

        services.AddPhotobankSwagger(options =>
        {
            configureInvocations++;
            options.DocumentFilter<TestDocumentFilter>();
        });

        using var provider = services.BuildServiceProvider();
        var configureOptions = provider.GetServices<IConfigureOptions<SwaggerGenOptions>>();
        var swaggerOptions = new SwaggerGenOptions();
        foreach (var configure in configureOptions)
        {
            configure.Configure(swaggerOptions);
        }

        configureInvocations.Should().Be(1);
        swaggerOptions.SwaggerGeneratorOptions.SwaggerDocs.Should().ContainKey("v1");
        swaggerOptions.DocumentFilterDescriptors.Should().Contain(df => df.Type == typeof(TestDocumentFilter));
    }

    [Test]
    public void AddPhotoEvents_RegistersMediatRHandlers()
    {
        var services = new ServiceCollection();

        services.AddPhotoEvents();

        var handlerDescriptor = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(INotificationHandler<>) &&
            d.ServiceType.GenericTypeArguments[0] == typeof(PhotoCreated));

        handlerDescriptor.Should().NotBeNull("PhotoCreated handler should be registered");
    }

    [Test]
    public void AddPhotobankCore_RegistersExpectedServicesAndOptions()
    {
        var services = new ServiceCollection();
        services.TryAddSingleton<IDbContextFactory<PhotoBankDbContext>>(new FakeDbContextFactory());
        services.AddScoped<PhotoBankDbContext>(sp => sp.GetRequiredService<IDbContextFactory<PhotoBankDbContext>>().CreateDbContext());
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Translator:Endpoint"] = "https://translator",
                ["Translator:Region"] = "westeurope",
                ["Translator:Key"] = "translator-key",
                ["Minio:Endpoint"] = "minio",
                ["Minio:AccessKey"] = "access",
                ["Minio:SecretKey"] = "secret",
                ["S3:Bucket"] = "bucket"
            })
            .Build();

        services.AddPhotobankCore(configuration);

        services.Should().Contain(d => d.ServiceType == typeof(PhotoBankDbContext));
        AssertFactoryRegistration<IMinioClient>(services, "Singleton");
        AssertScopedRegistration(typeof(IRepository<>), typeof(Repository<>), services);
        AssertSingletonRegistration<IFileSystem, FileSystem>(services);
        AssertScopedRegistration<IFaceStorageService, FaceStorageService>(services);
        AssertScopedRegistration<MinioObjectService, MinioObjectService>(services);
        AssertScopedRegistration<IMediaUrlResolver, MediaUrlResolver>(services);
        AssertScopedRegistration<ISearchFilterNormalizer, SearchFilterNormalizer>(services);
        AssertScopedRegistration<PhotoFilterSpecification, PhotoFilterSpecification>(services);
        AssertScopedRegistration<IPhotoDuplicateFinder, PhotoDuplicateFinder>(services);
        AssertScopedRegistration<IPhotoIngestionService, PhotoIngestionService>(services);
        AssertScopedRegistration<IPhotoService, PhotoService>(services);
        AssertScopedRegistration<ISearchReferenceDataService, SearchReferenceDataService>(services);
        AssertSingletonRegistration<IActiveEnricherProvider, ActiveEnricherProvider>(services);
        AssertHttpClientRegistration<ITranslatorService, TranslatorService>(services);

        services.Should().Contain(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(INotificationHandler<>) &&
            d.ServiceType.GenericTypeArguments[0] == typeof(PhotoCreated));
        AssertNoDuplicateRegistrations(services,
            typeof(IRepository<>),
            typeof(IFaceStorageService),
            typeof(MinioObjectService),
            typeof(IMediaUrlResolver),
            typeof(ISearchFilterNormalizer),
            typeof(PhotoFilterSpecification),
            typeof(IPhotoDuplicateFinder),
            typeof(IPhotoIngestionService),
            typeof(IPhotoService),
            typeof(ISearchReferenceDataService));

        services.RemoveAll(typeof(IRepository<>));
        services.AddScoped(typeof(IRepository<>), typeof(FakeRepository<>));

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IMinioClient>().Should().NotBeNull();
        provider.GetRequiredService<IPhotoDuplicateFinder>().Should().NotBeNull();
        provider.GetRequiredService<IPhotoIngestionService>().Should().NotBeNull();
        provider.GetRequiredService<IMediaUrlResolver>().Should().NotBeNull();
        provider.GetRequiredService<IMediator>().Should().NotBeNull();
    }

    [Test]
    public async Task AddPhotobankConsole_RegistersConsoleServicesWithCorrectLifetimes()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ComputerVision:Key"] = "cv-key",
                ["ComputerVision:Endpoint"] = "https://cv",
                ["Face:Key"] = "face-key",
                ["Face:Endpoint"] = "https://face",
                ["FaceProvider:Default"] = FaceProviderKind.Local.ToString(),
                ["LocalInsightFace:BaseUrl"] = "http://localhost",
                ["AzureFace:Endpoint"] = "https://azure",
                ["AzureFace:Key"] = "azure-key",
                ["AwsRekognition:CollectionId"] = "collection"
            })
            .Build();

        services.AddPhotobankConsole(configuration);

        AssertFactoryRegistration<IComputerVisionClient>(services, "Singleton");
        AssertFactoryRegistration<IFaceClient>(services, "Singleton");
        AssertSingletonRegistration(services, typeof(AmazonRekognitionClient), typeof(AmazonRekognitionClient));
        AssertTransientRegistration<IFaceService, FaceService>(services);
        AssertTransientRegistration<IFacePreviewService, FacePreviewService>(services);
        AssertTransientRegistration<IFaceServiceAws, FaceServiceAws>(services);
        AssertTransientRegistration<IPhotoProcessor, PhotoProcessor>(services);
        AssertSingletonRegistration<ICurrentUser, DummyCurrentUser>(services);
        AssertTransientRegistration<IImageService, ImageService>(services);
        AssertTransientRegistration<ISyncService, SyncService>(services);
        AssertTransientRegistration<IRecognitionService, RecognitionService>(services);
        AssertTransientRegistration<IImageMetadataReaderWrapper, ImageMetadataReaderWrapper>(services);
        AssertScopedRegistration<IUnifiedFaceService, UnifiedFaceService>(services);
        AssertScopedRegistration<IFaceService, FaceService>(services);
        AssertSingletonRegistration<IEnrichmentPipeline, EnrichmentPipeline>(services);
        AssertFactoryRegistration<EnricherTypeCatalog>(services, "Singleton");
        AssertFactoryRegistration<EnricherResolver>(services, "Singleton");
        AssertSingletonRegistration<IActiveEnricherProvider, ActiveEnricherProvider>(services);

        AssertEnricherRegistration(services);

        AssertNoDuplicateRegistrations(services,
            typeof(IFaceService),
            typeof(IFacePreviewService),
            typeof(IFaceServiceAws),
            typeof(IPhotoProcessor),
            typeof(ICurrentUser),
            typeof(IImageService),
            typeof(ISyncService),
            typeof(IEnricher),
            typeof(IImageMetadataReaderWrapper),
            typeof(IRecognitionService),
            typeof(IEnrichmentPipeline),
            typeof(IUnifiedFaceService));

        var activeEnrichers = new[]
        {
            new Enricher { Name = nameof(MetadataEnricher), IsActive = true },
            new Enricher { Name = nameof(TagEnricher), IsActive = false }
        };
        services.AddSingleton<IRepository<Enricher>>(new FakeEnricherRepository(activeEnrichers));

        foreach (var descriptor in services.Where(d => d.ServiceType == typeof(IEnricher) && d.ImplementationType is not null).ToList())
        {
            services.AddTransient(descriptor.ImplementationType!);
        }

        services.TryAddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        using var provider = services.BuildServiceProvider();
        var activeEnricherProvider = provider.GetRequiredService<IActiveEnricherProvider>();
        activeEnricherProvider.Should().BeOfType<ActiveEnricherProvider>();
        var enricherRepository = provider.GetRequiredService<IRepository<Enricher>>();
        var activeTypes = activeEnricherProvider.GetActiveEnricherTypes(enricherRepository);
        activeTypes.Should().Contain(typeof(MetadataEnricher));
        activeTypes.Should().NotContain(typeof(TagEnricher));

        var resolver = provider.GetRequiredService<EnricherResolver>();
        var resolved = resolver(provider.GetRequiredService<IRepository<Enricher>>()).ToList();
        resolved.Should().ContainSingle(e => e is MetadataEnricher);
        resolved.Should().NotContain(e => e is TagEnricher);

        var pipeline = provider.GetRequiredService<IEnrichmentPipeline>();
        pipeline.Should().BeOfType<EnrichmentPipeline>();
        Func<Task> run = async () => await pipeline.RunAsync(new Photo(), new SourceDataDto(), Array.Empty<Type>(), CancellationToken.None);
        await run.Should().NotThrowAsync();
    }

    [Test]
    public void AddPhotobankDbContext_RegistersContextsWithPool()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
        services.TryAddSingleton<ICurrentUserAccessor>(new StaticCurrentUserAccessor(new DummyCurrentUser()));
        var configuration = BuildDbConfiguration();

        services.AddPhotobankDbContext(configuration, usePool: true);

        AssertSingletonRegistration<DbTimingInterceptor, DbTimingInterceptor>(services);
        AssertFactoryRegistration<PhotoBankDbContext>(services, "Scoped");
        services.Should().Contain(d => d.ServiceType == typeof(IDbContextPool<PhotoBankDbContext>) && d.Lifetime.ToString() == "Singleton");
        AssertScopedRegistration<AccessControlDbContext, AccessControlDbContext>(services);

        services.TryAddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.TryAddSingleton<IDbContextFactory<PhotoBankDbContext>>(new FakeDbContextFactory());
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        using var pooledContext = scope.ServiceProvider.GetRequiredService<PhotoBankDbContext>();
        pooledContext.Should().NotBeNull();
    }

    [Test]
    public void AddPhotobankDbContext_RegistersContextsWithoutPool()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
        services.TryAddSingleton<ICurrentUserAccessor>(new StaticCurrentUserAccessor(new DummyCurrentUser()));
        var configuration = BuildDbConfiguration();

        services.AddPhotobankDbContext(configuration, usePool: false);

        AssertSingletonRegistration<DbTimingInterceptor, DbTimingInterceptor>(services);
        AssertFactoryRegistration<PhotoBankDbContext>(services, "Scoped");
        services.Should().Contain(d => d.ServiceType == typeof(DbContextOptions<PhotoBankDbContext>) && d.Lifetime.ToString() == "Scoped");
        AssertScopedRegistration<AccessControlDbContext, AccessControlDbContext>(services);

        services.TryAddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<PhotoBankDbContext>();
        context.Should().NotBeNull();
    }

    private static IConfiguration BuildDbConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(local);Database=photobank;Trusted_Connection=True;"
            })
            .Build();
    }

    private static void AssertEnricherRegistration(ServiceCollection services)
    {
        var enrichers = services
            .Where(d => d.ServiceType == typeof(IEnricher) && d.ImplementationType is not null)
            .ToList();

        var expected = new[]
        {
            typeof(MetadataEnricher),
            typeof(ThumbnailEnricher),
            typeof(PreviewEnricher),
            typeof(AnalyzeEnricher),
            typeof(ColorEnricher),
            typeof(CaptionEnricher),
            typeof(TagEnricher),
            typeof(ObjectPropertyEnricher),
            typeof(AdultEnricher),
            typeof(UnifiedFaceEnricher),
            typeof(FaceEnricher),
            typeof(FaceEnricherAws)
        };

        enrichers.Select(d => d.ImplementationType).Should().BeEquivalentTo(expected, options => options.WithoutStrictOrdering());
        enrichers.Should().OnlyContain(d => d.Lifetime.ToString() == "Transient");
    }

    private static void AssertSingletonRegistration<TService, TImplementation>(ServiceCollection services)
        where TImplementation : TService
    {
        AssertSingletonRegistration(services, typeof(TService), typeof(TImplementation));
    }

    private static void AssertSingletonRegistration(ServiceCollection services, Type serviceType, Type implementationType)
    {
        var descriptors = services
            .Where(d => d.ServiceType == serviceType && d.ImplementationType == implementationType)
            .ToList();

        descriptors.Should().NotBeEmpty($"{serviceType.Name} should be registered");
        descriptors.Should().Contain(d => d.Lifetime.ToString() == "Singleton");
    }

    private static void AssertScopedRegistration<TService, TImplementation>(ServiceCollection services)
        where TImplementation : TService
    {
        AssertScopedRegistration(typeof(TService), typeof(TImplementation), services);
    }

    private static void AssertScopedRegistration(Type serviceType, Type implementationType, ServiceCollection services)
    {
        var descriptors = services
            .Where(d => d.ServiceType == serviceType && d.ImplementationType == implementationType)
            .ToList();

        descriptors.Should().NotBeEmpty($"{serviceType.Name} should be registered");
        descriptors.Should().Contain(d => d.Lifetime.ToString() == "Scoped");
    }

    private static void AssertTransientRegistration<TService, TImplementation>(ServiceCollection services)
        where TImplementation : TService
    {
        var descriptors = services
            .Where(d => d.ServiceType == typeof(TService) && d.ImplementationType == typeof(TImplementation))
            .ToList();

        descriptors.Should().NotBeEmpty($"{typeof(TService).Name} should be registered");
        descriptors.Should().Contain(d => d.Lifetime.ToString() == "Transient");
    }

    private static void AssertFactoryRegistration<TService>(ServiceCollection services, string expectedLifetime)
    {
        var descriptors = services
            .Where(d => d.ServiceType == typeof(TService) && d.ImplementationFactory is not null)
            .ToList();

        descriptors.Should().NotBeEmpty($"{typeof(TService).Name} should be registered");
        descriptors.Should().Contain(d => d.Lifetime.ToString() == expectedLifetime);
    }

    private static void AssertFactoryRegistration(ServiceCollection services, Type serviceType, string expectedLifetime)
    {
        var descriptors = services
            .Where(d => d.ServiceType == serviceType && d.ImplementationFactory is not null)
            .ToList();

        descriptors.Should().NotBeEmpty($"{serviceType.Name} should be registered");
        descriptors.Should().Contain(d => d.Lifetime.ToString() == expectedLifetime);
    }

    private static void AssertHttpClientRegistration<TClient, TImplementation>(ServiceCollection services)
        where TClient : class
        where TImplementation : class, TClient
    {
        using var provider = services.BuildServiceProvider();
        var instance = provider.GetRequiredService<TClient>();
        instance.Should().BeOfType<TImplementation>();
    }

    private static void AssertNoDuplicateRegistrations(ServiceCollection services, params Type[] serviceTypes)
    {
        foreach (var serviceType in serviceTypes)
        {
            var duplicates = services
                .Where(d => d.ServiceType == serviceType)
                .GroupBy(d => (Implementation: GetImplementationKey(d), Lifetime: d.Lifetime.ToString()))
                .Where(g => g.Count() > 1)
                .ToList();

            duplicates.Should().BeEmpty($"Duplicate registrations detected for {serviceType.Name}");
        }
    }

    private static string GetImplementationKey(ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationType is not null)
        {
            return descriptor.ImplementationType.FullName ?? descriptor.ImplementationType.Name;
        }

        if (descriptor.ImplementationInstance is not null)
        {
            return descriptor.ImplementationInstance.GetType().FullName ?? descriptor.ImplementationInstance.GetType().Name;
        }

        if (descriptor.ImplementationFactory is not null)
        {
            var method = descriptor.ImplementationFactory.Method;
            return $"factory:{method.DeclaringType?.FullName}.{method.Name}";
        }

        return "unknown";
    }

    private sealed class StaticCurrentUserAccessor : ICurrentUserAccessor
    {
        private readonly ICurrentUser _user;

        public StaticCurrentUserAccessor(ICurrentUser user)
        {
            _user = user;
        }

        public ValueTask<ICurrentUser> GetCurrentUserAsync(CancellationToken ct = default)
            => ValueTask.FromResult(_user);

        public ICurrentUser CurrentUser => _user;
    }

    private sealed class TestDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class FakeEnricherRepository : IRepository<Enricher>
    {
        private readonly IQueryable<Enricher> _items;

        public FakeEnricherRepository(IEnumerable<Enricher> items)
        {
            _items = items.AsQueryable();
        }

        public IQueryable<Enricher> GetAll() => _items;
        public IQueryable<Enricher> GetByCondition(Expression<Func<Enricher, bool>> predicate) => _items.Where(predicate);
        public Task<Enricher> GetAsync(int id, Func<IQueryable<Enricher>, IQueryable<Enricher>> queryable) => throw new NotSupportedException();
        public Enricher Get(int id, Func<IQueryable<Enricher>, IQueryable<Enricher>> queryable) => throw new NotSupportedException();
        public Task<Enricher> GetAsync(int id) => throw new NotSupportedException();
        public Enricher Get(int id) => throw new NotSupportedException();
        public Task<Enricher> InsertAsync(Enricher entity) => throw new NotSupportedException();
        public Task InsertRangeAsync(List<Enricher> entities) => throw new NotSupportedException();
        public Task<Enricher> UpdateAsync(Enricher entity) => throw new NotSupportedException();
        public Task<int> UpdateAsync(Enricher entity, params Expression<Func<Enricher, object>>[] properties) => throw new NotSupportedException();
        public Task<int> DeleteAsync(int id) => throw new NotSupportedException();
    }

    private sealed class FakeDbContextFactory : IDbContextFactory<PhotoBankDbContext>
    {
        public PhotoBankDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<PhotoBankDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new PhotoBankDbContext(options);
        }
    }

    private sealed class FakeRepository<T> : IRepository<T>
        where T : class, IEntityBase, new()
    {
        public Task<int> DeleteAsync(int id) => Task.FromResult(0);

        public T Get(int id) => new();

        public T Get(int id, Func<IQueryable<T>, IQueryable<T>> queryable) => new();

        public IQueryable<T> GetAll() => Array.Empty<T>().AsQueryable();

        public IQueryable<T> GetByCondition(Expression<Func<T, bool>> predicate) => GetAll();

        public Task<T> GetAsync(int id) => Task.FromResult(new T());

        public Task<T> GetAsync(int id, Func<IQueryable<T>, IQueryable<T>> queryable) => Task.FromResult(new T());

        public Task<T> InsertAsync(T entity) => Task.FromResult(entity);

        public Task InsertRangeAsync(List<T> entities) => Task.CompletedTask;

        public Task<T> UpdateAsync(T entity) => Task.FromResult(entity);

        public Task<int> UpdateAsync(T entity, params Expression<Func<T, object>>[] properties) => Task.FromResult(0);
    }
}
