using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Extensions.Configuration;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;
using PhotoBank.Repositories;
using PhotoBank.Services.Enrichers;

namespace PhotoBank.Services
{
    public interface IPhotoProcessor
    {
        bool AddPhoto(string path);
        Task<bool> AddPhotoAsync(string path);
    }

    public class PhotoProcessor : IPhotoProcessor
    {
        private readonly IComputerVisionClient _client;
        private readonly IConfiguration _configuration;
        private readonly IRepository<Photo> _photoRepository;
        private readonly ImageEncoder _imageEncoder;
        private readonly IEnumerable<IEnricher> _enrichers;

        private readonly List<VisualFeatureTypes> _features = new List<VisualFeatureTypes>()
        {
            VisualFeatureTypes.Categories, VisualFeatureTypes.Description,
            VisualFeatureTypes.Faces, VisualFeatureTypes.ImageType,
            VisualFeatureTypes.Tags, VisualFeatureTypes.Adult,
            VisualFeatureTypes.Color, VisualFeatureTypes.Brands,
            VisualFeatureTypes.Objects
        };

        public PhotoProcessor(
            IComputerVisionClient client,
            IConfiguration configuration,
            IRepository<Photo> photoRepository,
            IEnumerable<IEnricher> enrichers,
            IOrderResolver<IEnricher> orderResolver,
            ImageEncoder imageEncoder
            )
        {
            _client = client;
            _configuration = configuration;
            _photoRepository = photoRepository;
            _imageEncoder = imageEncoder;
            _enrichers = orderResolver.Resolve(enrichers);
        }

        public bool AddPhoto(string path)
        {
            if (!File.Exists(path))
            {
                throw new ArgumentException("File does not exists", nameof(path));
            }

            var image = Image.FromFile(path);
            var stream = _imageEncoder.Encode(image, @"image/jpeg", 60L);
            stream.Position = 0;
            var analysis = _client.AnalyzeImageInStreamAsync(stream, _features).Result;

            var sourceData = new SourceDataDto
            {
                Path = path,
                Image = image,
                ImageAnalysis = analysis
            };

            var photo = new Photo();

            foreach (var enricher in _enrichers)
            {
                enricher.Enrich(photo, sourceData);
            }

            try
            {
                _photoRepository.Insert(photo).Wait();
            }
            catch (Exception exception)
            {
                Console.WriteLine("An exception occurred: {0}, {1}", exception.InnerException, exception.Message);
            }
            
            return true;
        }

        Task<bool> IPhotoProcessor.AddPhotoAsync(string path)
        {
            throw new NotImplementedException();
        }
    }
}
