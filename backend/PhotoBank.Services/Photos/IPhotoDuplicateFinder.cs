using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using ImageMagick;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoBank.AccessControl;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Internal;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Services.Photos;

public interface IPhotoDuplicateFinder
{
    Task<IEnumerable<PhotoItemDto>> FindDuplicatesAsync(int? id, string? hash, int threshold, CancellationToken cancellationToken = default);
}

public sealed class PhotoDuplicateFinder : IPhotoDuplicateFinder
{
    private readonly IRepository<Photo> _photoRepository;
    private readonly IMapper _mapper;
    private readonly S3Options _s3Options;
    private readonly IMediaUrlResolver _mediaUrlResolver;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public PhotoDuplicateFinder(
        IRepository<Photo> photoRepository,
        IMapper mapper,
        IOptions<S3Options> s3Options,
        IMediaUrlResolver mediaUrlResolver,
        ICurrentUserAccessor currentUserAccessor)
    {
        _photoRepository = photoRepository;
        _mapper = mapper;
        _s3Options = s3Options?.Value ?? new S3Options();
        _mediaUrlResolver = mediaUrlResolver;
        _currentUserAccessor = currentUserAccessor;
    }

    public async Task<IEnumerable<PhotoItemDto>> FindDuplicatesAsync(int? id, string? hash, int threshold, CancellationToken cancellationToken = default)
    {
        if (id.HasValue)
        {
            hash = await _photoRepository.GetByCondition(p => p.Id == id.Value)
                .AsNoTracking()
                .Select(p => p.ImageHash)
                .SingleOrDefaultAsync(cancellationToken);
        }

        if (string.IsNullOrEmpty(hash))
        {
            return Array.Empty<PhotoItemDto>();
        }

        var referenceHash = new PerceptualHash(hash);
        var candidateIds = new List<int>();

        await foreach (var photo in _photoRepository.GetAll().AsNoTracking()
                           .Where(p => !id.HasValue || p.Id != id.Value)
                           .Select(p => new { p.Id, p.ImageHash })
                           .AsAsyncEnumerable())
        {
            if (ImageHashHelper.HammingDistance(referenceHash, photo.ImageHash) <= threshold)
            {
                candidateIds.Add(photo.Id);
            }
        }

        var matchedIds = candidateIds.Distinct().ToArray();
        if (matchedIds.Length == 0)
        {
            return Array.Empty<PhotoItemDto>();
        }

        var entities = await _photoRepository.GetAll().AsNoTracking()
            .Where(p => matchedIds.Contains(p.Id))
            .Include(p => p.Files)!.ThenInclude(f => f.Storage)
            .Include(p => p.PhotoTags)
            .Include(p => p.Faces)
            .Include(p => p.Captions)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var currentUser = await _currentUserAccessor.GetCurrentUserAsync(cancellationToken);

        var items = _mapper.Map<List<PhotoItemDto>>(entities, opts =>
        {
            if (!currentUser.IsAdmin)
            {
                opts.Items["AllowedStorageIds"] = currentUser.AllowedStorageIds;
            }
        });
        await FillUrlsAsync(items, cancellationToken);

        return items;
    }

    private async Task FillUrlsAsync(IEnumerable<PhotoItemDto> items, CancellationToken cancellationToken)
    {
        var tasks = items.Select(async dto =>
        {
            dto.ThumbnailUrl = await _mediaUrlResolver.ResolveAsync(
                dto.S3Key_Thumbnail,
                _s3Options.UrlExpirySeconds,
                MediaUrlContext.ForPhoto(dto.Id),
                cancellationToken);
        });

        await Task.WhenAll(tasks);
    }
}
