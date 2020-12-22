using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace PhotoBank.Services
{
    public interface IFaceService
    {
        Task AddFacesToList();
        Task FindSimilarFaces();
        Task FindSimilarFacesInList();
        Task SyncPersonsAsync();
        Task SyncFacesToPersonAsync();
        Task FindFaceAsync();

        Task Test();
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

            var azureFaceList = await _faceClient.FaceList.GetAsync(AllFacesListId);
            IList<Guid?> faceIds = azureFaceList.PersistedFaces.Select(f => (Guid?)f.PersistedFaceId).ToList();
            var dbFaces = await _faceRepository.GetAll().Include(f => f.Person).ToListAsync();

            var results = new List<IList<SimilarFace>>();

            foreach (var face in dbFaces)
            {


                using (MemoryStream stream = new MemoryStream(face.Image))
                {
                    var faces = await _faceClient.Face.DetectWithStreamAsync(stream);
                    foreach (var f in faces)
                    {
                        results.Add(await _faceClient.Face.FindSimilarAsync(f.FaceId.Value, azureFaceList.FaceListId, maxNumOfCandidatesReturned:20));
                    }
                }

                var detectedFaces = await _faceClient.Face.DetectWithStreamAsync(new MemoryStream(face.Image));
                var detectedFace = detectedFaces.Single();

                //var similarFaces = await _faceClient.Face.FindSimilarAsync(faceIds[0].Value, null, null, faceIds, 20, FindSimilarMatchMode.MatchFace);

                var similarFaces = await _faceClient.Face.FindSimilarAsync(detectedFace.FaceId.Value, azureFaceList.FaceListId, null, null, 20, FindSimilarMatchMode.MatchFace);
                foreach (var similarFace in similarFaces)
                {
                    Console.WriteLine($"Persisted face{face.Id} similar to {similarFace.PersistedFaceId} {similarFace.FaceId} with {similarFace.Confidence}");
                }
            }
        }

        public async Task AddFacesToList()
        {
            var azureFaceList = await _faceClient.FaceList.GetAsync(AllFacesListId);

            if (azureFaceList != null)
            {
                await _faceClient.FaceList.DeleteAsync(AllFacesListId);
            }
            await _faceClient.FaceList.CreateAsync(AllFacesListId, "Faces_2", null, RecognitionModel.Recognition03);

            var dbFaces = _faceRepository.GetAll().ToList();

            foreach (var face in dbFaces)
            {
                Console.WriteLine(face.Id);
                try
                {
                    var targetFace = _geoWrapper.GetRectangleArray(face.Rectangle);

                    var persistedFace = await _faceClient.FaceList
                        .AddFaceFromStreamAsync(AllFacesListId, new MemoryStream(face.Image),
                            face.Id.ToString(), targetFace, DetectionModel.Detection02);

                    face.ExternalGuid = persistedFace.PersistedFaceId;
                    await _faceRepository.UpdateAsync(face);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

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


        public async Task SyncPersonsAsync()
        {
            try
            {
                var faceServicePersons = await _faceClient.PersonGroupPerson.ListAsync(PersonGroupId);
                var dbPersons = await _personRepository.GetAll().ToListAsync();

                foreach (var dbPerson in dbPersons)
                {
                    if (!faceServicePersons.Select(p => p.Name).Contains(dbPerson.Name))
                    {
                        Person servicePerson = await _faceClient.PersonGroupPerson.CreateAsync(PersonGroupId, dbPerson.Name);
                        dbPerson.ExternalGuid = servicePerson.PersonId;
                        await _personRepository.UpdateAsync(dbPerson);
                    }
                }

                foreach (var faceServicePerson in faceServicePersons)
                {
                    if (!dbPersons.Select(p => p.Name).Contains(faceServicePerson.Name))
                    {
                        await _faceClient.PersonGroupPerson.DeleteAsync(PersonGroupId, faceServicePerson.PersonId);
                    }
                }
            }
            catch (APIErrorException ae)
            {
                Debug.WriteLine(ae.Message);
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


        public async Task Test()
        {
            Console.WriteLine("Sample of finding similar faces in face list.");

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

            // Create a face list.
            string faceListId = "test-test-test";
            Console.WriteLine($"Create FaceList {faceListId}.");
            try
            {
                await _faceClient.FaceList.CreateAsync(faceListId, "face list for FindSimilar sample", "face list for FindSimilar sample", recognitionModel: recognitionModel);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }


            foreach (var targetImageFileName in targetImageFileNames)
            {
                // Add face to face list.
                var faces = await _faceClient.FaceList.AddFaceFromUrlAsync(
                                faceListId,
                                $"{ImageUrlPrefix}{targetImageFileName}",
                                targetImageFileName);
                if (faces == null)
                {
                    throw new Exception($"No face detected from image `{targetImageFileName}`.");
                }

                Console.WriteLine($"Face from image {targetImageFileName} is successfully added to the face list.");
            }

            // Get persisted faces from the face list.
            List<PersistedFace> persistedFaces = (await _faceClient.FaceList.GetAsync(faceListId)).PersistedFaces.ToList();
            if (persistedFaces.Count == 0)
            {
                throw new Exception($"No persisted face in face list '{faceListId}'.");
            }

            // Detect faces from source image url.
            IList<DetectedFace> detectedFaces =
                await _faceClient.Face.DetectWithUrlAsync("{ImageUrlPrefix}{sourceImageFileName}",
                    recognitionModel: recognitionModel);
                
            // Find similar example of faceId to face list.
            var similarResults = await _faceClient.Face.FindSimilarAsync(detectedFaces[0].FaceId.Value, faceListId);
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

            // Delete the face list.
            await _faceClient.FaceList.DeleteAsync(faceListId);
            Console.WriteLine($"Delete FaceList {faceListId}.");
            Console.WriteLine();
        }


        private async Task<bool> GetTrainingStatusAsync()
        {
            TrainingStatus trainingStatus = null;
            try
            {
                do
                {
                    trainingStatus = await _faceClient.PersonGroup.GetTrainingStatusAsync(PersonGroupId);
                    await Task.Delay(1000);
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
