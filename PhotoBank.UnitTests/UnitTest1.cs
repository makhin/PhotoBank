using AutoMapper;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;
using PhotoBank.Repositories;
using PhotoBank.Services.Api;

namespace PhotoBank.UnitTests
{
    [TestFixture]
    public class Tests
    {
        private Mapper _mapper; 

        [SetUp]
        public void Setup()
        {
            var mappingProfile = new MappingProfile();
            var config = new MapperConfiguration(c => c.AddProfile(mappingProfile));
            _mapper = new Mapper(config);

        }

        [Test]
        public void AutoMapper_Configuration_IsValid()
        {
            _mapper.ConfigurationProvider.AssertConfigurationIsValid();
        }

        [Test]
        public void Test1()
        {
            Mock<IRepository<Photo>> repoMock = new Mock<IRepository<Photo>>();
            Mock<IMapper> mapperMock = new Mock<IMapper>();
            PhotoService photoService = new PhotoService(repoMock.Object, mapperMock.Object);
            var result = photoService.GetAllAsync();
            repoMock.Verify(r => r.GetAll(), Times.Once);
        }
    }
}