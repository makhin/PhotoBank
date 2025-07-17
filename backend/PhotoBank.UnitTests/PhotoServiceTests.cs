using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PhotoBank.Services;

namespace PhotoBank.UnitTests
{
    [TestFixture]
    public class PhotoServiceTests
    {
        private IMapper _mapper;

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile(new MappingProfile());
            });

            var provider = services.BuildServiceProvider();
            _mapper = provider.GetRequiredService<IMapper>();
        }

        [Test]
        public void AutoMapper_Configuration_IsValid()
        {
            _mapper.ConfigurationProvider.AssertConfigurationIsValid();
        }
    }
}