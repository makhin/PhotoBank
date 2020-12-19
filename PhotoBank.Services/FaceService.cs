using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using FaceList = PhotoBank.DbContext.Models.FaceList;
using Person = Microsoft.Azure.CognitiveServices.Vision.Face.Models.Person;
using PersonGroup = Microsoft.Azure.CognitiveServices.Vision.Face.Models.PersonGroup;

namespace PhotoBank.Services
{
    public interface IFaceService
    {
        void AddFacesToList();
        void FindSimilarFaces();

        /// <summary>
        /// Returns all PersonGroup's associated with the Face subscription key
        /// </summary>
        /// <returns>A list of PersonGroup's or an empty list</returns>
        Task<IList<PersonGroup>> GetAllPersonGroupsAsync();

        /// <summary>
        /// Returns all Person.Name's associated with PERSONGROUPID
        /// </summary>
        /// <returns>A list of Person.Name's or an empty list</returns>
        Task<IList<string>> GetAllPersonNamesAsync();

        /// <summary>
        /// Gets or creates a PersonGroup with PERSONGROUPID
        /// </summary>
        Task GetOrCreatePersonGroupAsync();

        /// <summary>
        /// Gets or creates a PersonGroupPerson
        /// </summary>
        /// <param name="name">PersonGroupPerson.Name</param>
        /// <param name="groupInfos">A collection specifying the file paths of images associated with <paramref name="name"/></param>
        Task GetOrCreatePersonAsync(string name, ObservableCollection<ImageInfo> groupInfos);

        /// <summary>
        /// Adds PersistedFace's to 'personName'
        /// </summary>
        /// <param name="selectedItems">A collection specifying the file paths of images to be associated with searchedForPerson</param>
        /// <param name="groupInfos"></param>
        Task AddFacesToPersonAsync(IList<ImageInfo> selectedItems, ObservableCollection<ImageInfo> groupInfos);

        /// <summary>
        /// Determines whether a given face matches searchedForPerson 
        /// </summary>
        /// <param name="faceId">PersistedFace.PersistedFaceId</param>
        /// <param name="newImage">On success, contains confidence value</param>
        /// <returns>Whether <paramref name="faceId"/> matches searchedForPerson</returns>
        Task<bool> MatchFaceAsync(Guid faceId, ImageInfo newImage);

        /// <summary>
        /// Sets 'GroupInfos', which specifies the file paths of images associated with searchedForPerson
        /// </summary>
        /// <param name="groupInfos">On success, contains image info associated with searchedForPerson</param>
        Task DisplayFacesAsync(ObservableCollection<ImageInfo> groupInfos);

        /// <summary>
        /// Deletes searchedForPerson
        /// </summary>
        /// <param name="groupInfos"></param>
        /// <param name="groupNames"></param>
        /// <param name="askFirst">true to display a confirmation dialog</param>
        Task DeletePersonAsync(ObservableCollection<ImageInfo> groupInfos, ObservableCollection<string> groupNames, bool askFirst = true);

    }

    public class ImageInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Metadata { get; set; } = string.Empty;
        public string Attributes { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public string OcrResult { get; set; } = string.Empty;
        public string ThumbUrl { get; set; } = string.Empty; //"Assets/FaceFinder.jpg";
        public string Confidence { get; set; } = string.Empty;
    }

    public class FaceService : IFaceService
    {
        private readonly IFaceClient _faceClient;
        private readonly IRepository<Face> _faceRepository;
        private readonly IRepository<FaceList> _faceListRepository;

        private const string PersonGroupId = "my-cicrle-person-group";
        private readonly Person _emptyPerson = new Person(Guid.Empty, string.Empty);

        // Set in GetOrCreatePersonAsync()
        private Person _searchedForPerson;

        public bool IsPersonGroupTrained;

        public FaceService(IFaceClient faceClient, IRepository<Face> faceRepository, IRepository<FaceList> faceListRepository)
        {
            this._faceClient = faceClient;
            _faceRepository = faceRepository;
            _faceListRepository = faceListRepository;
        }

