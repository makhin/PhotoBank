using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using Person = Microsoft.Azure.CognitiveServices.Vision.Face.Models.Person;
using PersonGroup = Microsoft.Azure.CognitiveServices.Vision.Face.Models.PersonGroup;

namespace PhotoBank.Services
{
    public interface IFaceService
    {
        Task SyncPersonsAsync();
        Task AddFacesToList();
        Task FindSimilarFaces();
        Task FindSimilarFacesInList();
        Task SyncFacesToPersonAsync();
        Task FindFaceAsync();
    }

    public class FaceService : IFaceService
    {
        private readonly IFaceClient _faceClient;
        private readonly IRepository<Face> _faceRepository;
        private readonly IRepository<DbContext.Models.Person> _personRepository;
        private readonly IGeoWrapper _geoWrapper;

        private const string PersonGroupId = "my-cicrle-person-group";
        private const string AllFacesListId = "all-faces-list";

        public bool IsPersonGroupTrained;
        public bool IsFaceListTrained;

        public FaceService(IFaceClient faceClient, IRepository<Face> faceRepository, IRepository<DbContext.Models.Person> personRepository, IGeoWrapper geoWrapper)
        {
            this._faceClient = faceClient;
            _faceRepository = faceRepository;
            _personRepository = personRepository;
            _geoWrapper = geoWrapper;
        }

        public async Task FindSimilarFaces()
        {
            var faces = await _faceRepository.GetAll().Include(f => f.Person).Where(f => f.Person == null).ToListAsync();
            var allFaces = new Dictionary<Face, DetectedFace>();

            int GetFaceIfByDetectedFaceId(Guid faceId)
            {
                return allFaces.Where(face => face.Value.FaceId.Value == faceId).Select(face => face.Key.Id).FirstOrDefault();
            }

            foreach (var face in faces)
            {
                IList<DetectedFace> detectedFaces = await _faceClient.Face.DetectWithStreamAsync(new MemoryStream(face.Image), true, false, new List<FaceAttributeType?>()
                {
                    FaceAttributeType.Noise
                });
                

                foreach (var detectedFace in detectedFaces)
                {
                    if (detectedFace.FaceAttributes.Noise.Value > 0.1)
                    {
                        continue;
                    }

                    allFaces.Add(face, detectedFace);
                }
            }

            var allDetectedFaces = allFaces.Select(f => f.Value);
            var similarFacePairList = new List<Tuple<Guid, Guid>>();

            Console.WriteLine("------------------------");
            Console.WriteLine(allDetectedFaces.Count());
            Console.WriteLine();

            foreach (var face in allDetectedFaces)
            {
                Console.Write(".");
                if (similarFacePairList.Select(f => f.Item2).Contains(face.FaceId.Value))
                {
                    continue;
                }

                var faceIds = allDetectedFaces.Select(f => f.FaceId).Except(new List<Guid?>() {face.FaceId.Value}).ToList();
                var similarFaces = await _faceClient.Face.FindSimilarAsync(face.FaceId.Value, null, null, faceIds);
                

                foreach (var similarFace in similarFaces)
                {
                    if (similarFace.Confidence > 0.8)
                    {
                        similarFacePairList.Add(new Tuple<Guid, Guid>(face.FaceId.Value, similarFace.FaceId.Value));
                    }
                }
            }

            foreach (var tuple in similarFacePairList)
            {
                Console.WriteLine($"Face {GetFaceIfByDetectedFaceId(tuple.Item1)} similar with {GetFaceIfByDetectedFaceId(tuple.Item2)}");
            }
        }

        public async Task FindSimilarFacesInList()
        {

            //await _faceClient.LargeFaceList.TrainAsync(AllFacesListId);
            //var status = await GetListTrainingStatusAsync();
            var dbFaces = await _faceRepository.GetAll().Where(f => f.ExternalGuid.HasValue).Include(f => f.Person).Take(100).ToListAsync();

            var results = new List<IList<SimilarFace>>();

            foreach (var face in dbFaces)
            {
                IList<DetectedFace> detectedFaces = await _faceClient.Face.DetectWithStreamAsync(new MemoryStream(face.Image));

                foreach (DetectedFace detectedFace in detectedFaces)
                {
                    var faces = await _faceClient.Face.FindSimilarAsync(detectedFace.FaceId.Value, null,
                        largeFaceListId: AllFacesListId, null, 20, FindSimilarMatchMode.MatchFace);
                    results.Add(faces);
                }
            }
        }

