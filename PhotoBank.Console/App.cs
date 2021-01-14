using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using FaceRecognitionDotNet;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using NetTopologySuite.IO;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;
using PhotoBank.Repositories;
using PhotoBank.Services;
using PhotoBank.Services.Api;
using Person = PhotoBank.DbContext.Models.Person;

namespace PhotoBank.Console
{
    using System;

    public class App
    {
        private readonly IPhotoProcessor _photoProcessor;
        private readonly IRepository<Storage> _repository;
        private readonly IRepository<Face> _faceRepository;
        private readonly IFaceService _faceService;
        private readonly ILogger<App> _logger;
        private readonly ISyncService _syncService;
        private readonly IMapper _mapper;
        private readonly ISimpleRepository _f2FRepository;

        public App(IPhotoProcessor photoProcessor, IRepository<Storage> repository, IRepository<Face> faceRepository, IFaceService faceService, ILogger<App> logger, ISyncService syncService, IMapper mapper, ISimpleRepository f2fRepository)
        {
            _photoProcessor = photoProcessor;
            _repository = repository;
            _faceRepository = faceRepository;
            _faceService = faceService;
            _logger = logger;
            _syncService = syncService;
            _mapper = mapper;
            _f2FRepository = f2fRepository;
        }

        public async Task Run()
        {
            await TestLargeFaceList0("https://photobankface2.cognitiveservices.azure.com/",
                "686650135dc64559980a64c9a7ea51d5");
            //await MakeRequest();
            //await _faceService.AddFacesToList();
            //await _faceService.FindSimilarFacesInList();
        }

