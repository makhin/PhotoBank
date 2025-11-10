using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Internal;
using PhotoBank.Services.Search;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Services.Photos.Faces;

public interface IFaceCatalogService
{
    Task<PageResponse<FaceDto>> GetFacesPageAsync(int page, int pageSize);
    Task<IEnumerable<FaceDto>> GetAllFacesAsync();
    Task UpdateFaceAsync(int faceId, int? personId);
}

public class FaceCatalogService : IFaceCatalogService
{
    private readonly IRepository<Face> _faceRepository;
    private readonly IMapper _mapper;
    private readonly IMediaUrlResolver _mediaUrlResolver;
    private readonly ISearchReferenceDataService _searchReferenceDataService;
    private readonly S3Options _s3;

    public FaceCatalogService(
        IRepository<Face> faceRepository,
        IMapper mapper,
        IMediaUrlResolver mediaUrlResolver,
        ISearchReferenceDataService searchReferenceDataService,
        IOptions<S3Options> s3Options)
    {
        _faceRepository = faceRepository;
        _mapper = mapper;
        _mediaUrlResolver = mediaUrlResolver;
        _searchReferenceDataService = searchReferenceDataService;
        _s3 = s3Options?.Value ?? new S3Options();
    }

    public async Task<PageResponse<FaceDto>> GetFacesPageAsync(int page, int pageSize)
    {
        var boundedPage = Math.Max(1, page);
        var boundedPageSize = Math.Max(1, pageSize);

        var query = _faceRepository.GetAll()
            .AsNoTracking();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(f => f.Id)
            .Skip((boundedPage - 1) * boundedPageSize)
            .Take(boundedPageSize)
            .ProjectTo<FaceDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        await FillUrlsAsync(items);

        return new PageResponse<FaceDto>
        {
            TotalCount = totalCount,
            Items = items,
        };
    }

    public async Task<IEnumerable<FaceDto>> GetAllFacesAsync()
    {
        var faces = await _faceRepository.GetAll()
            .OrderBy(f => f.Id)
            .ProjectTo<FaceDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        await FillUrlsAsync(faces);

        return faces;
    }

    public async Task UpdateFaceAsync(int faceId, int? personId)
    {
        var face = new Face
        {
            Id = faceId,
            IdentifiedWithConfidence = personId == -1 ? 0 : 1,
            IdentityStatus = personId == -1 ? IdentityStatus.StopProcessing : IdentityStatus.Identified,
            PersonId = personId == -1 ? null : personId
        };
        await _faceRepository.UpdateAsync(face, f => f.PersonId, f => f.IdentifiedWithConfidence, f => f.IdentityStatus);

        // Invalidate persons cache because non-admin users' person lists depend on face assignments
        _searchReferenceDataService.InvalidatePersons();
    }

    private async Task FillUrlsAsync(IEnumerable<FaceDto> faces)
    {
        var tasks = faces.Select(async dto =>
        {
            dto.ImageUrl = await _mediaUrlResolver.ResolveAsync(
                dto.S3Key_Image,
                _s3.UrlExpirySeconds,
                MediaUrlContext.ForFace(dto.PhotoId, dto.Id));
        });
        await Task.WhenAll(tasks);
    }
}
