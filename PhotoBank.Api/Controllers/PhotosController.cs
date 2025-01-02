using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhotoBank.Dto.View;
using PhotoBank.Services.Api;
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

        [HttpGet(Name = "GetPhotos")]
        public async Task<ActionResult<QueryResult>> GetPhotos()
        {
            QueryResult? photos = await _photoService.GetAllPhotosAsync(new FilterDto(), null, 0, 10);
            return photos;
        }

        [HttpGet(Name = "GetPhoto")]
        public async Task<ActionResult<PhotoDto>> GetPhoto()
        {
            return null;
        }

        
        [HttpGet(Name = "GetStorages")]
        public async Task<ActionResult<IEnumerable<StorageDto>>> GetStorages()
        {
            IEnumerable<StorageDto>? storages = await _photoService.GetAllStoragesAsync();
            return null;
        }
        /*
                [HttpGet(Name = "GetPaths")]
                public async Task<ActionResult<IEnumerable<PathDto>>> GetPaths()
                {
                    var paths = await _photoService.GetAllPathsAsync();
                    return new ActionResult<IEnumerable<PathDto>>(paths);
                }

                [HttpGet(Name = "GetPersons")]
                public async Task<ActionResult<IEnumerable<PersonDto>>> GetPersons()
                {
                    var persons = await _photoService.GetAllPersonsAsync();
                    return new ActionResult<IEnumerable<PersonDto>>(persons);
                }

                [HttpGet(Name = "GetTags")]
                public async Task<ActionResult<IEnumerable<TagDto>>> GetTags()
                {
                    var tags = await _photoService.GetAllTagsAsync();
                    return new ActionResult<IEnumerable<TagDto>>(tags);
                }

        */
    }
}
