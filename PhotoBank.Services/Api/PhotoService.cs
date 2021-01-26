using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto.View;
using PhotoBank.Repositories;

namespace PhotoBank.Services.Api
{
    public interface IPhotoService
    {
        Task<IEnumerable<PhotoItemDto>> GetAllPhotosAsync();
        public IQueryable<PhotoItemDto> GetAllPhotos();
        Task<PhotoDto> GetPhotoAsync(int id);
        Task<List<PersonDto>> GetAllPersonsAsync();
        Task UpdateFaceAsync(FaceDto faceDto);
    }

    public class PhotoService : IPhotoService
    {
        private readonly IRepository<Photo> _photoRepository;
        private readonly IRepository<Person> _personRepository;
        private readonly IRepository<Face> _faceRepository;
        private readonly IMapper _mapper;
        private readonly Lazy<Task<List<PersonDto>>> _persons;

        public PhotoService(IRepository<Photo> photoRepository, IRepository<Person> personRepository, IRepository<Face> faceRepository, IMapper mapper)
        {
            _photoRepository = photoRepository;
            _personRepository = personRepository;
            _faceRepository = faceRepository;
            _mapper = mapper;
            _persons = new Lazy<Task<List<PersonDto>>>(() => personRepository.GetAll().ProjectTo<PersonDto>(_mapper.ConfigurationProvider).ToListAsync());
        }

        public async Task<IEnumerable<PhotoItemDto>> GetAllPhotosAsync()
        {
            return await _photoRepository.GetAll().Take(50).ProjectTo<PhotoItemDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public IQueryable<PhotoItemDto> GetAllPhotos()
        {
            return _photoRepository.GetAll().ProjectTo<PhotoItemDto>(_mapper.ConfigurationProvider);
        }

        public async Task<PhotoDto> GetPhotoAsync(int id)
        {
            var photo = await _photoRepository.GetAsync(id,
                p => p
                    .Include(p1 => p1.Faces)
                    .ThenInclude(f => f.Person)
                    .Include(p1 => p1.Captions)
                    .Include(p1 => p1.PhotoTags)
                    .ThenInclude(t => t.Tag)
            );
            return _mapper.Map<Photo, PhotoDto>(photo);
        }

        public async Task<List<PersonDto>> GetAllPersonsAsync()
        {
            return await _persons.Value;
        }

        public async Task UpdateFaceAsync(FaceDto faceDto)
        {
            var face = new Face {Id = faceDto.Id};
            if (faceDto.PersonId.HasValue && faceDto.PersonId.Value != 0)
            {
                face.Person = await _personRepository.GetAsync(faceDto.PersonId.Value);
                await _faceRepository.UpdateAsync(face, f => f.Person.Id);
            }
            else
            {
                await _faceRepository.UpdateAsync(face, f => f.Person);
            }
        }
    }
}