        public async Task TestLargeFaceList0(string endpoint, string key)
        {
            Console.WriteLine("Sample of finding similar faces in large face list.");

            IFaceClient client = new FaceClient(new ApiKeyServiceClientCredentials(key)) { Endpoint = endpoint };
            string recognitionModel = RecognitionModel.Recognition02;

            var dbFaces = await _faceRepository.GetAll().Include(f => f.Person).Where(f => f.Person != null).Take(10).ToListAsync();

            // Create a large face list.
            string largeFaceListId = Guid.NewGuid().ToString();
            Console.WriteLine($"Create large face list {largeFaceListId}.");
            await client.LargeFaceList.CreateAsync(
                largeFaceListId,
                "large face list for FindSimilar sample",
                "large face list for FindSimilar sample",
                recognitionModel: recognitionModel);

            foreach (var dbFace in dbFaces)
            {
                var stream = new MemoryStream(dbFace.Image);

                // Add face to the large face list.
                var faces = await client.LargeFaceList.AddFaceFromStreamAsync(
                                largeFaceListId, stream,
                                dbFace.Id.ToString());

                if (faces == null)
                {
                    throw new Exception($"No face detected from image `{dbFace.Id}`.");
                }

                Console.WriteLine(
                    $"Face from image {dbFace.Id} is successfully added to the large face list.");
            }

            // Start to train the large face list.
            Console.WriteLine($"Train large face list {largeFaceListId}.");
            await client.LargeFaceList.TrainAsync(largeFaceListId);

            // Wait until the training is completed.
            while (true)
            {
                await Task.Delay(1000);
                var trainingStatus = await client.LargeFaceList.GetTrainingStatusAsync(largeFaceListId);
                Console.WriteLine($"Training status is {trainingStatus.Status}.");
                if (trainingStatus.Status != TrainingStatusType.Running)
                {
                    if (trainingStatus.Status == TrainingStatusType.Failed)
                    {
                        throw new Exception($"Training failed with message {trainingStatus.Message}.");
                    }

                    break;
                }
            }

            // Get persisted faces from the large face list.
            List<PersistedFace> persistedFaces = (await client.LargeFaceList.ListFacesAsync(largeFaceListId)).ToList();
            if (persistedFaces.Count == 0)
            {
                throw new Exception($"No persisted face in large face list '{largeFaceListId}'.");
            }

            var ff = (await _faceRepository.GetAsync(1692)).Image;


            // Detect faces from source image url.
            IList<DetectedFace> detectedFaces = await DetectFaces(
                                                    client, 
                                                    ff,
                                                    recognitionModel: recognitionModel);

            // Find similar example of faceId to large face list.
            var similarResults = await client.Face.FindSimilarAsync(
                                     detectedFaces[0].FaceId.Value,
                                     null,
                                     largeFaceListId);

            foreach (var similarResult in similarResults)
            {
                PersistedFace persistedFace =
                    persistedFaces.Find(face => face.PersistedFaceId == similarResult.PersistedFaceId);
                if (persistedFace == null)
                {
                    Console.WriteLine("Persisted face not found in similar result.");
                    continue;
                }

                Console.WriteLine(
                    $"Faces from {persistedFace.UserData} are similar with confidence: {similarResult.Confidence}.");
            }

            // Delete the large face list.
            await client.LargeFaceList.DeleteAsync(largeFaceListId);
            Console.WriteLine($"Delete LargeFaceList {largeFaceListId}.");
            Console.WriteLine();
        }
        public static async Task TestLargeFaceList(string endpoint, string key)
        {
            Console.WriteLine("Sample of finding similar faces in large face list.");

            IFaceClient client = new FaceClient(new ApiKeyServiceClientCredentials(key)) { Endpoint = endpoint };
            string recognitionModel = RecognitionModel.Recognition02;

            const string ImageUrlPrefix = "https://csdx.blob.core.windows.net/resources/Face/Images/";
            List<string> targetImageFileNames = new List<string>
                                                    {
                                                        "Family1-Dad1.jpg",
                                                        "Family1-Daughter1.jpg",
                                                        "Family1-Mom1.jpg",
                                                        "Family1-Son1.jpg",
                                                        "Family2-Lady1.jpg",
                                                        "Family2-Man1.jpg",
                                                        "Family3-Lady1.jpg",
                                                        "Family3-Man1.jpg"
                                                    };

            string sourceImageFileName = "findsimilar.jpg";

            // Create a large face list.
            string largeFaceListId = Guid.NewGuid().ToString();
            Console.WriteLine($"Create large face list {largeFaceListId}.");
            await client.LargeFaceList.CreateAsync(
                largeFaceListId,
                "large face list for FindSimilar sample",
                "large face list for FindSimilar sample",
                recognitionModel: recognitionModel);

            foreach (var targetImageFileName in targetImageFileNames)
            {
                // Add face to the large face list.
                var faces = await client.LargeFaceList.AddFaceFromUrlAsync(
                                largeFaceListId,
                                $"{ImageUrlPrefix}{targetImageFileName}",
                                targetImageFileName);
                if (faces == null)
                {
                    throw new Exception($"No face detected from image `{targetImageFileName}`.");
                }

                Console.WriteLine(
                    $"Face from image {targetImageFileName} is successfully added to the large face list.");
            }

            // Start to train the large face list.
            Console.WriteLine($"Train large face list {largeFaceListId}.");
            await client.LargeFaceList.TrainAsync(largeFaceListId);

            // Wait until the training is completed.
            while (true)
            {
                await Task.Delay(1000);
                var trainingStatus = await client.LargeFaceList.GetTrainingStatusAsync(largeFaceListId);
                Console.WriteLine($"Training status is {trainingStatus.Status}.");
                if (trainingStatus.Status != TrainingStatusType.Running)
                {
                    if (trainingStatus.Status == TrainingStatusType.Failed)
                    {
                        throw new Exception($"Training failed with message {trainingStatus.Message}.");
                    }

                    break;
                }
            }

            // Get persisted faces from the large face list.
            List<PersistedFace> persistedFaces = (await client.LargeFaceList.ListFacesAsync(largeFaceListId)).ToList();
            if (persistedFaces.Count == 0)
            {
                throw new Exception($"No persisted face in large face list '{largeFaceListId}'.");
            }

            // Detect faces from source image url.
            IList<DetectedFace> detectedFaces = await DetectFaces(
                                                    client,
                                                    $"{ImageUrlPrefix}{sourceImageFileName}",
                                                    recognitionModel: recognitionModel);

            // Find similar example of faceId to large face list.
            var similarResults = await client.Face.FindSimilarAsync(
                                     detectedFaces[0].FaceId.Value,
                                     null,
                                     largeFaceListId);
            foreach (var similarResult in similarResults)
            {
                PersistedFace persistedFace =
                    persistedFaces.Find(face => face.PersistedFaceId == similarResult.PersistedFaceId);
                if (persistedFace == null)
                {
                    Console.WriteLine("Persisted face not found in similar result.");
                    continue;
                }

                Console.WriteLine(
                    $"Faces from {sourceImageFileName} & {persistedFace.UserData} are similar with confidence: {similarResult.Confidence}.");
            }

            // Delete the large face list.
            await client.LargeFaceList.DeleteAsync(largeFaceListId);
            Console.WriteLine($"Delete LargeFaceList {largeFaceListId}.");
            Console.WriteLine();
        }

        internal static async Task<List<DetectedFace>> DetectFaces(IFaceClient faceClient, byte[] image, string recognitionModel = RecognitionModel.Recognition01)
        {
            var stream = new MemoryStream(image);

            // Detect faces from image stream.
            IList<DetectedFace> detectedFaces = await faceClient.Face.DetectWithStreamAsync(stream, recognitionModel: recognitionModel);
            if (detectedFaces == null || detectedFaces.Count == 0)
            {
                throw new Exception($"No face detected from image ``.");
            }

            Console.WriteLine($"{detectedFaces.Count} faces detected from image ``.");
            if (detectedFaces[0].FaceId == null)
            {
                throw new Exception(
                    "Parameter `returnFaceId` of `DetectWithStreamAsync` must be set to `true` (by default) for recognition purpose.");
            }

            return detectedFaces.ToList();
        }

