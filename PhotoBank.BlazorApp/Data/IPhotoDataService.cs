using System.Collections.Generic;
using System.Threading.Tasks;
using PhotoBank.Dto;
using PhotoBank.Dto.View;

namespace PhotoBank.BlazorApp.Data
{
    public interface IPhotoDataService
    {
        Task<IEnumerable<PhotoDto>> GetAllPhotos();
        Task<PhotoDto> GetPhotoById(int photoId);
    }
}