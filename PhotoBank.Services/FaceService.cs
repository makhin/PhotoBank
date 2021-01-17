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
using PersonGroup = Microsoft.Azure.CognitiveServices.Vision.Face.Models.PersonGroup;

namespace PhotoBank.Services
{
    public interface IFaceService
    {
        Task SyncPersonsAsync();
        Task SyncFacesToPersonAsync();
        Task AddFacesToLargeFaceListAsync();
    }

    public class FaceService : IFaceService
    {
        private readonly IFaceClient _faceClient;
        private readonly IRepository<Face> _faceRepository;
        private readonly IRepository<DbContext.Models.Person> _personRepository;
        private readonly IRepository<PersonGroupFace> _personGroupFaceRepository;

        private const string PersonGroupId = "my-cicrle-person-group";
        private const string AllFacesListId = "all-faces-list";

        public bool IsPersonGroupTrained;
        public bool IsFaceListTrained;

        private const string RecognitionModel = Microsoft.Azure.CognitiveServices.Vision.Face.Models.RecognitionModel.Recognition02;

        public FaceService(IFaceClient faceClient, IRepository<Face> faceRepository, IRepository<DbContext.Models.Person> personRepository, IRepository<PersonGroupFace> personGroupFaceRepository)
        {
            this._faceClient = faceClient;
            _faceRepository = faceRepository;
            _personRepository = personRepository;
            _personGroupFaceRepository = personGroupFaceRepository;
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
            var dbPersonGroupFaces = await _personGroupFaceRepository.GetAll().ToListAsync();

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

                    var dbFace = new Face {Id = personFace.FaceId};
                    await using (var stream = new MemoryStream(dbFace.Image))
                    {
                        try
                        {
                            var face = await _faceClient.PersonGroupPerson.AddFaceFromStreamAsync(PersonGroupId, person.PersonId, stream, personFace.FaceId.ToString());
                            var personGroupFace = dbPersonGroupFaces.Single(g => g.PersonId == dbPerson.Key.PersonId && g.FaceId == personFace.FaceId);
                            personGroupFace.ExternalGuid = face.PersistedFaceId;
                            await _personGroupFaceRepository.UpdateAsync(personGroupFace, pgf => pgf.ExternalGuid);

                            dbFace.Status = Status.Uploaded;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            dbFace.Status = Status.Failed;
                        }

                        await _faceRepository.UpdateAsync(dbFace, f => f.Status);
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

        private async Task<List<DetectedFace>> DetectFacesAsync(byte[] image, string recognitionModel = Microsoft.Azure.CognitiveServices.Vision.Face.Models.RecognitionModel.Recognition01)
        {
            var stream = new MemoryStream(image);

            // Detect faces from image stream.
            IList<DetectedFace> detectedFaces = await _faceClient.Face.DetectWithStreamAsync(stream, recognitionModel: recognitionModel);
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

/*
        // Get persisted faces from the large face list.
        var persistedFaces = (await _faceClient.LargeFaceList.ListFacesAsync(AllFacesListId)).ToList();
            if (persistedFaces.Count == 0)
        {
            throw new Exception($"No persisted face in large face list '{AllFacesListId}'.");
        }

        var ff = (await _faceRepository.GetAsync(1692)).Image;

        // Detect faces from source image url.
        IList<DetectedFace> detectedFaces = await DetectFacesAsync(ff, recognitionModel: RecognitionModel);

        // Find similar example of faceId to large face list.
        var similarResults = await _faceClient.Face.FindSimilarAsync(detectedFaces[0].FaceId.Value, null, AllFacesListId);

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
*/
    }
}
