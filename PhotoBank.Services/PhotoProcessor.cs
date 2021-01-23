using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;
using PhotoBank.Dto.Load;
using PhotoBank.Repositories;
using PhotoBank.Services.Enrichers;

namespace PhotoBank.Services
{
    public interface IPhotoProcessor
    {
        Task AddPhotoAsync(Storage storage, string path);
    }

    public class PhotoProcessor : IPhotoProcessor
    {
        private readonly IRepository<Photo> _photoRepository;
        private readonly IRepository<DbContext.Models.File> _fileRepository;
        private readonly IEnumerable<IEnricher> _enrichers;

        public PhotoProcessor(
            IRepository<Photo> photoRepository,
            IRepository<DbContext.Models.File> fileRepository,
            IEnumerable<IEnricher> enrichers,
            IOrderResolver<IEnricher> orderResolver
            )
        {
            _photoRepository = photoRepository;
            _fileRepository = fileRepository;
            _enrichers = orderResolver.Resolve(enrichers);
        }

        public async Task AddPhotoAsync(Storage storage, string path)
        {
            var startTime = DateTime.Now;

            await VerifyDuplicates(storage, path);

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

            var ms = 3000 - (int)(DateTime.Now - startTime).TotalMilliseconds;
            if (ms <= 0)
            {
                return;
            }
            Console.WriteLine($"Wait {ms}");
            await Task.Delay(ms);
        }

        private async Task VerifyDuplicates(Storage storage, string path)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            var relativePath = Path.GetRelativePath(storage.Folder, Path.GetDirectoryName(path));

            var photoId = await _photoRepository.GetByCondition(p =>
                p.Name == name && p.RelativePath == relativePath && p.Storage.Id == storage.Id).Select(p => p.Id).SingleOrDefaultAsync();

            if (photoId == 0)
            {
                return;
            }

            var fileName = Path.GetFileName(path);
            await _fileRepository.InsertAsync(new DbContext.Models.File
            {
                Photo = new Photo {Id = photoId},
                Name = fileName,
            });
        }
    }
}
