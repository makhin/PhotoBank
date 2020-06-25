using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;

namespace PhotoBank.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhotoController : Controller
    {
        private readonly IRepository<Photo> _photoRepository;

        public PhotoController(IRepository<Photo> photoRepository)
        {
            _photoRepository = photoRepository;
        }

        // GET
        [HttpGet]
        public async Task<IActionResult> GetPhotos()
        {
            var photos = await _photoRepository.GetAll().OrderBy(p => p.Name).Select(p => new { p.Id, p.Name })
                .ToListAsync();

            if (!photos.Any())
            {
                return NoContent();
            }

            return Ok(photos);
        }

        // GET api/<controller>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPhotoById(int id)
        {
            var photo = await _photoRepository.Get(id, photos => photos);

            if (photo == null)
            {
                return NoContent();
            }

            return Ok(photo);
        }
    }
}