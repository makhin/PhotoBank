using System.Net.Http;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using PhotoBank.Services.FaceRecognition.Azure;

namespace PhotoBank.UnitTests.Infrastructure.FaceRecognition;

internal static class AzureFaceClientFactory
{
    public static IFaceClient Create(AzureFaceOptions options, HttpMessageHandler handler)
    {
        var client = new FaceClient(new ApiKeyServiceClientCredentials(options.Key), new HttpClient(handler), true)
        {
            Endpoint = options.Endpoint
        };

        return client;
    }
}
