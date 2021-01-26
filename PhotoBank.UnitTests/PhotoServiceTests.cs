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
    public class PhotoServiceTests
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
    }
}