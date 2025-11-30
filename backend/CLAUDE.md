# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PhotoBank is a .NET 9.0 photo management system with AI-powered enrichment capabilities. The system processes photos through an enrichment pipeline that extracts metadata, detects objects/faces, generates captions, and stores media in S3-compatible object storage (MinIO). It uses PostgreSQL with pgvector extension for face embedding similarity search.

## Common Commands

### Build and Run
```bash
# Build the entire solution
dotnet build PhotoBank.sln

# Run the API server (starts on http://0.0.0.0:5066)
dotnet run --project PhotoBank.Api

# Run the console app for batch photo processing
dotnet run --project PhotoBank.Console -- [options]

# Console app options:
# --storage <id> (-s): Process photos from specific storage ID
# --no-register: Skip person registration step
# migrate-embeddings: Migrate face embeddings for all faces
# re-enrich: Re-run enrichers on processed photos
#   --photo-ids <ids> (-p): Specific photo IDs
#   --enrichers <names> (-e): Specific enricher names
#   --missing-only (-m): Only apply missing enrichers
#   --limit <n> (-l): Max number of photos
#   --dry-run: Show what would be processed
# delete-photos: Delete photos and S3 objects
#   --photo-id <id> (-p): Delete specific photo
#   --last <n> (-l): Delete last N photos
```

### Testing
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test PhotoBank.UnitTests
dotnet test PhotoBank.IntegrationTests

# Run tests with filter
dotnet test --filter "FullyQualifiedName~Namespace.Class"

# Run specific test
dotnet test --filter "FullyQualifiedName=Namespace.ClassName.MethodName"
```

### Database Migrations
```bash
# Note: EF Core tools must be installed globally first
# Install: dotnet tool install --global dotnet-ef

# Add a new migration (run from backend directory)
dotnet ef migrations add <MigrationName> --project PhotoBank.DbContext

# Update database to latest migration
dotnet ef database update --project PhotoBank.DbContext