        public void FindSimilarFaces()
        {
            var faceList = _faceListRepository.Get(2);
            var fl = _faceClient.FaceList.GetAsync(faceList.ExternalGuid.ToString()).Result;
            var face = _faceRepository.GetAll().ToList().FirstOrDefault();

            var detectedFaces = _faceClient.Face.DetectWithStreamAsync(new MemoryStream(face.Image)).Result;

            var similarFaces = _faceClient.Face.FindSimilarAsync(detectedFaces[0].FaceId.Value, fl.FaceListId).Result;
        }

        public void AddFacesToList()
        {
            var faceList = _faceListRepository.Get(2);

            var fl = _faceClient.FaceList.GetAsync(faceList.ExternalGuid.ToString()).Result;

            if (fl == null)
            {
                _faceClient.FaceList.CreateAsync(faceList.ExternalGuid.ToString(), "Faces_2", null, RecognitionModel.Recognition03).Wait();
            }
            else
            {
                foreach (var persistedFace in fl.PersistedFaces)
                {
                    _faceClient.FaceList.DeleteFaceAsync(faceList.ExternalGuid.ToString(), persistedFace.PersistedFaceId).Wait();
                }
                
                fl.PersistedFaces.Clear();
            }

            var faces = _faceRepository.GetAll().ToList();

            foreach (var face in faces)
            {
                try
                {
                    var persistedFace = _faceClient.FaceList.AddFaceFromStreamAsync(faceList.ExternalGuid.ToString(), new MemoryStream(face.Image), null, null, DetectionModel.Detection02).Result;

                    face.FaceListFaces ??= new List<FaceListFace>();
                    face.FaceListFaces.Add(new FaceListFace
                    {
                        ExternalGuid = persistedFace.PersistedFaceId,
                        Face = face,
                        FaceList = faceList
                    });

                    Task.Delay(3000).Wait();
                    var result = _faceRepository.UpdateAsync(face).Result;
                }
                catch (Exception e)
                {
                    Console.WriteLine(face.Id);
                    Console.WriteLine(e);
                }
            }

            Console.WriteLine("Done");
        }

        /// <summary>
        /// Returns all PersonGroup's associated with the Face subscription key
        /// </summary>
        /// <returns>A list of PersonGroup's or an empty list</returns>
        public async Task<IList<PersonGroup>> GetAllPersonGroupsAsync()
        {
            try
            {
                return await _faceClient.PersonGroup.ListAsync();
            }
            catch (APIErrorException e)
            {
                Debug.WriteLine("GetAllPersonGroupsAsync: " + e.Message);
            }
            return new List<PersonGroup>();
        }

        /// <summary>
        /// Returns all Person.Name's associated with PERSONGROUPID
        /// </summary>
        /// <returns>A list of Person.Name's or an empty list</returns>
        public async Task<IList<string>> GetAllPersonNamesAsync()
        {
            IList<string> names = new List<string>();
            try
            {
                IList<Person> personNames = await _faceClient.PersonGroupPerson.ListAsync(PersonGroupId);
                foreach (Person person in personNames)
                {
                    // Remove appended "-group".
                    names.Add(person.Name.Replace("_", " "));
                }
            }
            catch (APIErrorException e)
            {
                Debug.WriteLine("GetAllPersonNamesAsync: " + e.Message);
            }
            return names;
        }

        /// <summary>
        /// Gets or creates a PersonGroup with PERSONGROUPID
        /// </summary>
        public async Task GetOrCreatePersonGroupAsync()
        {
            try
            {
                PersonGroup personGroup = null;

                // Get PersonGroup if it exists.
                IList<PersonGroup> groups = await _faceClient.PersonGroup.ListAsync();
                foreach (PersonGroup group in groups)
                {
                    if (group.PersonGroupId == PersonGroupId)
                    {
                        personGroup = group;
                        break;
                    }
                }

                if (personGroup == null)
                {
                    // PersonGroup doesn't exist, create it.
                    await _faceClient.PersonGroup.CreateAsync(PersonGroupId, "Test_group");
                    personGroup = (await _faceClient.PersonGroup.ListAsync())[0];
                }
                Debug.WriteLine("GetOrCreatePersonGroupAsync: " + personGroup.PersonGroupId);
            }
            catch (APIErrorException ae)
            {
                Debug.WriteLine("GetOrCreatePersonGroupAsync: " + ae.Message);
            }
        }

