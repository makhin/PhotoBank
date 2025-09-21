using System.Collections.Generic;
using System.Linq;
using Amazon.Rekognition.Model;

namespace PhotoBank.UnitTests.Infrastructure.FaceRecognition.Aws;

internal static class RekognitionResponseBuilder
{
    public static ListCollectionsResponse Collections(params string[] collectionIds)
        => new() { CollectionIds = collectionIds.ToList() };

    public static ListUsersResponse Users(params string[] userIds)
        => new()
        {
            Users = userIds.Select(id => new User { UserId = id }).ToList()
        };

    public static CreateCollectionResponse CollectionCreated()
        => new();

    public static CreateUserResponse UserCreated()
        => new();

    public static DeleteUserResponse UserDeleted()
        => new();

    public static ListFacesResponse Faces(params string[] faceIds)
        => new()
        {
            Faces = faceIds.Select(id => new Face { FaceId = id }).ToList()
        };

    public static IndexFacesResponse IndexedFaces(params string[] faceIds)
        => new()
        {
            FaceRecords = faceIds.Select(id => new FaceRecord
            {
                Face = new Face { FaceId = id }
            }).ToList()
        };

    public static AssociateFacesResponse AssociatedFaces(params string[] faceIds)
        => new()
        {
            AssociatedFaces = faceIds.Select(id => new AssociatedFace { FaceId = id }).ToList()
        };

    public static DetectFacesResponse DetectedFaces(params FaceDetail[] faces)
        => new() { FaceDetails = faces.ToList() };

    public static DetectFacesResponse DetectedFaces(IList<FaceDetail> faces)
        => new() { FaceDetails = faces as List<FaceDetail> ?? faces.ToList() };

    public static SearchUsersByImageResponse UserMatches(params (string UserId, float? Similarity)[] matches)
        => new()
        {
            UserMatches = matches.Select(match => new UserMatch
            {
                User = new MatchedUser { UserId = match.UserId },
                Similarity = match.Similarity
            }).ToList()
        };

    public static SearchUsersByImageResponse UserMatches(List<UserMatch> matches)
        => new() { UserMatches = matches };
}
