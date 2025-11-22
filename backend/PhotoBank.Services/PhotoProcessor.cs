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
using File = PhotoBank.DbContext.Models.File;
using Storage = PhotoBank.DbContext.Models.Storage;
using ImageMagick;

namespace PhotoBank.Services
{
    public interface IPhotoProcessor
    {
        Task<int> AddPhotoAsync(Storage storage, string path, IReadOnlyCollection<Type>? activeEnrichers = null);
        Task<bool> IsDuplicateAsync(Storage storage, string path);
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
            IMediator mediator
            )
        {
            _photoRepository = photoRepository;
            _fileRepository = fileRepository;
            _faceRepository = faceRepository;
            _enricherRepository = enricherRepository;
            _enrichmentPipeline = enrichmentPipeline;
            _activeEnricherProvider = activeEnricherProvider;
            _mediator = mediator;
        }

        public async Task<int> AddPhotoAsync(Storage storage, string path, IReadOnlyCollection<Type>? activeEnrichers = null)
        {
            var duplicate = await VerifyDuplicates(storage, path);

            if (duplicate.DuplicateStatus == DuplicateStatus.FileExists)
            {
                return duplicate.PhotoId;
            }

            if (duplicate.DuplicateStatus == DuplicateStatus.FileNotExists)
            {
                await _fileRepository.InsertAsync(new File
                {
                    Photo = new Photo {Id = duplicate.PhotoId},
                    Name = duplicate.Name,
                });
                return duplicate.PhotoId;
            }

            var sourceData = new SourceDataDto { AbsolutePath = path };
            var photo = new Photo { Storage = storage };

            var enrichersToUse = activeEnrichers ?? _activeEnricherProvider.GetActiveEnricherTypes(_enricherRepository);
            await _enrichmentPipeline.RunAsync(photo, sourceData, enrichersToUse);

            if (sourceData.PreviewImage != null)
            {
                photo.ImageHash = ImageHashHelper.ComputeHash(sourceData.PreviewImage.ToByteArray());
            }

            try
            {
                await _photoRepository.InsertAsync(photo);
            }
            catch (Exception exception)
            {
                Console.WriteLine("An exception occurred: {0}, {1}", exception.InnerException, exception.Message);
            }

            var faces = (photo.Faces ?? new List<Face>())
                .Zip(sourceData.FaceImages, (f, img) => new PhotoCreatedFace(f.Id, img))
                .ToList();
            var evt = new PhotoCreated(photo.Id, sourceData.PreviewImage.ToByteArray(), sourceData.ThumbnailImage, faces);
            await _mediator.Publish(evt);

            return photo.Id;
        }

        public async Task<bool> IsDuplicateAsync(Storage storage, string path)
        {
            var duplicate = await VerifyDuplicates(storage, path);
            return duplicate.DuplicateStatus == DuplicateStatus.FileExists;
        }

        public async Task AddFacesAsync(Storage storage)
        {
            var files = await _photoRepository
                .GetAll()
                .Include(p => p.Files)
                .AsNoTracking()
                .Where(p => p.StorageId == storage.Id && p.FaceIdentifyStatus == FaceIdentifyStatus.Undefined)
                .Select(p => new PhotoFilePath
                {
                    PhotoId =p.Id,
                    RelativePath = p.RelativePath,
                    Files = p.Files
                }).ToListAsync();

            await UpdateInfoAsync(storage, files,
                photoFile => _faceRepository.GetAll().Any(f => f.PhotoId == photoFile.PhotoId), InsertFacesAsync);
        }

        public async Task UpdatePhotosAsync(Storage storage)
        {
            var files = await _photoRepository
                .GetAll()
                .AsNoTracking()
                .Where(p => p.StorageId == storage.Id && p.TakenDate == null)
                .Select(p => new PhotoFilePath
                {
                    PhotoId = p.Id,
                    RelativePath = p.RelativePath,
                    Files = p.Files
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

                var absolutePath = Path.Combine(storage.Folder, photoFile.RelativePath, photoFile.Files.First().Name);
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

                await _enrichmentPipeline.RunAsync(photo, sourceData, activeEnrichers, CancellationToken.None);

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

        private async Task<DuplicateVerification> VerifyDuplicates(Storage storage, string path)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            var relativePath = Path.GetRelativePath(storage.Folder, Path.GetDirectoryName(path));
            var result = new DuplicateVerification
            {
                PhotoId = await _photoRepository.GetByCondition(p =>
                        p.Name == name && p.RelativePath == relativePath && p.Storage.Id == storage.Id)
                    .Select(p => p.Id)
                    .SingleOrDefaultAsync(),
                Name = Path.GetFileName(path)
            };
            
            if (result.PhotoId == 0)
            {
                result.DuplicateStatus = DuplicateStatus.PhotoNotExists;
                return result;
            }

            var fileExists = await _fileRepository
                .GetByCondition(f => f.Name == result.Name && f.Photo.Id == result.PhotoId)
                .AnyAsync();
            result.DuplicateStatus = fileExists ? DuplicateStatus.FileExists : DuplicateStatus.FileNotExists;
            return result;
        }

        private class DuplicateVerification
        {
            public DuplicateStatus DuplicateStatus { get; set; }
            public int PhotoId { get; init; }
            public string Name { get; init; }
        }

        private enum DuplicateStatus
        {
            PhotoNotExists,
            FileNotExists,
            FileExists
        }
    }
}
