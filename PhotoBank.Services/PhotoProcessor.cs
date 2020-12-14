using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Extensions.Configuration;
using Microsoft.Rest;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;
using PhotoBank.Repositories;
using PhotoBank.Services.Enrichers;

namespace PhotoBank.Services
{
    public interface IPhotoProcessor
    {
        bool Contains(Storage storage, string path);
        bool AddPhoto(Storage storage, string path);
        Task<bool> AddPhotoAsync(Storage storage, string path);
    }

    public class PhotoProcessor : IPhotoProcessor
    {
        private readonly IComputerVisionClient _client;
        private readonly IRecognitionService _faceClient;
        private readonly IConfiguration _configuration;
        private readonly IRepository<Photo> _photoRepository;
        private readonly IImageEncoder _imageEncoder;
        private readonly IEnumerable<IEnricher> _enrichers;

        private readonly IList<VisualFeatureTypes?> _features = new List<VisualFeatureTypes?>()
        {
            VisualFeatureTypes.Categories, VisualFeatureTypes.Description,
            VisualFeatureTypes.Faces, VisualFeatureTypes.ImageType,
            VisualFeatureTypes.Tags, VisualFeatureTypes.Adult,
            VisualFeatureTypes.Color, VisualFeatureTypes.Brands,
            VisualFeatureTypes.Objects
        };

        public PhotoProcessor(
            IComputerVisionClient client,
            IRecognitionService faceClient,
            IConfiguration configuration,
            IRepository<Photo> photoRepository,
            IEnumerable<IEnricher> enrichers,
            IOrderResolver<IEnricher> orderResolver,
            IImageEncoder imageEncoder
            )
        {
            _client = client;
            _faceClient = faceClient;
            _configuration = configuration;
            _photoRepository = photoRepository;
            _imageEncoder = imageEncoder;
            _enrichers = orderResolver.Resolve(enrichers);
        }

        public bool Contains(Storage storage, string path)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            var relativePath = Path.GetRelativePath(storage.Folder, Path.GetDirectoryName(path));
            return _photoRepository.GetByCondition(p => p.Name == name && p.Path == relativePath && p.Storage.Id == storage.Id).Any();
        }

        public bool AddPhoto(Storage storage, string path)
        {
            if (!File.Exists(path))
            {
                throw new ArgumentException("File does not exists", nameof(path));
            }

            var image = _imageEncoder.Prepare(path);
            var analysis = _client.AnalyzeImageInStreamAsync(new MemoryStream(image), _features).Result;

            var photo = new Photo()
            {
                Storage = storage
            };

            var sourceData = new SourceDataDto
            {
                Path = Path.GetRelativePath(storage.Folder, path),
                Image = image,
                ImageAnalysis = analysis
            };

            foreach (var enricher in _enrichers)
            {
                enricher.Enrich(photo, sourceData);
            }

            try
            {
                _photoRepository.InsertAsync(photo).Wait();
            }
            catch (Exception exception)
            {
                Console.WriteLine("An exception occurred: {0}, {1}", exception.InnerException, exception.Message);
            }
            
            return true;
        }

        Task<bool> IPhotoProcessor.AddPhotoAsync(Storage storage, string path)
        {
            throw new NotImplementedException();
        }
    }
}
