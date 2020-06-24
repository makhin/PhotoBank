using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Extensions.Configuration;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;

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
        private readonly IEnumerable<IEnricher<string>> _stringEnrichers;
        private readonly IEnumerable<IEnricher<ImageAnalysis>> _analysisEnrichers;

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
            IEnumerable<IEnricher<string>> stringEnrichers,
            IEnumerable<IEnricher<ImageAnalysis>> analysisEnrichers
            )
        {
            _client = client;
            _configuration = configuration;
            _photoRepository = photoRepository;
            _stringEnrichers = stringEnrichers;
            _analysisEnrichers = analysisEnrichers;
        }

        public bool AddPhoto(string path)
        {
            if (!File.Exists(path))
            {
                throw new ArgumentException("File does not exists", nameof(path));
            }

            var photo = new Photo
            {
                Name = Path.GetFileNameWithoutExtension(path),
                Path = Path.GetDirectoryName(path),
            };

            foreach (var enricher in _stringEnrichers)
            {
                enricher.Enrich(photo, path);
            }

            ImageAnalysis analysis;

            try
            {
                var stream = new MemoryStream(photo.PreviewImage);
                analysis = _client.AnalyzeImageInStreamAsync(stream, _features).Result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            foreach (var enricher in _analysisEnrichers)
            {
                enricher.Enrich(photo, analysis);
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
