using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
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
        public IQueryable<PhotoItemDto> GetAllPhotos(FilterDto filter);
        Task<PhotoDto> GetPhotoAsync(int id);
        Task<List<PersonDto>> GetAllPersonsAsync();
        Task<List<StorageDto>> GetAllStoragesAsync();
        Task<List<TagDto>> GetAllTagsAsync();
        Task UpdateFaceAsync(FaceDto faceDto);
    }

    public class PhotoService : IPhotoService
    {
        private readonly IRepository<Photo> _photoRepository;
        private readonly IRepository<Person> _personRepository;
        private readonly IRepository<Face> _faceRepository;
        private readonly IMapper _mapper;
        private readonly Lazy<Task<List<PersonDto>>> _persons;
        private readonly Lazy<Task<List<StorageDto>>> _storages;
        private readonly Lazy<Task<List<TagDto>>> _tags;

        public PhotoService(
            IRepository<Photo> photoRepository,
            IRepository<Person> personRepository,
            IRepository<Face> faceRepository,
            IRepository<Storage> storageRepository,
            IRepository<Tag> tagRepository,
            IMapper mapper)
        {
            _photoRepository = photoRepository;
            _personRepository = personRepository;
            _faceRepository = faceRepository;
            _mapper = mapper;
            _persons = new Lazy<Task<List<PersonDto>>>(() => personRepository.GetAll().OrderBy(p => p.Name).ProjectTo<PersonDto>(_mapper.ConfigurationProvider).ToListAsync());
            _storages = new Lazy<Task<List<StorageDto>>>(() => storageRepository.GetAll().OrderBy(p => p.Name).ProjectTo<StorageDto>(_mapper.ConfigurationProvider).ToListAsync());
            _tags = new Lazy<Task<List<TagDto>>>(() => tagRepository.GetAll().OrderBy(p => p.Name).ProjectTo<TagDto>(_mapper.ConfigurationProvider).ToListAsync());
        }

        public async Task<IEnumerable<PhotoItemDto>> GetAllPhotosAsync()
        {
            return await _photoRepository.GetAll().Take(50).ProjectTo<PhotoItemDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public IQueryable<PhotoItemDto> GetAllPhotos(FilterDto filter)
        {
            var photos = _photoRepository.GetAll();

            if (filter.IsBW.HasValue)
            {
                photos = photos.Where(p => p.IsBW);
            }

            if (filter.IsAdultContent.HasValue)
            {
                photos = photos.Where(p => p.IsAdultContent);
            }

            if (filter.IsRacyContent.HasValue)
            {
                photos = photos.Where(p => p.IsRacyContent);
            }

            if (filter.Storages != null && filter.Storages.Any())
            {
                photos = photos
                    .Include(p => p.Storage)
                    .Where(p => filter.Storages.ToList().Contains(p.Storage.Id));
            }

            if (filter.Persons != null && filter.Persons.Any())
            {
                var list = filter.Persons.ToList<int>();

                photos = photos
                    .Include(p => p.Faces)
                    .ThenInclude(f => f.Person)
                    .Where(p => p.Faces != null)
                    .Where(p => p.Faces.Any())
                    .Where(p => p.Faces
                        .Select(f => f.Person.Id)
                        .Any(x => list.Contains(x)));
            }


            if (filter.Tags != null && filter.Tags.Any())
            {
                var list = filter.Tags.ToList<int>();

                photos = photos
                    .Include(p => p.PhotoTags)
                    .ThenInclude(f => f.Tag)
                    .Where(p => p.PhotoTags != null)
                    .Where(p => p.PhotoTags.Any())
                    .Where(p => p.PhotoTags
                        .Select(f => f.Tag.Id)
                        .Any(x => list.Contains(x)));
            }

            return photos
                .ProjectTo<PhotoItemDto>(_mapper.ConfigurationProvider);
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

        public async Task<List<StorageDto>> GetAllStoragesAsync()
        {
            return await _storages.Value;
        }

        public async Task<List<TagDto>> GetAllTagsAsync()
        {
            return await _tags.Value;
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