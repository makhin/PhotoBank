﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;
using System.Diagnostics.Metrics;

namespace PhotoBank.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhotosController : ControllerBase
    {
        private readonly ILogger<PhotosController> _logger;
        private readonly IPhotoService _photoService;

        public PhotosController(ILogger<PhotosController> logger, IPhotoService photoService)
        {
            _logger = logger;
            _photoService = photoService;
        }

        [HttpPost("GetPhotos")]
        public async Task<ActionResult<QueryResult>> GetPhotos(FilterDto request)
        {
            QueryResult? photos = await _photoService.GetAllPhotosAsync(request);
            return photos;
        }

        [HttpGet("GetPhoto")]
        public async Task<ActionResult<PhotoDto>> GetPhoto(int id)
        {
            var photo = await _photoService.GetPhotoAsync(id);
            return photo;
        }

        [HttpGet("GetStorages")]
        public async Task<ActionResult<IEnumerable<StorageDto>>> GetStorages()
        {
            IEnumerable<StorageDto>? storages = await _photoService.GetAllStoragesAsync();
            return new ActionResult<IEnumerable<StorageDto>>(storages);
        }

        [HttpGet("GetPaths")]
        public async Task<ActionResult<IEnumerable<PathDto>>> GetPaths()
        {
            var paths = await _photoService.GetAllPathsAsync();
            return new ActionResult<IEnumerable<PathDto>>(paths);
        }

        [HttpGet("GetPersons")]
        public async Task<ActionResult<IEnumerable<PersonDto>>> GetPersons()
        {
            var persons = await _photoService.GetAllPersonsAsync();
            return new ActionResult<IEnumerable<PersonDto>>(persons);
        }

        [HttpGet("GetTags")]
        public async Task<ActionResult<IEnumerable<TagDto>>> GetTags()
        {
            var tags = await _photoService.GetAllTagsAsync();
            return new ActionResult<IEnumerable<TagDto>>(tags);
        }
    }
}
