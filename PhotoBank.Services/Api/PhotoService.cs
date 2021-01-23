using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;
using PhotoBank.Dto.View;
using PhotoBank.Repositories;

namespace PhotoBank.Services.Api
{
    public interface IPhotoService
    {
        Task<IEnumerable<PhotoItemDto>> GetAllAsync();
        Task<PhotoDto> GetAsync(int id);
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
            return await _photoRepository.GetAll().Take(50).ProjectTo<PhotoItemDto>(_mapper.ConfigurationProvider).ToListAsync();
        }

        public async Task<PhotoDto> GetAsync(int id)
        {
            var photo = await _photoRepository.GetAsync(id, p => p.Include(p1 => p1.Faces).ThenInclude(f => f.Person));
            return _mapper.Map<Photo, PhotoDto>(photo);
        }
    }
}
