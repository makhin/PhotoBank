using Amazon;
using Amazon.Rekognition;
using Amazon.Runtime;
using Moq;

namespace PhotoBank.UnitTests.Infrastructure.FaceRecognition.Aws;

internal static class RekognitionClientMockFactory
{
    public static Mock<AmazonRekognitionClient> Create()
    {
        return new Mock<AmazonRekognitionClient>(new AnonymousAWSCredentials(), new AmazonRekognitionConfig { RegionEndpoint = RegionEndpoint.USEast1 })
        {
            CallBase = false
        };
    }
}
