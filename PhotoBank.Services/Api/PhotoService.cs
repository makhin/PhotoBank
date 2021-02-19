using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto.View;
using PhotoBank.Repositories;

namespace PhotoBank.Services.Api
{
    public interface IPhotoService
    {
        Task<QueryResult> GetAllPhotosAsync(FilterDto filter, int? skip, int? top);
        Task<PhotoDto> GetPhotoAsync(int id);
        Task<IEnumerable<PersonDto>> GetAllPersonsAsync();
        Task<IEnumerable<StorageDto>> GetAllStoragesAsync();
        Task<IEnumerable<TagDto>> GetAllTagsAsync();
        Task<IEnumerable<PathDto>> GetAllPathsAsync();
        Task UpdateFaceAsync(int faceId, int personId);
    }

    public class PhotoService : IPhotoService
    {
        private readonly IRepository<Photo> _photoRepository;
        private readonly IRepository<Face> _faceRepository;
        private readonly IMapper _mapper;
        private readonly Lazy<Task<List<PersonDto>>> _persons;
        private readonly Lazy<Task<List<StorageDto>>> _storages;
        private readonly Lazy<Task<List<TagDto>>> _tags;
        private readonly Lazy<Task<List<PathDto>>> _paths;

        public PhotoService(
            IRepository<Photo> photoRepository,
            IRepository<Person> personRepository,
            IRepository<Face> faceRepository,
            IRepository<Storage> storageRepository,
            IRepository<Tag> tagRepository,
            IMapper mapper)
        {
            _photoRepository = photoRepository;
            _faceRepository = faceRepository;
            _mapper = mapper;
            _persons = new Lazy<Task<List<PersonDto>>>(() => personRepository.GetAll().OrderBy(p => p.Name).ProjectTo<PersonDto>(_mapper.ConfigurationProvider).ToListAsync());
            _storages = new Lazy<Task<List<StorageDto>>>(() => storageRepository.GetAll().OrderBy(p => p.Name).ProjectTo<StorageDto>(_mapper.ConfigurationProvider).ToListAsync());
            _tags = new Lazy<Task<List<TagDto>>>(() => tagRepository.GetAll().OrderBy(p => p.Name).ProjectTo<TagDto>(_mapper.ConfigurationProvider).ToListAsync());
            _paths = new Lazy<Task<List<PathDto>>>(() => photoRepository.GetAll()
                .ProjectTo<PathDto>(_mapper.ConfigurationProvider).Distinct().OrderBy(p=>p.Path).ToListAsync());
        }

        public async Task<QueryResult> GetAllPhotosAsync(FilterDto filter, int? skip, int? top)
        {
            var queryResult = new QueryResult();

            var photos = _photoRepository
                .GetAll()
                .Include(p => p.PhotoTags)
                .Include(p => p.Faces)
                .AsQueryable();

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

            if (filter.TakenDateFrom.HasValue)
            {
                photos = photos.Where(p => p.TakenDate.HasValue && p.TakenDate >= filter.TakenDateFrom.Value);
            }

            if (filter.TakenDateTo.HasValue)
            {
                photos = photos.Where(p => p.TakenDate.HasValue && p.TakenDate <= filter.TakenDateTo.Value);
            }

            if (filter.Storages != null && filter.Storages.Any())
            {
                photos = photos
                    .Include(p => p.Storage)
                    .Where(p => filter.Storages.ToList().Contains(p.Storage.Id));

                if (!string.IsNullOrEmpty(filter.RelativePath))
                {
                    photos = photos.Where(p => p.RelativePath == filter.RelativePath);
                }
            }

            if (!string.IsNullOrEmpty(filter.Caption))
            {
                photos = photos
                    .Include(p => p.Captions)
                    .Where(p => p.Captions.Any(c => EF.Functions.FreeText(c.Text, filter.Caption)));
            }

            if (filter.Persons != null && filter.Persons.Any())
            {
                var list = filter.Persons.ToList<int>();

                photos = photos
                    .Where(p => p.Faces != null)
                    .Where(p => p.Faces.Any())
                    .Where(p => p.Faces
                        .Select(f => f.PersonId.Value)
                        .Any(x => list.Contains(x)));
            }

            if (filter.Tags != null && filter.Tags.Any())
            {
                var list = filter.Tags.ToList<int>();

                photos = photos
                    .Where(p => p.PhotoTags != null)
                    .Where(p => p.PhotoTags.Any())
                    .Where(p => p.PhotoTags
                        .Select(f => f.Tag.Id)
                        .Any(x => list.Contains(x)));
            }

            if ((filter.Tags == null || filter.Tags.Count() == 1) && (filter.Persons == null || filter.Persons.Count() == 1))
            {
                queryResult.Count = await photos.CountAsync();
                queryResult.Photos = await photos
                    .OrderBy(p => p.Id)
                    .Skip(skip.Value)
                    .Take(top.Value)
                    .ProjectTo<PhotoItemDto>(_mapper.ConfigurationProvider)
                    .ToListAsync();
                return queryResult;
            }

            var result = await photos.ProjectTo<PhotoItemDto>(_mapper.ConfigurationProvider).ToListAsync();

            if (filter.Tags != null && filter.Tags.Count() > 1)
            {
                var list = filter.Tags.ToList<int>();
                result =
                    (from photo in result
                    where
                    (
                        from requiredId in list
                        where
                        (
                            from tag in photo.Tags
                            where tag.TagId == requiredId
                            select tag
                        ).Any() == false
                        select requiredId
                    ).Any() == false
                    select photo).ToList();
            }

            if (filter.Persons != null && filter.Persons.Count() > 1)
            {
                var list = filter.Persons.ToList<int>();
                result =
                    (from photo in result
                        where
                        (
                            from requiredId in list
                            where
                            (
                                from person in photo.Persons
                                where person.PersonId == requiredId
                                select person
                            ).Any() == false
                            select requiredId
                        ).Any() == false
                        select photo).ToList();
            }

            queryResult.Photos = result.Skip(skip.Value).Take(top.Value);
            queryResult.Count = result.Count;
            return queryResult;
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

        public async Task<IEnumerable<PersonDto>> GetAllPersonsAsync()
        {
            return await _persons.Value;
        }

        public async Task<IEnumerable<PathDto>> GetAllPathsAsync()
        {
            return await _paths.Value;
        }

        public async Task<IEnumerable<StorageDto>> GetAllStoragesAsync()
        {
            return await _storages.Value;
        }

        public async Task<IEnumerable<TagDto>> GetAllTagsAsync()
        {
            return await _tags.Value;
        }

        public async Task UpdateFaceAsync(int faceId, int personId)
        {
            var face = new Face
            {
                Id = faceId,
                IdentifiedWithConfidence = personId == -1 ? 0 : 1,
                IdentityStatus = personId == -1 ? IdentityStatus.StopProcessing : IdentityStatus.Identified,
                PersonId = personId == -1 ? (int?)null : personId
            };
            await _faceRepository.UpdateAsync(face, f => f.PersonId, f => f.IdentifiedWithConfidence, f => f.IdentityStatus);
        }
    }
}