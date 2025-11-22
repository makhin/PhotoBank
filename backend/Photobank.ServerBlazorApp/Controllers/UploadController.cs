using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services;

namespace PhotoBank.ServerBlazorApp.Controllers
{
    [DisableRequestSizeLimit]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IPhotoProcessor _photoProcessor;
        private readonly IRepository<Storage> _repository;

        public UploadController(IWebHostEnvironment webHostEnvironment, IPhotoProcessor photoProcessor, IRepository<Storage> repository)
        {
            _webHostEnvironment = webHostEnvironment;
            _photoProcessor = photoProcessor;
            _repository = repository;
        }

        [HttpPost("upload/single")]
        public IActionResult Single(IFormFile file)
        {
            try
            {
                var photoId = UploadFile(file).Result;
                return StatusCode(200, photoId);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        public async Task<int> UploadFile(IFormFile file)
        {
            if (file is { Length: > 0 })
            {
                //var imagePath = @"\Upload";
                //var uploadPath = _webHostEnvironment.WebRootPath + imagePath;
                var uploadPath = @"d:\Test";
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var fullPath = Path.Combine(uploadPath, file.FileName);
                await using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                {
                    await file.CopyToAsync(fileStream);
                }

                var storage = await _repository.GetAsync(12);
                var (photoId, _) = await _photoProcessor.AddPhotoAsync(storage, fullPath);

                return photoId;
            }

            return 0;
        }
    }
}
