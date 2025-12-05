using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoBank.AccessControl;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Internal;
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
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly ISearchReferenceDataService _searchReferenceDataService;
    private readonly ISearchFilterNormalizer _searchFilterNormalizer;
    private readonly PhotoFilterSpecification _photoFilterSpecification;
    private readonly IMediaUrlResolver _mediaUrlResolver;
    private readonly S3Options _s3;
    private ICurrentUser? _currentUser;

    public PhotoQueryService(
        PhotoBankDbContext db,
        IRepository<Photo> photoRepository,
        IMapper mapper,
        ICurrentUserAccessor currentUserAccessor,
        ISearchReferenceDataService searchReferenceDataService,
        ISearchFilterNormalizer searchFilterNormalizer,
        PhotoFilterSpecification photoFilterSpecification,
        IMediaUrlResolver mediaUrlResolver,
        IOptions<S3Options> s3Options)
    {
        _db = db;
        _photoRepository = photoRepository;
        _mapper = mapper;
        _currentUserAccessor = currentUserAccessor;
        _searchReferenceDataService = searchReferenceDataService;
        _searchFilterNormalizer = searchFilterNormalizer;
        _photoFilterSpecification = photoFilterSpecification;
        _mediaUrlResolver = mediaUrlResolver;
        _s3 = s3Options?.Value ?? new S3Options();
    }

    public async Task<PageResponse<PhotoItemDto>> GetAllPhotosAsync(FilterDto filter, CancellationToken ct = default)
    {
        filter = await _searchFilterNormalizer.NormalizeAsync(filter, ct);
        var currentUser = await GetCurrentUserAsync(ct).ConfigureAwait(false);

        var query = _photoFilterSpecification.Build(filter, currentUser);

        var count = await query.CountAsync(ct);

        var pageSize = Math.Min(filter.PageSize, PageRequest.MaxPageSize);
        var skip = (filter.Page - 1) * pageSize;

        var photos = await query
            .OrderByDescending(p => p.TakenDate)
            .ThenByDescending(p => p.Id)
            .Skip(skip)
            .Take(pageSize)
            .Include(p => p.Files)!.ThenInclude(f => f.Storage)
            .Include(p => p.PhotoTags)
            .Include(p => p.Faces)
            .Include(p => p.Captions)
            .AsSplitQuery()
            .ToListAsync(ct);

        var photoDtos = _mapper.Map<List<PhotoItemDto>>(photos, opts =>
        {
            if (!currentUser.IsAdmin)
            {
                opts.Items["AllowedStorageIds"] = currentUser.AllowedStorageIds;
            }
        });

        await FillUrlsAsync(photoDtos);

        return new PageResponse<PhotoItemDto>
        {
            TotalCount = count,
            Items = photoDtos
        };
    }

    public async Task<PhotoDto> GetPhotoAsync(int id)
    {
        var currentUser = await GetCurrentUserAsync().ConfigureAwait(false);
        PhotoDto? dto;
        if (currentUser.IsAdmin)
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
            var acl = Acl.FromUser(currentUser);
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

    private async Task<ICurrentUser> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        if (_currentUser is not null)
        {
            return _currentUser;
        }

        var resolved = await _currentUserAccessor.GetCurrentUserAsync(cancellationToken).ConfigureAwait(false);
        _currentUser = resolved;
        return resolved;
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
