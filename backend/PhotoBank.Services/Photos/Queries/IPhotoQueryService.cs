using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using ImageMagick;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoBank.AccessControl;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Internal;
using PhotoBank.Services.Models;
using PhotoBank.Services.Search;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Services.Photos.Queries;

public interface IPhotoQueryService
{
    Task<PageResponse<PhotoItemDto>> GetAllPhotosAsync(FilterDto filter, CancellationToken ct = default);
    Task<PhotoDto> GetPhotoAsync(int id);
    Task<IEnumerable<PathDto>> GetAllPathsAsync();
    Task<IEnumerable<StorageDto>> GetAllStoragesAsync();
    Task<IEnumerable<TagDto>> GetAllTagsAsync();
}

public class PhotoQueryService : IPhotoQueryService
{
    private readonly PhotoBankDbContext _db;
    private readonly IRepository<Photo> _photoRepository;
    private readonly IMapper _mapper;
    private readonly ICurrentUser _currentUser;
    private readonly ISearchReferenceDataService _searchReferenceDataService;
    private readonly ISearchFilterNormalizer _searchFilterNormalizer;
    private readonly PhotoFilterSpecification _photoFilterSpecification;
    private readonly IMediaUrlResolver _mediaUrlResolver;
    private readonly S3Options _s3;

    public PhotoQueryService(
        PhotoBankDbContext db,
        IRepository<Photo> photoRepository,
        IMapper mapper,
        ICurrentUser currentUser,
        ISearchReferenceDataService searchReferenceDataService,
        ISearchFilterNormalizer searchFilterNormalizer,
        PhotoFilterSpecification photoFilterSpecification,
        IMediaUrlResolver mediaUrlResolver,
        IOptions<S3Options> s3Options)
    {
        _db = db;
        _photoRepository = photoRepository;
        _mapper = mapper;
        _currentUser = currentUser;
        _searchReferenceDataService = searchReferenceDataService;
        _searchFilterNormalizer = searchFilterNormalizer;
        _photoFilterSpecification = photoFilterSpecification;
        _mediaUrlResolver = mediaUrlResolver;
        _s3 = s3Options?.Value ?? new S3Options();
    }

    public async Task<PageResponse<PhotoItemDto>> GetAllPhotosAsync(FilterDto filter, CancellationToken ct = default)
    {
        filter = await _searchFilterNormalizer.NormalizeAsync(filter, ct);

        var query = _photoFilterSpecification.Build(filter, _currentUser);

        var count = await query.CountAsync(ct);

        var pageSize = Math.Min(filter.PageSize, PageRequest.MaxPageSize);
        var skip = (filter.Page - 1) * pageSize;

        var photos = await query
            .OrderByDescending(p => p.TakenDate)
            .ThenByDescending(p => p.Id)
            .Skip(skip)
            .Take(pageSize)
            .ProjectTo<PhotoItemDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);

        await FillUrlsAsync(photos);

        return new PageResponse<PhotoItemDto>
        {
            TotalCount = count,
            Items = photos
        };
    }

    public async Task<PhotoDto> GetPhotoAsync(int id)
    {
        PhotoDto? dto;
        if (_currentUser.IsAdmin)
        {
            var adminQuery = _db.Photos
                .Include(p => p.PhotoTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.Faces).ThenInclude(f => f.Person).ThenInclude(pg => pg.PersonGroups)
                .Include(p => p.Captions)
                .AsSplitQuery();

            var adminEntity = await adminQuery.FirstOrDefaultAsync(p => p.Id == id);
            dto = adminEntity is null ? null : _mapper.Map<Photo, PhotoDto>(adminEntity);
        }
        else
        {
            var acl = Acl.FromUser(_currentUser);
            var nonAdminQuery = _db.Photos
                .Include(p => p.PhotoTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.Faces).ThenInclude(f => f.Person).ThenInclude(pg => pg.PersonGroups)
                .Include(p => p.Captions)
                .AsSplitQuery()
                .Where(p => p.Id == id)
                .Where(AclPredicates.PhotoWhere(acl));

            var entity = await nonAdminQuery.FirstOrDefaultAsync();
            dto = entity is null ? null : _mapper.Map<Photo, PhotoDto>(entity);
        }

        if (dto != null)
        {
            await FillUrlsAsync(dto);
        }

        return dto!;
    }

    public async Task<IEnumerable<PathDto>> GetAllPathsAsync()
    {
        var paths = await _searchReferenceDataService.GetPathsAsync();
        return paths;
    }

    public async Task<IEnumerable<StorageDto>> GetAllStoragesAsync()
    {
        var storages = await _searchReferenceDataService.GetStoragesAsync();
        return storages;
    }

    public async Task<IEnumerable<TagDto>> GetAllTagsAsync()
    {
        var tags = await _searchReferenceDataService.GetTagsAsync();
        return tags;
    }

    private async Task FillUrlsAsync(PhotoDto dto)
    {
        dto.PreviewUrl = await _mediaUrlResolver.ResolveAsync(
            dto.S3Key_Preview,
            _s3.UrlExpirySeconds,
            MediaUrlContext.ForPhoto(dto.Id));
    }

    private async Task FillUrlsAsync(IEnumerable<PhotoItemDto> items)
    {
        var tasks = items.Select(async dto =>
        {
            dto.ThumbnailUrl = await _mediaUrlResolver.ResolveAsync(
                dto.S3Key_Thumbnail,
                _s3.UrlExpirySeconds,
                MediaUrlContext.ForPhoto(dto.Id));
        });
        await Task.WhenAll(tasks);
    }
}
