using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Enrichment;
using PhotoBank.Services.Events;
using PhotoBank.Services.Models;
using PhotoBank.Services.Photos;
using File = PhotoBank.DbContext.Models.File;
using Storage = PhotoBank.DbContext.Models.Storage;

namespace PhotoBank.Services
{
    public enum PhotoProcessResult
    {
        Added,
        Duplicate,
        Skipped
    }

    public interface IPhotoProcessor
    {
        Task<(int PhotoId, PhotoProcessResult Result, string? SkipReason, EnrichmentStats? Stats)> AddPhotoAsync(Storage storage, string path, IReadOnlyCollection<Type>? activeEnrichers = null);
        Task AddFacesAsync(Storage storage);
        Task UpdatePhotosAsync(Storage storage);
    }

    public class PhotoProcessor : IPhotoProcessor
    {
        private readonly IRepository<Photo> _photoRepository;
        private readonly IRepository<File> _fileRepository;
        private readonly IRepository<Face> _faceRepository;
        private readonly IRepository<Enricher> _enricherRepository;
        private readonly IEnrichmentPipeline _enrichmentPipeline;
        private readonly IActiveEnricherProvider _activeEnricherProvider;
        private readonly IMediator _mediator;
        private readonly IPhotoFileSystemDuplicateChecker _duplicateChecker;
        private readonly IServiceProvider _serviceProvider;

        private class PhotoFilePath
        {
            public int PhotoId { get; init; }
            public string RelativePath { get; init; }
            public List<File> Files { get; init; }
        }

        public PhotoProcessor(
            IRepository<Photo> photoRepository,
            IRepository<File> fileRepository,
            IRepository<Face> faceRepository,
            IRepository<Enricher> enricherRepository,
            IEnrichmentPipeline enrichmentPipeline,
            IActiveEnricherProvider activeEnricherProvider,
            IMediator mediator,
            IPhotoFileSystemDuplicateChecker duplicateChecker,
            IServiceProvider serviceProvider
            )
        {
            _photoRepository = photoRepository;
            _fileRepository = fileRepository;
            _faceRepository = faceRepository;
            _enricherRepository = enricherRepository;
            _enrichmentPipeline = enrichmentPipeline;
            _activeEnricherProvider = activeEnricherProvider;
            _mediator = mediator;
            _duplicateChecker = duplicateChecker;
            _serviceProvider = serviceProvider;
        }

        private static string BuildAbsolutePath(Storage storage, params string[] pathSegments)
        {
            var allSegments = new List<string> { storage.Folder };
            allSegments.AddRange(pathSegments);
            return Path.Combine(allSegments.ToArray());
        }

        private static string GetRelativePath(Storage storage, string path)
        {
            var directoryName = Path.GetDirectoryName(path);
            var relativePath = string.IsNullOrEmpty(directoryName) ? string.Empty : directoryName;

            // Convert "." to empty string for files in root directory
            if (relativePath == ".")
            {
                relativePath = string.Empty;
            }

            return relativePath;
        }

        public async Task<(int PhotoId, PhotoProcessResult Result, string? SkipReason, EnrichmentStats? Stats)> AddPhotoAsync(Storage storage, string path, IReadOnlyCollection<Type>? activeEnrichers = null)
        {
            var duplicateResult = await HandleDuplicateCheckAsync(storage, path);
            if (duplicateResult.HasValue)
            {
                return (duplicateResult.Value.PhotoId, PhotoProcessResult.Duplicate, null, null);
            }

            var (photo, sourceData, enrichmentResult) = await CreateAndEnrichPhotoAsync(storage, path, activeEnrichers);

            // If enrichment was stopped (e.g., adult content detected), handle appropriately
            if (enrichmentResult.StopReason != null)
            {
                // Check if this is a duplicate photo detected by DuplicateEnricher
                if (sourceData.DuplicatePhotoId.HasValue)
                {
                    // Add File entry to existing Photo instead of creating new Photo
                    var fileName = Path.GetFileName(path);
                    var relativePath = GetRelativePath(storage, path);

                    await _fileRepository.InsertAsync(new File
                    {
                        PhotoId = sourceData.DuplicatePhotoId.Value,
                        StorageId = storage.Id,
                        RelativePath = relativePath,
                        Name = fileName,
                        IsDeleted = false
                    });

                    return (sourceData.DuplicatePhotoId.Value, PhotoProcessResult.Duplicate, null, enrichmentResult.Stats);
                }

                // Other stop reasons (e.g., adult content)
                return (0, PhotoProcessResult.Skipped, enrichmentResult.StopReason, enrichmentResult.Stats);
            }

            await InsertPhotoAsync(photo);
            await PublishPhotoCreatedEventAsync(photo, sourceData);

            return (photo.Id, PhotoProcessResult.Added, null, enrichmentResult.Stats);
        }

        private async Task<(int PhotoId, bool WasDuplicate)?> HandleDuplicateCheckAsync(Storage storage, string path)
        {
            var duplicate = await _duplicateChecker.VerifyDuplicatesAsync(storage, path);

            switch (duplicate.DuplicateStatus)
            {
                case DuplicateStatus.FileExists:
                    return (duplicate.PhotoId, true);
                case DuplicateStatus.FileNotExists:
                    await _fileRepository.InsertAsync(new File
                    {
                        Photo = new Photo { Id = duplicate.PhotoId },
                        StorageId = storage.Id,
                        RelativePath = GetRelativePath(storage, path),
                        Name = duplicate.Name,
                    });
                    return (duplicate.PhotoId, false);
                case DuplicateStatus.PhotoNotExists:
                default:
                    return null;
            }
        }

