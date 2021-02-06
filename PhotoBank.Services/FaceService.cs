using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;
using PhotoBank.Dto.Load;
using PhotoBank.Repositories;
using Person = PhotoBank.DbContext.Models.Person;
using PersonGroup = Microsoft.Azure.CognitiveServices.Vision.Face.Models.PersonGroup;

namespace PhotoBank.Services
{
    public interface IFaceService
    {
        Task SyncPersonsAsync();
        Task SyncFacesToPersonAsync();
        Task AddFacesToLargeFaceListAsync();
        Task GroupIdentifyAsync();
        Task FaceIdentityAsync(Face face);
        Task ListFindSimilarAsync();
    }

    public class FaceService : IFaceService
    {
        private readonly IFaceClient _faceClient;
        private readonly IRepository<Face> _faceRepository;
        private readonly IRepository<DbContext.Models.Person> _personRepository;
        private readonly IRepository<PersonGroupFace> _personGroupFaceRepository;
        private readonly IRepository<Photo> _photoRepository;
        private readonly IMapper _mapper;

        private const string PersonGroupId = "my-cicrle-person-group";
        private const string AllFacesListId = "all-faces-list";

        public bool IsPersonGroupTrained;
        public bool IsFaceListTrained;
        private List<Person> _persons;

        private const string RecognitionModel = Microsoft.Azure.CognitiveServices.Vision.Face.Models.RecognitionModel.Recognition02;
        private const string DetectionModel = Microsoft.Azure.CognitiveServices.Vision.Face.Models.DetectionModel.Detection02;


        public FaceService(IFaceClient faceClient,
            IRepository<Face> faceRepository,
            IRepository<DbContext.Models.Person> personRepository,
            IRepository<PersonGroupFace> personGroupFaceRepository,
            IRepository<Photo> photoRepository,
            IMapper mapper)
        {
            this._faceClient = faceClient;
            _faceRepository = faceRepository;
            _personRepository = personRepository;
            _personGroupFaceRepository = personGroupFaceRepository;
            _photoRepository = photoRepository;
            _mapper = mapper;
        }

        public async Task SyncPersonsAsync()
        {
            try
            {
                PersonGroup group = null;
                try
                {
                    group = await _faceClient.PersonGroup.GetAsync(PersonGroupId);
                }
                catch (Exception)
                {
                    // ignored
                }

                if (group == null)
                {
                    await _faceClient.PersonGroup.CreateAsync(PersonGroupId, PersonGroupId, recognitionModel: RecognitionModel);
                }

                var dbPersons = await _personRepository.GetAll().ToListAsync();
                var servicePersons = await _faceClient.PersonGroupPerson.ListAsync(PersonGroupId);

                foreach (var dbPerson in dbPersons)
                {
                    if (dbPerson.ExternalGuid != Guid.Empty && servicePersons.Select(p => p.PersonId).Contains(dbPerson.ExternalGuid))
                    {
                        continue;
                    }

                    var person = await _faceClient.PersonGroupPerson.CreateAsync(PersonGroupId, dbPerson.Name,
                        dbPerson.Id.ToString());
                    await Task.Delay(1000);

                    dbPerson.ExternalGuid = person.PersonId;
                    await _personRepository.UpdateAsync(dbPerson, p => p.ExternalGuid);
                }

                foreach (var servicePerson in servicePersons)
                {
                    if (dbPersons.Select(p => p.ExternalGuid).Contains(servicePerson.PersonId))
                    {
                        continue;
                    }

                    await _faceClient.PersonGroupPerson.DeleteAsync(PersonGroupId, servicePerson.PersonId);
                    await Task.Delay(1000);
                }

            }
            catch (APIErrorException ae)
            {
                Debug.WriteLine(ae.Message);
            }
        }

