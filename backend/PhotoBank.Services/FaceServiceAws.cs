using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using Face = PhotoBank.DbContext.Models.Face;

namespace PhotoBank.Services
{
    public interface IFaceServiceAws
    {
        Task SyncPersonsAsync();
        Task SyncFacesToPersonAsync();
        Task<List<FaceDetail>> DetectFacesAsync(byte[] image);
        Task<List<UserMatch>> SearchUsersByImageAsync(byte[] image);
    }

    public class FaceServiceAws : IFaceServiceAws
    {
        private readonly AmazonRekognitionClient _faceClient;
        private readonly IRepository<Face> _faceRepository;
        private readonly IRepository<Person> _personRepository;
        private readonly IRepository<PersonGroupFace> _personGroupFaceRepository;
        private readonly ILogger<FaceService> _logger;

        private const string PersonGroupId = "my-cicrle-person-group";

        public FaceServiceAws(AmazonRekognitionClient faceClient,
            IRepository<Face> faceRepository,
            IRepository<Person> personRepository,
            IRepository<PersonGroupFace> personGroupFaceRepository,
            ILogger<FaceService> logger)
        {
            _faceClient = faceClient;
            _faceRepository = faceRepository;
            _personRepository = personRepository;
            _personGroupFaceRepository = personGroupFaceRepository;
            _logger = logger;
        }

        public async Task SyncPersonsAsync()
        {
            try
            {
                var listCollectionsRequest = new ListCollectionsRequest {MaxResults = 1000};
                var listCollectionsResponse = await _faceClient.ListCollectionsAsync(listCollectionsRequest);
                if (listCollectionsResponse.CollectionIds.All(c => c != PersonGroupId))
                {
                    var collectionAsync = await _faceClient.CreateCollectionAsync(new CreateCollectionRequest {CollectionId = PersonGroupId});
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "error");
            }

            var dbPersons = await _personRepository.GetAll().AsNoTracking().ToListAsync();
            var servicePersons = await _faceClient.ListUsersAsync(new ListUsersRequest
            {
                CollectionId = PersonGroupId,
                MaxResults = 500
            });

            foreach (var dbPerson in dbPersons)
            {
                if (servicePersons.Users.Select(u => u.UserId).Contains(dbPerson.Id.ToString()))
                {
                    continue;
                }

                await _faceClient.CreateUserAsync(new CreateUserRequest
                {
                    CollectionId = PersonGroupId,
                    UserId = dbPerson.Id.ToString()
                });
            }

            foreach (var user in servicePersons.Users)
            {
                if (dbPersons.Select(p => p.Id).Contains(int.Parse(user.UserId)))
                {
                    continue;
                }

                await _faceClient.DeleteUserAsync(new DeleteUserRequest
                {
                    CollectionId = PersonGroupId,
                    UserId = user.UserId
                });
            }
        }

        public async Task SyncFacesToPersonAsync()
        {
            var dbPersonGroupFaces = await _personGroupFaceRepository.GetAll().Include(p => p.Person).AsNoTracking().ToListAsync();

            var groupBy = dbPersonGroupFaces.GroupBy(x => new { x.PersonId }, p => new { p.FaceId, p.ExternalGuid },
                (key, g) => new { Key = key, Faces = g.ToList() });

            foreach (var dbPerson in groupBy)
            {
                var listFaces = await _faceClient.ListFacesAsync(new ListFacesRequest
                {
                    CollectionId = PersonGroupId,
                    UserId = dbPerson.Key.PersonId.ToString()
                });

                foreach (var personFace in dbPerson.Faces)
                {
                    if (listFaces.Faces.Select(f=> f.FaceId).Contains(personFace.ExternalGuid.ToString()))
                    {
                        continue;
                    }

                    var dbFace = await _faceRepository.GetAsync(personFace.FaceId);

                    await using (var stream = new MemoryStream(dbFace.Image))
                    {
                        try
                        {
                            var indexFaces =  await _faceClient.IndexFacesAsync(new IndexFacesRequest
                            {
                                CollectionId = PersonGroupId,
                                MaxFaces = 1,
                                Image = new Image
                                {
                                    Bytes = stream
                                },
                                DetectionAttributes = new List<string> { "ALL" }
                            });

                            foreach (var faceRecord in indexFaces.FaceRecords)
                            {
                                var associateFaces  = await _faceClient.AssociateFacesAsync(new AssociateFacesRequest
                                {
                                    CollectionId = PersonGroupId,
                                    UserId = dbPerson.Key.PersonId.ToString(),
                                    FaceIds = new List<string> { faceRecord.Face.FaceId }
                                });

                                var associatedFace = associateFaces.AssociatedFaces.FirstOrDefault();
                                if (associatedFace == null)
                                {
                                    continue;
                                }
                                var personGroupFace = dbPersonGroupFaces.Single(g => g.PersonId == dbPerson.Key.PersonId && g.FaceId == personFace.FaceId);
                                personGroupFace.ExternalGuid = Guid.Parse(associatedFace.FaceId);
                                await _personGroupFaceRepository.UpdateAsync(personGroupFace, pgf => pgf.ExternalGuid);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                }
            }
        }

        public async Task<List<FaceDetail>> DetectFacesAsync(byte[] image)
        {
            var detectFacesRequest = new DetectFacesRequest
            {
                Image = new Image
                {
                    Bytes = new MemoryStream(image)
                },
                Attributes = new List<string> { "ALL" },
            };

            var detectFaces = await _faceClient.DetectFacesAsync(detectFacesRequest);

            if (!detectFaces.FaceDetails.Any())
            {
                return await Task.FromResult(new List<FaceDetail>());
            }

            return detectFaces.FaceDetails;
        }

        public async Task<List<UserMatch>> SearchUsersByImageAsync(byte[] image)
        {
            var users = await _faceClient.SearchUsersByImageAsync(new SearchUsersByImageRequest()
            {
                CollectionId = PersonGroupId,
                Image = new Image
                {
                    Bytes = new MemoryStream(image)
                },
                MaxUsers = 10
            });

            if (users.UserMatches.Any())
            {
                return users.UserMatches;
            }

            return new List<UserMatch>();
        }
    }
}