        private async Task<(Photo Photo, SourceDataDto SourceData, EnrichmentResult Result)> CreateAndEnrichPhotoAsync(
            Storage storage,
            string path,
            IReadOnlyCollection<Type>? activeEnrichers)
        {
            var absolutePath = BuildAbsolutePath(storage, path);
            var sourceData = new SourceDataDto
            {
                AbsolutePath = absolutePath,
                Storage = storage
            };
            var photo = new Photo();

            var enrichersToUse = activeEnrichers ?? _activeEnricherProvider.GetActiveEnricherTypes(_enricherRepository);
            // Pass serviceProvider so enrichers use the same DbContext as PhotoProcessor
            var enrichmentResult = await _enrichmentPipeline.RunAsync(photo, sourceData, enrichersToUse, _serviceProvider);

            return (photo, sourceData, enrichmentResult);
        }

        private async Task InsertPhotoAsync(Photo photo)
        {
            try
            {
                await _photoRepository.InsertAsync(photo);
            }
            catch (Exception exception)
            {
                Console.WriteLine("An exception occurred: {0}, {1}", exception.InnerException, exception.Message);
            }
        }

        private async Task PublishPhotoCreatedEventAsync(Photo photo, SourceDataDto sourceData)
        {
            var faces = (photo.Faces ?? new List<Face>())
                .Zip(sourceData.FaceImages, (f, img) => new PhotoCreatedFace(f.Id, img))
                .ToList();

            // Use RelativePath from the first File entry (for cross-storage support)
            var relativePath = photo.Files?.FirstOrDefault()?.RelativePath ?? string.Empty;
            var evt = new PhotoCreated(photo.Id, photo.Storage.Name, relativePath, sourceData.PreviewImageBytes, sourceData.ThumbnailImage, faces);
            await _mediator.Publish(evt);
        }


        public async Task AddFacesAsync(Storage storage)
        {
            // Filter by Files.StorageId instead of Photo.StorageId for cross-storage support
            var files = await _photoRepository
                .GetAll()
                .Include(p => p.Files)
                .AsNoTracking()
                .Where(p => p.Files.Any(f => f.StorageId == storage.Id) && p.FaceIdentifyStatus == FaceIdentifyStatus.Undefined)
                .Select(p => new PhotoFilePath
                {
                    PhotoId = p.Id,
                    RelativePath = p.Files.First(f => f.StorageId == storage.Id).RelativePath,
                    Files = p.Files.Where(f => f.StorageId == storage.Id).ToList()
                }).ToListAsync();

            await UpdateInfoAsync(storage, files,
                photoFile => _faceRepository.GetAll().Any(f => f.PhotoId == photoFile.PhotoId), InsertFacesAsync);
        }

        public async Task UpdatePhotosAsync(Storage storage)
        {
            // Filter by Files.StorageId instead of Photo.StorageId for cross-storage support
            var files = await _photoRepository
                .GetAll()
                .Include(p => p.Files)
                .AsNoTracking()
                .Where(p => p.Files.Any(f => f.StorageId == storage.Id) && p.TakenDate == null)
                .Select(p => new PhotoFilePath
                {
                    PhotoId = p.Id,
                    RelativePath = p.Files.First(f => f.StorageId == storage.Id).RelativePath,
                    Files = p.Files.Where(f => f.StorageId == storage.Id).ToList()
                }).ToListAsync();

            await UpdateInfoAsync(storage, files,
                _ => false,
                async delegate(Photo photo)
                {
                    if (photo.TakenDate == null)
                    {
                        return;
                    }

                    await _photoRepository.UpdateAsync(
                        new Photo
                        {
                            Id = photo.Id,
                            TakenDate = photo.TakenDate
                        }, p => p.TakenDate);
                });
        }

        private async Task InsertFacesAsync(Photo photo)
        {
            await _photoRepository.UpdateAsync(new Photo
            {
                Id = photo.Id,
                FaceIdentifyStatus = photo.Faces == null ? FaceIdentifyStatus.NotDetected : FaceIdentifyStatus.Detected
            }, p => p.FaceIdentifyStatus);

            if (photo.Faces == null)
            {
                return;
            }

            foreach (var face in photo.Faces)
            {
                await _faceRepository.InsertAsync(face);
            }
        }

        private async Task UpdateInfoAsync(Storage storage, IEnumerable<PhotoFilePath> files, Func<PhotoFilePath, bool> skipCondition, Func<Photo, Task> updateAction)
        {
            var activeEnrichers = _activeEnricherProvider.GetActiveEnricherTypes(_enricherRepository);

            foreach (var photoFile in files)
            {
                if (skipCondition(photoFile))
                {
                    continue;
                }

                var absolutePath = BuildAbsolutePath(storage, photoFile.RelativePath, photoFile.Files.First().Name);
                if (!Path.Exists(absolutePath))
                {
                    continue;
                }

                var sourceData = new SourceDataDto
                {
                    AbsolutePath = absolutePath,
                };

                var photo = new Photo
                {
                    Id = photoFile.PhotoId,
                    Storage = storage
                };

                // Pass serviceProvider so enrichers use the same DbContext
                await _enrichmentPipeline.RunAsync(photo, sourceData, activeEnrichers, _serviceProvider, CancellationToken.None);

                try
                {
                    await updateAction(photo);
                }
                catch (Exception exception)
                {
                    Console.WriteLine("An exception occurred: {0}, {1}", exception.InnerException, exception.Message);
                }
            }
        }

    }
}