        public async Task SyncFacesToPersonAsync()
        {
            var dbPersonGroupFaces = await _personGroupFaceRepository.GetAll().Include(p => p.Person).ToListAsync();

            var groupBy = dbPersonGroupFaces.GroupBy(x => new { x.PersonId, x.Person.ExternalGuid }, p=> new { p.FaceId, p.ExternalGuid } ,
                (key, g) => new { Key = key, Faces = g.ToList()});

            foreach (var dbPerson in groupBy)
            {
                var person = await _faceClient.PersonGroupPerson.GetAsync(PersonGroupId, dbPerson.Key.ExternalGuid);

                foreach (var personFace in dbPerson.Faces)
                {
                    if (person.PersistedFaceIds.Contains(personFace.ExternalGuid))
                    {
                        continue;
                    }

                    var dbFace = await _faceRepository.GetAsync(personFace.FaceId);
                    await using (var stream = new MemoryStream(dbFace.Image))
                    {
                        try
                        {
                            var face = await _faceClient.PersonGroupPerson.AddFaceFromStreamAsync(PersonGroupId, person.PersonId, stream, personFace.FaceId.ToString());
                            var personGroupFace = dbPersonGroupFaces.Single(g => g.PersonId == dbPerson.Key.PersonId && g.FaceId == personFace.FaceId);
                            personGroupFace.ExternalGuid = face.PersistedFaceId;
                            await _personGroupFaceRepository.UpdateAsync(personGroupFace, pgf => pgf.ExternalGuid);

                            dbFace.ListStatus = ListStatus.Uploaded; // TODO Remove
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            dbFace.ListStatus = ListStatus.Failed; // TODO Remove
                        }

                        await _faceRepository.UpdateAsync(dbFace, f => f.ListStatus); // TODO Remove
                    }
                }

                foreach (var persistedFaceId in person.PersistedFaceIds.Where(f => f != null))
                {
                    if (dbPerson.Faces.Select(f => f.ExternalGuid).ToList().Contains(persistedFaceId.Value))
                    {
                        continue;
                    }

                    await _faceClient.PersonGroupPerson.DeleteFaceAsync(PersonGroupId, person.PersonId, persistedFaceId.Value);
                }
            }
            
            await _faceClient.PersonGroup.TrainAsync(PersonGroupId);
            IsPersonGroupTrained = await GetTrainingStatusAsync();
        }