        /// <summary>
        /// Gets or creates a PersonGroupPerson
        /// </summary>
        /// <param name="name">PersonGroupPerson.Name</param>
        /// <param name="GroupInfos">A collection specifying the file paths of images associated with <paramref name="name"/></param>
        public async Task GetOrCreatePersonAsync(string name, ObservableCollection<ImageInfo> GroupInfos)
        {
            if (string.IsNullOrWhiteSpace(name)) { return; }
            Debug.WriteLine("GetOrCreatePersonAsync: " + name);

            GroupInfos.Clear();
            IsPersonGroupTrained = false;

            _searchedForPerson = _emptyPerson;
            string personName = ConfigurePersonName(name);

            try
            {
                IList<Person> people =
                    await _faceClient.PersonGroupPerson.ListAsync(PersonGroupId);

                // Get Person if it exists.
                foreach (Person person in people)
                {
                    if (person.Name.Equals(personName))
                    {
                        _searchedForPerson = person;
                        if (_searchedForPerson.PersistedFaceIds.Count > 0)
                        {
                            await DisplayFacesAsync(GroupInfos);
                            IsPersonGroupTrained = true;
                        }
                        return;
                    }
                }

                // Person doesn't exist, create it.
                await _faceClient.PersonGroupPerson.CreateAsync(PersonGroupId, personName);

                // MUST re-query to get completely formed PersonGroupPerson
                _searchedForPerson = (await _faceClient.PersonGroupPerson.ListAsync(PersonGroupId))[0];
                return;
            }
            catch (APIErrorException ae)
            {
                Debug.WriteLine("GetOrCreatePersonAsync: " + ae.Message);
                _searchedForPerson = _emptyPerson;
            }
        }

        // Each image should contain only 1 detected face; otherwise, must specify face rectangle.
        /// <summary>
        /// Adds PersistedFace's to 'personName'
        /// </summary>
        /// <param name="selectedItems">A collection specifying the file paths of images to be associated with searchedForPerson</param>
        /// <param name="GroupInfos"></param>
        public async Task AddFacesToPersonAsync(
            IList<ImageInfo> selectedItems, ObservableCollection<ImageInfo> GroupInfos)
        {
            if ((_searchedForPerson == null) || (_searchedForPerson == _emptyPerson))
            {
                Debug.WriteLine("AddFacesToPersonAsync, no searchedForPerson");
                return;
            }

            IList<string> faceImagePaths = await GetFaceImagePathsAsync();

            foreach (ImageInfo info in selectedItems)
            {
                string imagePath = info.FilePath;

                // Check for duplicate images
                if (faceImagePaths.Contains(imagePath)) { continue; } // Face already added to Person

                using (FileStream stream = new FileStream(info.FilePath, FileMode.Open))
                {
                    PersistedFace persistedFace =
                        await _faceClient.PersonGroupPerson.AddFaceFromStreamAsync(
                            PersonGroupId, _searchedForPerson.PersonId, stream, imagePath);
                }

                GroupInfos.Add(info);
            }

            // MUST re-query to get updated PersonGroupPerson
            _searchedForPerson = (await _faceClient.PersonGroupPerson.ListAsync(PersonGroupId))[0];

            if (_searchedForPerson.PersistedFaceIds.Count == 0)
            {
                IsPersonGroupTrained = false;
                return;
            }

            await _faceClient.PersonGroup.TrainAsync(PersonGroupId);

            IsPersonGroupTrained = await GetTrainingStatusAsync();
        }

