using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto.Load;
using PhotoBank.Repositories;
using PhotoBank.Services.Enrichers;
using File = PhotoBank.DbContext.Models.File;
using Storage = PhotoBank.DbContext.Models.Storage;

namespace PhotoBank.Services
{
    public interface IPhotoProcessor
    {
        Task<int> AddPhotoAsync(Storage storage, string path);
        Task AddFacesAsync(Storage storage);
        Task UpdatePhotosAsync(Storage storage);
    }

    public class PhotoProcessor : IPhotoProcessor
    {
        private readonly IRepository<Photo> _photoRepository;
        private readonly IRepository<File> _fileRepository;
        private readonly IRepository<Face> _faceRepository;
        private readonly IEnumerable<IEnricher> _enrichers;

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
            IEnumerable<IEnricher> enrichers,
            IOrderResolver<IEnricher> orderResolver
            )
        {
            var activeEnrichers = enricherRepository.GetAll().Where(e => e.IsActive).Select(e => e.Name).ToList();
            _photoRepository = photoRepository;
            _fileRepository = fileRepository;
            _faceRepository = faceRepository;
            _enrichers = orderResolver.Resolve(enrichers.Where(e => activeEnrichers.Contains(e.GetType().Name)).ToList());
        }

        public async Task<int> AddPhotoAsync(Storage storage, string path)
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

            foreach (var enricher in _enrichers)
            {
                await enricher.Enrich(photo, sourceData);
            }

            try
            {
                await _photoRepository.InsertAsync(photo);
            }
            catch (Exception exception)
            {
                Console.WriteLine("An exception occurred: {0}, {1}", exception.InnerException, exception.Message);
            }

            return photo.Id;
        }

        public async Task AddFacesAsync(Storage storage)
        {
            var files = await _photoRepository
                .GetAll()
                .Include(p => p.Files)
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

                foreach (var enricher in _enrichers)
                {
                    await enricher.Enrich(photo, sourceData);
                }

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

            var file = _fileRepository.GetByCondition(f => f.Name == result.Name && f.Photo.Id == result.PhotoId);
            result.DuplicateStatus = file != null ? DuplicateStatus.FileExists : DuplicateStatus.FileNotExists;
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