        public async Task AddFacesToLargeFaceListAsync()
        {
            var dbFaces = await _faceRepository.GetAll()
                .Include(f => f.PersonGroupFace)
                .Where(f => f.PersonGroupFace == null)
                .Take(1000).ToListAsync();

            LargeFaceList list = null;

            try
            {
                list = await _faceClient.LargeFaceList.GetAsync(AllFacesListId);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            if (list == null)
            {
                Console.WriteLine($"Create large face list {AllFacesListId}.");
                await _faceClient.LargeFaceList.CreateAsync(AllFacesListId, "All faces", "All Faces", recognitionModel: RecognitionModel);
            }

            var serviceFaces = await _faceClient.LargeFaceList.ListFacesAsync(AllFacesListId);
            var ids = serviceFaces.Select(f => f.UserData).ToList();

            foreach (var dbFace in dbFaces)
            {
                if (ids.Contains(dbFace.Id.ToString()))
                {
                    continue;
                }

                await Task.Delay(1800);
                await using (var stream = new MemoryStream(dbFace.Image))
                {
                    try
                    {
                        var faces = await _faceClient.LargeFaceList.AddFaceFromStreamAsync(AllFacesListId, stream, dbFace.Id.ToString());
                        if (faces == null)
                        {
                            throw new Exception($"No face detected from image `{dbFace.Id}`.");
                        }

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    Console.WriteLine($"Face from image {dbFace.Id} is successfully added to the large face list.");
                }
            }

            // Start to train the large face list.
            Console.WriteLine($"Train large face list {AllFacesListId}.");
            await _faceClient.LargeFaceList.TrainAsync(AllFacesListId);
            IsFaceListTrained = await GetListTrainingStatusAsync();
        }

        public async Task GroupIdentifyAsync()
        {
            var faces = await _faceRepository
                .GetAll()
                .Include(f => f.Person)
                .Include(f => f.Photo)
                .Where( f => f.Photo.StorageId == 9 && f.IdentityStatus == IdentityStatus.ForReprocessing)
                .ProjectTo<FaceDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            _persons = await _personRepository.GetAll().ToListAsync();

            int currentMinute = DateTime.Now.Minute;
            int callCounter = 0;

            foreach (var face in faces)
            {
                try
                {
                    IList<DetectedFace> detectedFaces;
                    var dbFace = new Face {Id = face.Id};

                    try
                    {
                        await SleepAsync();
                        detectedFaces = await DetectFacesAsync(face.Image);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        dbFace.IdentityStatus = IdentityStatus.NotDetected;
                        await _faceRepository.UpdateAsync(dbFace, f => f.IdentityStatus);
                        continue;
                    }

                    await SleepAsync();
                    var identifyResults = await _faceClient.Face.IdentifyAsync(detectedFaces.Select(f => f.FaceId).ToList(), PersonGroupId);

                    if (!identifyResults.Any())
                    {
                        dbFace.IdentityStatus = IdentityStatus.NotIdentified;
                        await _faceRepository.UpdateAsync(dbFace, f => f.IdentityStatus);
                    }

                    foreach (var identifyResult in identifyResults)
                    {
                        var candidate = identifyResult.Candidates.OrderByDescending(x => x.Confidence).FirstOrDefault();
                        if (candidate == null)
                        {
                            dbFace.IdentityStatus = IdentityStatus.NotIdentified;
                            await _faceRepository.UpdateAsync(dbFace, f => f.IdentityStatus);
                        }
                        else
                        {
                            var person = _persons.Single(p => p.ExternalGuid == candidate.PersonId);
                            if (person.DateOfBirth != null && 
                                face.PhotoTakenDate > new DateTime(1990, 1, 1) &&
                                face.PhotoTakenDate < person.DateOfBirth)
                            {
                                dbFace.IdentityStatus = IdentityStatus.NotIdentified;
                                await _faceRepository.UpdateAsync(dbFace, f => f.IdentityStatus);
                            }
                            else
                            {
                                dbFace.IdentityStatus = IdentityStatus.Identified;
                                dbFace.IdentifiedWithConfidence = candidate.Confidence;
                                dbFace.Person = person;
                                await _faceRepository.UpdateAsync(dbFace, f => f.IdentityStatus, f => f.Person.Id, f => f.IdentifiedWithConfidence);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            async Task SleepAsync()
            {
                const int callsPerMinute = 6000;
                if (currentMinute == DateTime.Now.Minute && ++callCounter >= callsPerMinute)
                {
                    var millisecondsDelay = 60000 - DateTime.Now.Millisecond;
                    var color = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Sleep for {millisecondsDelay}");
                    await Task.Delay(millisecondsDelay);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Resume");
                    Console.ForegroundColor = color;
                }

                if (currentMinute < DateTime.Now.Minute)
                {
                    callCounter = 0;
                }

                currentMinute = DateTime.Now.Minute;
            }
        }

        public async Task ListFindSimilarAsync()
        {
            var photos = await _faceRepository
                .GetAll()
                .Take(100).Select(p => p.Image).ToListAsync();

            var persistedFaces = (await _faceClient.LargeFaceList.ListFacesAsync(AllFacesListId)).ToList();

            foreach (var photo in photos)
            {
                try
                {
                    IList<DetectedFace> detectedFaces = await DetectFacesAsync(photo);
                    await Task.Delay(1800);
    
                    var similarResults = await _faceClient.Face.FindSimilarAsync(detectedFaces[0].FaceId.Value, null, AllFacesListId);
                    await Task.Delay(1800);

                    foreach (var similarResult in similarResults)
                    {
                        var persistedFace = persistedFaces.Find(face => face.PersistedFaceId == similarResult.PersistedFaceId);
                        if (persistedFace == null)
                        {
                            Console.WriteLine("Persisted face not found in similar result.");
                            continue;
                        }

                        Console.WriteLine($"Faces from {persistedFace.UserData} are similar with confidence: {similarResult.Confidence}.");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public async Task FaceIdentityAsync(Face face)
        {
            _persons ??= await _personRepository.GetAll().ToListAsync();

            try
            {
                IList<DetectedFace> detectedFaces;

                try
                {
                    detectedFaces = await DetectFacesAsync(face.Image);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    face.IdentityStatus = IdentityStatus.NotDetected;
                    return;
                }

                var identifyResults = await _faceClient.Face.IdentifyAsync(detectedFaces.Select(f => f.FaceId).ToList(), PersonGroupId);

                if (!identifyResults.Any())
                {
                    face.IdentityStatus = IdentityStatus.NotIdentified;
                    return;
                }

                foreach (var identifyResult in identifyResults)
                {
                    var candidate = identifyResult.Candidates.OrderByDescending(x => x.Confidence).FirstOrDefault();
                    if (candidate == null)
                    {
                        face.IdentityStatus = IdentityStatus.NotIdentified;
                    }
                    else
                    {
                        var person = _persons.Single(p => p.ExternalGuid == candidate.PersonId);
                        if (person.DateOfBirth != null && face.Photo.TakenDate > new DateTime(1990, 1, 1) &&
                            face.Photo.TakenDate < person.DateOfBirth)
                        {
                            face.IdentityStatus = IdentityStatus.NotIdentified;
                        }
                        else
                        {
                            face.IdentityStatus = IdentityStatus.Identified;
                            face.IdentifiedWithConfidence = candidate.Confidence;
                            face.Person = person;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task<List<DetectedFace>> DetectFacesAsync(byte[] image)
        {
            await using (var stream = new MemoryStream(image))
            {
                IList<FaceAttributeType?> attributes = new List<FaceAttributeType?>()
                {
                    FaceAttributeType.Accessories, FaceAttributeType.Age, FaceAttributeType.Blur,
                    FaceAttributeType.Emotion, FaceAttributeType.Exposure, FaceAttributeType.FacialHair,
                    FaceAttributeType.Gender, FaceAttributeType.Glasses, FaceAttributeType.Hair, 
                    FaceAttributeType.HeadPose, FaceAttributeType.Makeup, FaceAttributeType.Noise,
                    FaceAttributeType.Smile,
                };
                var detectedFaces = await _faceClient.Face.DetectWithStreamAsync(stream, recognitionModel: RecognitionModel, detectionModel: DetectionModel );
                if (detectedFaces == null || detectedFaces.Count == 0)
                {
                    throw new Exception($"No face detected");
                }

                return detectedFaces.ToList();
            }
        }

        private async Task<bool> GetTrainingStatusAsync()
        {
            TrainingStatus trainingStatus;
            try
            {
                do
                {
                    await Task.Delay(1000);
                    trainingStatus = await _faceClient.PersonGroup.GetTrainingStatusAsync(PersonGroupId);
                } while (trainingStatus.Status == TrainingStatusType.Running);
            }
            catch (APIErrorException ae)
            {
                Debug.WriteLine("GetTrainingStatusAsync: " + ae.Message);
                return false;
            }
            return trainingStatus.Status == TrainingStatusType.Succeeded;
        }

        private async Task<bool> GetListTrainingStatusAsync()
        {
            TrainingStatus trainingStatus;
            try
            {
                do
                {
                    await Task.Delay(1000);
                    trainingStatus = await _faceClient.LargeFaceList.GetTrainingStatusAsync(AllFacesListId);
                } while (trainingStatus.Status == TrainingStatusType.Running);
            }
            catch (APIErrorException ae)
            {
                Debug.WriteLine("GetTrainingStatusAsync: " + ae.Message);
                return false;
            }
            return trainingStatus.Status == TrainingStatusType.Succeeded;
        }
    }
}
