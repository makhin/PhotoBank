using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using ImageMagick;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoBank.DbContext.Models;
using PhotoBank.Services;
using PhotoBank.Repositories;
using PhotoBank.Services.Internal;
using PhotoBank.Services.Models;
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
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PhotoDuplicateFinder(
        IRepository<Photo> photoRepository,
        IMapper mapper,
        IOptions<S3Options> s3Options,
        IMediaUrlResolver mediaUrlResolver,
        IHttpContextAccessor httpContextAccessor)
    {
        _photoRepository = photoRepository;
        _mapper = mapper;
        _s3Options = s3Options?.Value ?? new S3Options();
        _mediaUrlResolver = mediaUrlResolver;
        _httpContextAccessor = httpContextAccessor;
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
            .ToListAsync(cancellationToken);

        var items = _mapper.Map<List<PhotoItemDto>>(entities);
        await FillUrlsAsync(items, cancellationToken);

        return items;
    }

    private async Task FillUrlsAsync(IEnumerable<PhotoItemDto> items, CancellationToken cancellationToken)
    {
        var requestHost = GetRequestHost();
        var tasks = items.Select(async dto =>
        {
            dto.ThumbnailUrl = await _mediaUrlResolver.ResolveAsync(
                dto.S3Key_Thumbnail,
                _s3Options.UrlExpirySeconds,
                MediaUrlContext.ForPhoto(dto.Id),
                requestHost,
                cancellationToken);
        });

        await Task.WhenAll(tasks);
    }

    private string? GetRequestHost()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return null;
        }

        var host = httpContext.Request.Host;
        return host.HasValue ? host.Value : null;
    }
}
