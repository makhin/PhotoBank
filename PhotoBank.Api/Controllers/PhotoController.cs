using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Api;

namespace PhotoBank.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhotoController : Controller
    {
        private readonly IPhotoService _photoService;

        public PhotoController(IPhotoService photoService)
        {
            _photoService = photoService;
        }

        // GET
        [HttpGet]
        public async Task<IActionResult> GetPhotos()
        {
            var photos = await _photoService.GetAllPhotosAsync();

            if (!photos.Any())
            {
                return NoContent();
            }

            return Ok(photos);
        }

        // GET api/<controller>/5
        [HttpGet("{id}")]
        public IActionResult GetPhotoById(int id)
        {
            var photo = _photoService.GetPhotoAsync(id);

            if (photo == null)
            {
                return NoContent();
            }

            return Ok(photo);
        }
    }
}