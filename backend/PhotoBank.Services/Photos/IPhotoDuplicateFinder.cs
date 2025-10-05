using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using ImageMagick;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
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
    private readonly IMinioClient _minioClient;
    private readonly S3Options _s3Options;
    private readonly ILogger<PhotoDuplicateFinder> _logger;

    public PhotoDuplicateFinder(
        IRepository<Photo> photoRepository,
        IMapper mapper,
        IMinioClient minioClient,
        IOptions<S3Options> s3Options,
        ILogger<PhotoDuplicateFinder> logger)
    {
        _photoRepository = photoRepository;
        _mapper = mapper;
        _minioClient = minioClient;
        _s3Options = s3Options?.Value ?? new S3Options();
        _logger = logger;
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
        var tasks = items.Select(async dto =>
        {
            dto.ThumbnailUrl = await GetPresignedUrlAsync(dto.S3Key_Thumbnail, dto.Id, cancellationToken);
        });

        await Task.WhenAll(tasks);
    }

    private async Task<string?> GetPresignedUrlAsync(string? key, int? photoId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        try
        {
            return await _minioClient.PresignedGetObjectAsync(new PresignedGetObjectArgs()
                .WithBucket(_s3Options.Bucket)
                .WithObject(key)
                .WithExpiry(_s3Options.UrlExpirySeconds));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate presigned URL for photo {PhotoId} with key {S3Key}.", photoId, key);
            return null;
        }
    }
}