        /// <summary>
        /// Determines whether a given face matches searchedForPerson 
        /// </summary>
        /// <param name="faceId">PersistedFace.PersistedFaceId</param>
        /// <param name="newImage">On success, contains confidence value</param>
        /// <returns>Whether <paramref name="faceId"/> matches searchedForPerson</returns>
        public async Task<bool> MatchFaceAsync(Guid faceId, ImageInfo newImage)
        {
            if ((faceId == Guid.Empty) || (_searchedForPerson?.PersonId == null)) { return false; }

            VerifyResult results;
            try
            {
                results = await _faceClient.Face.VerifyFaceToPersonAsync(
                    faceId, _searchedForPerson.PersonId, PersonGroupId);
                newImage.Confidence = results.Confidence.ToString("P");

            }
            catch (APIErrorException ae)
            {
                Debug.WriteLine("MatchFaceAsync: " + ae.Message);
                return false;
            }

            // TODO: add Confidence slider
            // Default: True if similarity confidence is greater than or equal to 0.5.
            // Can change by specifying VerifyResult.Confidence.
            return results.IsIdentical;
        }

        /// <summary>
        /// Sets 'GroupInfos', which specifies the file paths of images associated with searchedForPerson
        /// </summary>
        /// <param name="GroupInfos">On success, contains image info associated with searchedForPerson</param>
        public async Task DisplayFacesAsync(ObservableCollection<ImageInfo> GroupInfos)
        {
            IList<string> faceImagePaths = await GetFaceImagePathsAsync();
            if (faceImagePaths == Array.Empty<string>()) { return; }

            foreach (string path in faceImagePaths)
            {
                ImageInfo groupInfo = new ImageInfo();
                groupInfo.FilePath = path;
                GroupInfos.Add(groupInfo);
            }
        }

        /// <summary>
        /// Deletes searchedForPerson
        /// </summary>
        /// <param name="GroupInfos"></param>
        /// <param name="GroupNames"></param>
        /// <param name="askFirst">true to display a confirmation dialog</param>
        public async Task DeletePersonAsync(ObservableCollection<ImageInfo> GroupInfos,
            ObservableCollection<string> GroupNames, bool askFirst = true)
        {
            try
            {
                GroupInfos.Clear();
                await _faceClient.PersonGroupPerson.DeleteAsync(PersonGroupId, _searchedForPerson.PersonId);
                string personName = _searchedForPerson.Name.Replace("_", " ");
                if (GroupNames.Contains(personName))
                {
                    GroupNames.Remove(personName);
                    Debug.WriteLine("DeletePersonAsync: " + personName);
                }
                _searchedForPerson = _emptyPerson;
            }
            catch (APIErrorException ae)
            {
                Debug.WriteLine("DeletePersonAsync: " + ae.Message);
            }
            catch (Exception e)
            {
                Debug.WriteLine("DeletePersonAsync: " + e.Message);
            }
        }

        // TODO: add progress indicator
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

        // PersistedFace.UserData stores the associated image file path.
        // Returns the image file paths associated with each PersistedFace
        private async Task<IList<string>> GetFaceImagePathsAsync()
        {
            IList<string> faceImagePaths = new List<string>();

            IList<Guid?> persistedFaceIds = _searchedForPerson.PersistedFaceIds;
            foreach (Guid pfid in persistedFaceIds)
            {
                PersistedFace face = await _faceClient.PersonGroupPerson.GetFaceAsync(
                    PersonGroupId, _searchedForPerson.PersonId, pfid);
                if (!string.IsNullOrEmpty(face.UserData))
                {
                    string imagePath = face.UserData;
                    if (System.IO.File.Exists(imagePath))
                    {
                        faceImagePaths.Add(imagePath);
                        Debug.WriteLine("GetFaceImagePathsAsync: " + imagePath);
                    }
                    else
                    {
                        await _faceClient.PersonGroupPerson.DeleteFaceAsync(PersonGroupId, _searchedForPerson.PersonId, pfid);
                        Debug.WriteLine("GetFaceImagePathsAsync, file not found, deleting reference: " + imagePath);
                    }
                }
            }
            return faceImagePaths;
        }

        private static string ConfigurePersonName(string name)
        {
            return name.Replace(" ", "_");
        }
    }
}
