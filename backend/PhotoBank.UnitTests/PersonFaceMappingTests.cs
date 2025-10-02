using System;
using AutoMapper;
using FluentAssertions;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Services;
using PhotoBank.ViewModel.Dto;
using Microsoft.Extensions.DependencyInjection;

namespace PhotoBank.UnitTests;

[TestFixture]
public class PersonFaceMappingTests
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
    public void MapsIds()
    {
        var entity = new Face
        {
            Id = 3,
            PersonId = 2,
            Provider = "azure",
            ExternalId = "ext",
            ExternalGuid = Guid.NewGuid()
        };

        var dto = _mapper.Map<PersonFaceDto>(entity);

        dto.Id.Should().Be(entity.Id);
        dto.FaceId.Should().Be(entity.Id);
        dto.PersonId.Should().Be(entity.PersonId!.Value);
        dto.Provider.Should().Be(entity.Provider);
        dto.ExternalId.Should().Be(entity.ExternalId);
        dto.ExternalGuid.Should().Be(entity.ExternalGuid);
    }
}
