using System;
using AutoMapper;
using FluentAssertions;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Services;
using PhotoBank.ViewModel.Dto;
using Microsoft.Extensions.DependencyInjection;
using PhotoBank.Services.Models;

namespace PhotoBank.UnitTests;

[TestFixture]
public class FaceDtoMappingTests
{
    private IMapper _mapper = null!;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
        var provider = services.BuildServiceProvider();
        _mapper = provider.GetRequiredService<IMapper>();
    }

    [Test]
    public void MapsMetadata()
    {
        var externalGuid = Guid.NewGuid();
        var entity = new Face
        {
            Id = 3,
            PersonId = 2,
            Provider = "azure",
            ExternalId = "ext",
            ExternalGuid = externalGuid,
            PhotoId = 8,
            IdentifiedWithConfidence = 0.8,
            IdentityStatus = IdentityStatus.Identified
        };

        var dto = _mapper.Map<FaceDto>(entity);

        dto.Id.Should().Be(entity.Id);
        dto.PersonId.Should().Be(entity.PersonId);
        dto.Provider.Should().Be(entity.Provider);
        dto.PhotoId.Should().Be(entity.PhotoId);
        dto.IdentifiedWithConfidence.Should().Be(entity.IdentifiedWithConfidence);
        dto.IdentityStatus.Should().Be(IdentityStatusDto.Identified);
    }
}
