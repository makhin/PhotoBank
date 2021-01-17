using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;
using PhotoBank.Repositories;

namespace PhotoBank.Services.Api
{
    public interface IPhotoService
    {
        Task<IEnumerable<PhotoItemDto>> GetAllAsync();
        PhotoDto Get(int id);
    }

    public class PhotoService : IPhotoService
    {
        private readonly IRepository<Photo> _photoRepository;
        private readonly IMapper _mapper;

        public PhotoService(IRepository<Photo> photoRepository, IMapper mapper)
        {
            _photoRepository = photoRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<PhotoItemDto>> GetAllAsync()
        {
            return await _photoRepository.GetAll().ProjectTo<PhotoItemDto>(_mapper.ConfigurationProvider).ToListAsync();
        }

        public PhotoDto Get(int id)
        {
            var photo = _photoRepository.Get(id);
            return _mapper.Map<Photo, PhotoDto>(photo);
        }
    }
}
