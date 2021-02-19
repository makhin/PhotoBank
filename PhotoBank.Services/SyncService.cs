using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using File = PhotoBank.DbContext.Models.File;

namespace PhotoBank.Services
{
    public interface ISyncService
    {
        Task<IEnumerable<string>> SyncStorage(Storage storage);
    }

    public class SyncService : ISyncService 
    {
        private const string SupportedExtensions = "*.jpg,*.gif,*.png,*.bmp,*.jpe,*.jpeg,*.tiff,*.arw,*.crw,*.bmp,*.cr2,*.pdf";
        private readonly IRepository<File> _fileRepository;

        public SyncService(IRepository<PhotoBank.DbContext.Models.File> fileRepository)
        {
            _fileRepository = fileRepository;
        }

        public async Task<IEnumerable<string>> SyncStorage(Storage storage)
        {
            var folder = storage.Folder;

            var folderFiles = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
                .Where(s => SupportedExtensions.Contains(Path.GetExtension(s).ToLower()))
                .ToList();

            var storageFiles = await _fileRepository.GetByCondition(f => f.Photo.Storage.Id == storage.Id)
                .Include(f => f.Photo).Select(f => new
                {
                    f.Id, 
                    Path = Path.Combine(folder, f.Photo.RelativePath, f.Name), 
                    f.IsDeleted
                }).ToListAsync();

            foreach (var storageFile in storageFiles)
            {
                if (!folderFiles.Contains(storageFile.Path) && !storageFile.IsDeleted)
                {
                    var file = await _fileRepository.GetAsync(storageFile.Id);
                    file.IsDeleted = true;
                    await _fileRepository.UpdateAsync(file);
                    continue;
                }

                if (folderFiles.Contains(storageFile.Path) && storageFile.IsDeleted)
                {
                    var file = await _fileRepository.GetAsync(storageFile.Id);
                    file.IsDeleted = false;
                    await _fileRepository.UpdateAsync(file);
                }
            }

            var result = folderFiles.Where(ff => !storageFiles.Where(sf => !sf.IsDeleted).Select(sf => sf.Path).Contains(ff)).ToList();
            return result;
        }
    }
}