# Revert to specific migration
dotnet ef database update <MigrationName> --project PhotoBank.DbContext
```

## Architecture

### Project Structure

**Core Projects:**
- `PhotoBank.DbContext` - EF Core models, DbContext, and migrations for PostgreSQL
- `PhotoBank.Repositories` - Generic repository pattern implementation
- `PhotoBank.Services` - Business logic, enrichment pipeline, and photo processing
- `PhotoBank.DependencyInjection` - Service registration and configuration extensions

**API Projects:**
- `PhotoBank.Api` - REST API with Swagger, runs on port 5066 with `/api` path base
- `PhotoBank.ServerBlazorApp` - Blazor Server UI
- `PhotoBank.MAUI.Blazor` - MAUI Blazor mobile/desktop app

**CLI Projects:**
- `PhotoBank.Console` - Batch processing CLI using System.CommandLine

**Supporting Projects:**
- `PhotoBank.ViewModel.Dto` - DTOs for API/UI communication
- `PhotoBank.InsightFaceApiClient` - Client for InsightFace API (face recognition)
- `PhotoBank.BlobMigrator` - S3/blob migration utilities

**Test Projects:**
- `PhotoBank.UnitTests` - NUnit tests with Moq, FluentAssertions
- `PhotoBank.IntegrationTests` - Integration tests

### Dependency Injection Pattern

The project uses extension methods in `PhotoBank.DependencyInjection` to register services:
- `AddPhotobankDbContext(config, usePool)` - DbContext with pooling option
- `AddPhotobankCore(config)` - Core services, repositories, enrichers
- `AddPhotobankApi(config)` - API-specific services
- `AddPhotobankConsole(config)` - Console-specific services
- `AddPhotobankMvc(config)` - MVC services
- `AddPhotobankCors()` - CORS configuration
- `AddPhotobankSwagger()` - Swagger/OpenAPI

These are chained in Program.cs files to compose the full service container.

### Enrichment Pipeline

The photo enrichment system is the core of PhotoBank's processing:

**Pipeline Flow:**
1. Photo is added via `PhotoProcessor.AddPhotoAsync()`
2. `EnrichmentPipeline.RunAsync()` executes enrichers in topologically sorted order
3. Each enricher implements `IEnricher` and declares dependencies via `IOrderDependent`
4. Enrichers modify the `Photo` entity and `SourceDataDto` with extracted data
5. Pipeline can stop early based on `IEnrichmentStopCondition` (e.g., adult content detected)
6. After enrichment, `PhotoCreated` event is published via MediatR

**Key Enrichers:**
- `MetadataEnricher` - Extracts EXIF data (date, GPS, camera info)
- `PreviewEnricher` - Generates preview images
- `ThumbnailEnricher` - Generates thumbnails
- `UnifiedFaceEnricher` - Detects faces using configured provider (InsightFace/Azure/AWS)
- `UnifiedObjectPropertyEnricher` - Detects objects using YOLO ONNX models
- `AdultEnricher` - Detects adult/racy content using NudeNet ONNX
- `CaptionEnricher` - Generates captions using image analysis (OpenRouter/Ollama)
- `ColorEnricher` - Analyzes dominant colors
- `CategoryEnricher`, `TagEnricher` - Categorize and tag based on detected content

**Enricher Dependencies:**
Enrichers declare dependencies to ensure proper execution order. For example:
- Face/object detection depends on metadata extraction
- Caption generation depends on face/object detection
- All enrichers that need images depend on Preview/Thumbnail enrichers

**Re-enrichment:**
The `IReEnrichmentService` allows re-running enrichers on already-processed photos:
- `ReEnrichPhotoAsync()` - Force re-run specific enrichers
- `ReEnrichMissingAsync()` - Only run enrichers not yet applied

### Database Context and Access Control

`PhotoBankDbContext` extends `IdentityDbContext<ApplicationUser>` and implements row-level security:
- `ConfigureUser(ICurrentUser)` - Sets user permissions (storage access, date ranges, NSFW visibility)
- `ResetState()` - Clears permissions (used in pooled contexts)
- Uses pgvector extension for face embedding similarity search
- Custom middleware `CurrentUserInitializationMiddleware` configures context per request

### Event System

Uses MediatR for domain events:
- `PhotoCreated` - Published after photo is successfully enriched
- `PhotoCreatedHandler` - Handles post-creation tasks (e.g., S3 upload of preview/thumbnail/faces)

### Storage Architecture

**File Storage:**
- Original photos stored on filesystem in configured storage paths
- `Storage` entities define base folders
- Photos reference storage + relative path

**Object Storage (S3/MinIO):**
- Previews, thumbnails, and face images uploaded to MinIO
- `MinioObjectService` handles S3 operations
- `IMediaUrlResolver` generates pre-signed URLs for client access
- S3 keys stored in database for retrieval

### Face Recognition

Pluggable face provider system via `IRecognitionService`:
- **LocalInsightFace** (default) - HTTP client to InsightFace server, stores embeddings in pgvector
- **AzureFace** - Azure Cognitive Services Face API
- **AwsRekognition** - AWS Rekognition

Configuration via `appsettings.json`:
```json
{
  "FaceProvider": { "Default": "Local" },
  "LocalInsightFace": {
    "BaseUrl": "http://localhost:5555",
    "Model": "buffalo_l",
    "FaceMatchThreshold": 0.45
  }
}
```

### Object Detection (ONNX)

On-device ML using ONNX Runtime:
- **YOLO** (`YoloOnnxService`) - General object detection
- **NudeNet** (`NudeNetDetector`) - Adult/racy content detection

Models configured in `appsettings.json` with paths, thresholds, and enable flags.

### Image Analysis / Captioning

Pluggable image analyzer via `IImageAnalyzer`:
- **OpenRouter** - Uses OpenRouter API with configurable models (default: gpt-4o-mini)
- **Ollama** - Local Ollama with vision models (e.g., qwen2.5vl)

## Configuration

Configuration is hierarchical via `appsettings.json`:
- Database: PostgreSQL connection string
- Logging: Serilog with console and file sinks (JSON formatted)
- JWT: Authentication configuration
- MinIO/S3: Object storage endpoints and credentials
- Face providers: InsightFace, Azure Face API, AWS Rekognition settings
- ONNX models: Paths and thresholds for YOLO/NudeNet
- Image analysis: OpenRouter/Ollama configuration
- Processing: `MaxDegreeOfParallelism` for batch enrichment

## Development Patterns

### Repository Pattern
All data access goes through `IRepository<T>`:
```csharp
public interface IRepository<T>
{
    Task<T> GetByIdAsync(int id);
    IQueryable<T> GetAll();
    Task InsertAsync(T entity);
    Task UpdateAsync(T entity, params Expression<Func<T, object>>[] properties);
    Task DeleteAsync(int id);
}
```

### Service Scoping
- Repositories and most services are scoped (per-request lifetime)
- Enrichment pipeline can use provided `IServiceProvider` to share DbContext for transactions
- Background tasks create scopes via `IServiceScopeFactory`

**CRITICAL: DbContext Thread-Safety**
- `PhotoBankDbContext` is NOT thread-safe and must not be shared across parallel operations
- When using `Parallel.ForEach` or similar constructs, ALWAYS create a new scope for each iteration:
  ```csharp
  await Parallel.ForEachAsync(items, async (item, ct) =>
  {
      await using var scope = serviceProvider.CreateAsyncScope();
      var service = scope.ServiceProvider.GetRequiredService<IMyService>();
      await service.ProcessAsync(item);
  });
  ```
- Never inject scoped services (like `IPhotoProcessor`, `IRepository<T>`) into singleton services that perform parallel operations
- See `PhotoBank.Console/App.cs:128-155` for a correct implementation example

**CRITICAL: Detached Entities Across Scopes**
- Entities loaded in one DbContext cannot be used in another DbContext without being reloaded
- Passing entity objects across scopes causes EF Core to attempt INSERT on existing entities
- **SOLUTION**: Always reload entities by ID in each new scope:
  ```csharp
  // BAD - storage from parent scope
  await Parallel.ForEachAsync(files, async (file, ct) =>
  {
      await using var scope = serviceProvider.CreateAsyncScope();
      var processor = scope.ServiceProvider.GetRequiredService<IPhotoProcessor>();
      await processor.AddPhotoAsync(storage, file); // ERROR: storage is detached!
  });

  // GOOD - reload storage in each scope
  var storageId = storage.Id;
  await Parallel.ForEachAsync(files, async (file, ct) =>
  {
      await using var scope = serviceProvider.CreateAsyncScope();
      var storageRepo = scope.ServiceProvider.GetRequiredService<IRepository<Storage>>();
      var scopedStorage = await storageRepo.GetAsync(storageId);
      var processor = scope.ServiceProvider.GetRequiredService<IPhotoProcessor>();
      await processor.AddPhotoAsync(scopedStorage, file); // OK: storage tracked in this context
  });
  ```
- See `PhotoBank.Console/App.cs:114-140` for correct implementation

### Testing
- Unit tests use `Microsoft.EntityFrameworkCore.InMemory` provider
- `System.IO.Abstractions.TestingHelpers` for filesystem mocking
- Moq for mocking dependencies
- FluentAssertions for readable assertions

## Important Notes

- **PostgreSQL required**: Uses pgvector extension for embeddings, computed columns for date parts
- **npgsql timestamp handling**: `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)` treats unspecified DateTime as UTC
- **DbContext pooling**: API uses non-pooled contexts by default due to per-request user state (`ConfigureUser`)
- **Path handling**: Always use `Path.Combine()` for cross-platform compatibility
- **Cancellation**: All async operations should accept `CancellationToken` and check it in loops
- **Error handling**: API returns RFC7807 ProblemDetails; exceptions logged with correlation IDs
- **CORS**: Configured for `AllowAll` in development
- **Swagger**: Always enabled (not just in development mode)

## API Structure

- Base path: `/api`
- Health checks: `/health` (readiness), `/liveness`
- Minimal API endpoints: `/Paths`, `/Tags`, `/Storages`
- Controllers: Standard ASP.NET Core MVC controllers
- Authentication: JWT bearer tokens
- Logging: Serilog with request logging middleware
