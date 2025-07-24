using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Services.Api
{
    public interface IPhotoService
    {
        Task<QueryResult> GetAllPhotosAsync(FilterDto filter);
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

        public async Task<QueryResult> GetAllPhotosAsync(FilterDto filter)
        {
            var query = _photoRepository
                .GetAll()
                .AsNoTrackingWithIdentityResolution()
                .AsSplitQuery();

            if (filter.IsBW is true)
            {
                query = query.Where(p => p.IsBW);
            }

            if (filter.IsAdultContent is true)
            {
                query = query.Where(p => p.IsAdultContent);
            }

            if (filter.IsRacyContent is true)
            {
                query = query.Where(p => p.IsRacyContent);
            }

            if (filter.TakenDateFrom.HasValue)
            {
                query = query.Where(p => p.TakenDate.HasValue && p.TakenDate >= filter.TakenDateFrom.Value);
            }

            if (filter.TakenDateTo.HasValue)
            {
                query = query.Where(p => p.TakenDate.HasValue && p.TakenDate <= filter.TakenDateTo.Value);
            }

            if (filter.ThisDay is true)
            {
                query = query.Where(p =>
                    p.TakenDate.HasValue && p.TakenDate.Value.Day == DateTime.Today.Day &&
                    p.TakenDate.Value.Month == DateTime.Today.Month);
            }

            if (filter.Storages != null && filter.Storages.Any())
            {
                var storages = filter.Storages.ToList();
                query = query.Where(p => storages.Contains(p.StorageId));

                if (!string.IsNullOrEmpty(filter.RelativePath))
                {
                    query = query.Where(p => p.RelativePath == filter.RelativePath);
                }
            }

            if (!string.IsNullOrEmpty(filter.Caption))
            {
                query = query.Where(p => p.Captions.Any(c => EF.Functions.FreeText(c.Text, filter.Caption!)));
            }

            if (filter.Persons != null && filter.Persons.Any())
            {
                var ids = filter.Persons.ToList();
                query = query.Where(p => ids.All(id => p.Faces.Any(f => f.PersonId == id)));
            }

            if (filter.Tags != null && filter.Tags.Any())
            {
                var ids = filter.Tags.ToList();
                query = query.Where(p => ids.All(id => p.PhotoTags.Any(t => t.TagId == id)));
            }

            var result = new QueryResult
            {
                Count = await query.CountAsync(),
                Photos = await query
                    .OrderByDescending(p => p.TakenDate)
                    .Skip(filter.Skip ?? 0)
                    .Take(filter.Top ?? int.MaxValue)
                    .ProjectTo<PhotoItemDto>(_mapper.ConfigurationProvider)
                    .ToListAsync()
            };

            return result;
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