        internal static async Task<List<DetectedFace>> DetectFaces(IFaceClient faceClient, string imageUrl, string recognitionModel = RecognitionModel.Recognition01)
        {
            // Detect faces from image stream.
            IList<DetectedFace> detectedFaces = await faceClient.Face.DetectWithUrlAsync(imageUrl, recognitionModel: recognitionModel);
            if (detectedFaces == null || detectedFaces.Count == 0)
            {
                throw new Exception($"No face detected from image `{imageUrl}`.");
            }

            Console.WriteLine($"{detectedFaces.Count} faces detected from image `{imageUrl}`.");
            if (detectedFaces[0].FaceId == null)
            {
                throw new Exception(
                    "Parameter `returnFaceId` of `DetectWithStreamAsync` must be set to `true` (by default) for recognition purpose.");
            }

            return detectedFaces.ToList();
        }


        static async Task MakeRequest()
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "686650135dc64559980a64c9a7ea51d5");

            var uri = "https://photobankface2.cognitiveservices.azure.com/face/v1.0/findsimilars?" + queryString;

            HttpResponseMessage response;

            // Request body
            byte[] byteData = Encoding.UTF8.GetBytes("{\"faceId\": \"2de9761d-66fd-4f27-977a-d2661c3bc73f\", \"faceListId\": \"\", \"largeFaceListId\": \"all-faces-list\", \"faceIds\": [], \"maxNumOfCandidatesReturned\": 10,\"mode\": \"matchFace\"}");

            using (var content = new ByteArrayContent(byteData))
            {
                var headerValue = new MediaTypeHeaderValue("application/json");

                content.Headers.ContentType = headerValue;
                response = await client.PostAsync(uri, content);
            }
        }

        private async Task NewMethod1()
        {
            var faces = await _faceRepository.GetAll()
                .ProjectTo<FaceDto>(_mapper.ConfigurationProvider).ToListAsync();

            var result = from left in faces
                from right in faces
                let distance = FaceRecognition.FaceDistance(left.FaceEncoding, right.FaceEncoding)
                where left.Id > right.Id
                      && left.Gender.HasValue
                      && right.Gender.HasValue
                      && left.Gender.Value == right.Gender.Value
                      && Math.Abs(left.Age - right.Age) < 20
                      && distance <= 0.5
                select new FaceToFace
                {
                    Face1Id = right.Id,
                    Face2Id = left.Id,
                    Distance = distance
                };

            var countTran = 500000;
            var count = countTran;

            var batch = new List<FaceToFace>();

            foreach (var r in result)
            {
                batch.Add(r);

                if (++count % countTran == 0)
                {
                    try
                    {
                        await _f2FRepository.InsertRangeAsync(batch);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    Console.WriteLine($"count = {count}");
                    batch = new List<FaceToFace>();
                }
            }

            await _f2FRepository.InsertRangeAsync(batch);
        }

        private async Task NewMethod()
        {
            const double checkedWithTolerance = 0.5;

            var sampleFaces = await _faceRepository.GetAll()
                .Where(f => f.IsSample == true)
                .ProjectTo<FaceDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            var faces = await _faceRepository.GetAll()
                .Include(f => f.Person)
                .Where(f => f.Person == null && f.CheckedWithTolerance == 0)
                .ProjectTo<FaceDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            foreach (var face in faces)
            {
                var comparedFaces = FaceRecognition.CompareFaces(sampleFaces.Select(f => f.FaceEncoding), face.FaceEncoding,
                    checkedWithTolerance);
                var matches = comparedFaces.Select((m, i) => new {m, i}).Where(item => item.m).Select(item => item.i).ToList();
                var persons = matches.Select(match => sampleFaces[match].PersonId).ToList();
                if (persons.Count < 3 || persons.Distinct().Count() != 1)
                {
                    await _faceRepository.UpdateAsync(
                        new Face
                        {
                            Id = face.Id,
                            CheckedWithTolerance = checkedWithTolerance,
                        },
                        h => h.CheckedWithTolerance);

                    continue;
                }

                await _faceRepository.UpdateAsync(
                    new Face
                    {
                        Id = face.Id,
                        CheckedWithTolerance = checkedWithTolerance,
                        Person = new Person
                        {
                            Id = persons.First().Value
                        }
                    },
                    g => g.Person.Id, h => h.CheckedWithTolerance);
            }
        }

        private async Task AddFilesAsync()
        {
            var storage = await _repository.GetAsync(9);

            var files = await _syncService.SyncStorage(storage);

            //Parallel.ForEach(files,
            //    new ParallelOptions { MaxDegreeOfParallelism = 4 }, 
            //    async (file) =>
            //{
            //    await _photoProcessor.AddPhotoAsync(storage, file);
            //    Console.WriteLine($"Processing {file} on thread {Thread.CurrentThread.ManagedThreadId}");
            //});

            var count = 2000;

            foreach (var file in files)
            {
                try
                {
                    await _photoProcessor.AddPhotoAsync(storage, file);
                }
                catch (Exception e)
                {
                    _logger.Log(LogLevel.Debug, e, file);
                }

                if (count-- == 0)
                {
                    break;
                }

                var savedColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"Count = {count}");
                Console.ForegroundColor = savedColor;
            }

            Console.WriteLine("Done");
        }
    }
}
