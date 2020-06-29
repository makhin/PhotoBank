using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        Task<IEnumerable<PhotoItemDto>> GetAll();
        Task<PhotoDto> Get(int id);
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

        public async Task<IEnumerable<PhotoItemDto>> GetAll()
        {
            var items = await _photoRepository.GetAll().ProjectTo<PhotoItemDto>(_mapper.ConfigurationProvider).ToListAsync();
            return items;
        }

        public async Task<PhotoDto> Get(int id)
        {
            var photo = await _photoRepository.Get(id, photos => photos);
            var photoDto = _mapper.Map<Photo, PhotoDto>(photo);
            return photoDto;
        }
    }
}