        public async Task SyncPersonsAsync()
        {
            const string recognitionModel = RecognitionModel.Recognition02;

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
                    await _faceClient.PersonGroup.CreateAsync(PersonGroupId, PersonGroupId, recognitionModel: recognitionModel);
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

        public async Task AddFacesToList()
        {
            var azureFaceList = await _faceClient.LargeFaceList.GetAsync(AllFacesListId);

            if (azureFaceList != null)
            {
                await _faceClient.LargeFaceList.DeleteAsync(AllFacesListId);
            }
            await _faceClient.LargeFaceList.CreateAsync(AllFacesListId, "Faces_2", null, RecognitionModel.Recognition03);

            var dbFaces = await _faceRepository.GetAll().Where(f => !f.IsExcluded).Take(100).ToListAsync();

            foreach (var face in dbFaces)
            {
                Console.WriteLine(face.Id);
                try
                {
//                    var targetFace = _geoWrapper.GetRectangleArray(face.Rectangle);

                    var persistedFace = await _faceClient.LargeFaceList
                        .AddFaceFromStreamAsync(AllFacesListId, new MemoryStream(face.Image),
                            face.Id.ToString(), null, DetectionModel.Detection02);

                    face.ExternalGuid = persistedFace.PersistedFaceId;
                    await _faceRepository.UpdateAsync(face);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            IsFaceListTrained = await GetListTrainingStatusAsync();
            Console.WriteLine("Done");
        }

        public async Task FindFaceAsync()
        {
            var faces = await _faceRepository.GetAll().Include(f => f.Person).Where(f => f.Id > 1797 && (!f.IsSample.HasValue || (f.IsSample.HasValue && !f.IsSample.Value))).ToListAsync();
            var persons = await _personRepository.GetAll().ToListAsync();

            foreach (var face in faces)
            {
                Console.WriteLine(face.Id);
                var detectedFaces=  await _faceClient.Face.DetectWithStreamAsync(new MemoryStream(face.Image));
                try
                {
                    var detectedFace = detectedFaces.Single();
                    foreach (var person in persons)
                    {
                        var verifyResult = await _faceClient.Face.VerifyFaceToPersonAsync(detectedFace.FaceId.Value, person.ExternalGuid, PersonGroupId);
                        
                        if (verifyResult.IsIdentical)
                        {
                            Console.WriteLine("Identical");
                            face.Person = person;
                            await _faceRepository.UpdateAsync(face);
                            break;
                        }
                        Console.WriteLine("Not identical");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public async Task SyncFacesToPersonAsync()
        {
            var faces = await _faceRepository.GetAll().Include(f => f.Person)
                .Where(f => f.IsSample.HasValue && f.IsSample.Value).ToListAsync();

            foreach (var face in faces)
            {
                var person = await _faceClient.PersonGroupPerson.GetAsync(PersonGroupId, face.Person.ExternalGuid);

                foreach (var faceId in person.PersistedFaceIds)
                {
                    await _faceClient.PersonGroupPerson.DeleteFaceAsync(PersonGroupId, face.Person.ExternalGuid, faceId.Value);
                }

                await _faceClient.PersonGroupPerson.AddFaceFromStreamAsync(PersonGroupId, face.Person.ExternalGuid, new MemoryStream(face.Image), face.Id.ToString());
            }
            
            await _faceClient.PersonGroup.TrainAsync(PersonGroupId);

            IsPersonGroupTrained = await GetTrainingStatusAsync();
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

        private async Task<bool> GetTrainingStatusAsync()
        {
            TrainingStatus trainingStatus = null;
            try
            {
                do
                {
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
            TrainingStatus trainingStatus = null;
            try
            {
                do
                {
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
