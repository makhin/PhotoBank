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
        private readonly ILogger<FaceService> _logger;
        private readonly IFaceStorageService _storage;

        private const string PersonGroupId = "my-cicrle-person-group";

        public FaceServiceAws(AmazonRekognitionClient faceClient,
            IRepository<Face> faceRepository,
            IRepository<Person> personRepository,
            IFaceStorageService storage,
            ILogger<FaceService> logger)
        {
            _faceClient = faceClient;
            _faceRepository = faceRepository;
            _personRepository = personRepository;
            _storage = storage;
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
            var faces = await _faceRepository.GetAll()
                .AsNoTracking()
                .Where(f => f.PersonId != null)
                .Select(f => new FaceSyncProjection
                {
                    FaceId = f.Id,
                    PersonId = f.PersonId!.Value,
                    Provider = f.Provider,
                    ExternalId = f.ExternalId,
                    ExternalGuid = f.ExternalGuid,
                    FaceKey = f.S3Key_Image
                })
                .ToListAsync();

            var groupBy = faces
                .GroupBy(x => x.PersonId)
                .Select(g => new { PersonId = g.Key, Faces = g.ToList() });

            foreach (var dbPerson in groupBy)
            {
                var listFaces = await _faceClient.ListFacesAsync(new ListFacesRequest
                {
                    CollectionId = PersonGroupId,
                    UserId = dbPerson.PersonId.ToString()
                });

                var knownProviderIds = listFaces.Faces
                    .Select(f => f.FaceId)
                    .Where(id => !string.IsNullOrEmpty(id))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var faceInfo in dbPerson.Faces)
                {
                    if (!string.IsNullOrEmpty(faceInfo.Provider) && !string.Equals(faceInfo.Provider, "Aws", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(faceInfo.ExternalId) && knownProviderIds.Contains(faceInfo.ExternalId))
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(faceInfo.FaceKey))
                    {
                        continue;
                    }

                    var dbFace = new Face
                    {
                        Id = faceInfo.FaceId,
                        S3Key_Image = faceInfo.FaceKey
                    };

                    await using var stream = await _storage.OpenReadStreamAsync(dbFace);
                    try
                    {
                        var indexFaces =  await _faceClient.IndexFacesAsync(new IndexFacesRequest
                        {
                            CollectionId = PersonGroupId,
                            MaxFaces = 1,
                            Image = new Image
                            {
                                Bytes = (MemoryStream)stream
                            },
                            DetectionAttributes = new List<string> { "ALL" }
                        });

                        foreach (var faceRecord in indexFaces.FaceRecords)
                        {
                            var associateFaces  = await _faceClient.AssociateFacesAsync(new AssociateFacesRequest
                            {
                                CollectionId = PersonGroupId,
                                UserId = dbPerson.PersonId.ToString(),
                                FaceIds = new List<string> { faceRecord.Face.FaceId }
                            });

                            var associatedFace = associateFaces.AssociatedFaces.FirstOrDefault();
                            if (associatedFace == null)
                            {
                                continue;
                            }
                            var providerId = associatedFace.FaceId;
                            faceInfo.ExternalId = providerId;
                            faceInfo.Provider = "Aws";

                            if (Guid.TryParse(providerId, out var parsed))
                            {
                                faceInfo.ExternalGuid = parsed;
                            }

                            knownProviderIds.Add(providerId);

                            await _faceRepository.UpdateAsync(new Face
                            {
                                Id = faceInfo.FaceId,
                                Provider = faceInfo.Provider,
                                ExternalId = faceInfo.ExternalId,
                                ExternalGuid = faceInfo.ExternalGuid
                            }, f => f.Provider, f => f.ExternalId, f => f.ExternalGuid);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
        }

        private sealed class FaceSyncProjection
        {
            public int FaceId { get; init; }
            public int PersonId { get; init; }
            public string? Provider { get; set; }
            public string? ExternalId { get; set; }
            public Guid ExternalGuid { get; set; }
            public string? FaceKey { get; init; }
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
