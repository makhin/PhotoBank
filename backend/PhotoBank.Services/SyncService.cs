﻿using System;
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
        private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".gif",
            ".png",
            ".bmp",
            ".jpe",
            ".jpeg",
            ".tiff",
            ".arw",
            ".crw",
            ".cr2",
            ".pdf"
        };
        private readonly IRepository<File> _fileRepository;

        public SyncService(IRepository<File> fileRepository)
        {
            _fileRepository = fileRepository;
        }

        public async Task<IEnumerable<string>> SyncStorage(Storage storage)
        {
            // Files on disk relative to storage folder
            var folderFiles = new HashSet<string>(
                Directory.EnumerateFiles(storage.Folder, "*", SearchOption.AllDirectories)
                    .Where(f => SupportedExtensions.Contains(Path.GetExtension(f)))
                    .Select(p => Path.GetRelativePath(storage.Folder, p)),
                StringComparer.OrdinalIgnoreCase);

            // Files in DB
            var storageFiles = await _fileRepository
                .GetByCondition(f => f.Photo.Storage.Id == storage.Id)
                .Include(f => f.Photo)
                .Select(f => new
                {
                    f.Id,
                    Path = Path.Combine(f.Photo.RelativePath ?? string.Empty, f.Name),
                    f.IsDeleted
                })
                .ToListAsync();

            // Alive files in DB
            var dbAlive = new HashSet<string>(
                storageFiles.Where(sf => !sf.IsDeleted).Select(sf => sf.Path),
                StringComparer.OrdinalIgnoreCase);

            // Reactivate: file marked deleted but present on disk
            foreach (var sf in storageFiles.Where(sf => sf.IsDeleted))
            {
                if (folderFiles.Contains(sf.Path))
                {
                    var file = await _fileRepository.GetAsync(sf.Id);
                    file.IsDeleted = false;
                    await _fileRepository.UpdateAsync(file);
                }
            }

            // New files: exist on disk but not in DB
            var newFiles = folderFiles.Except(dbAlive, StringComparer.OrdinalIgnoreCase).ToList();
            return newFiles;
        }
    }
}
