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
    public void MapsFaceImage()
    {
        var entity = new PersonFace
        {
            Id = 1,
            PersonId = 2,
            FaceId = 3,
            Face = new Face { Image = new byte[] { 1, 2, 3 } }
        };

        var dto = _mapper.Map<PersonFaceDto>(entity);

        dto.FaceImage.Should().BeEquivalentTo(new byte[] { 1, 2, 3 });
    }
}
