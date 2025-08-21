using Amazon.Rekognition;
using Amazon.Runtime;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using PhotoBank.Services.FaceRecognition;
using PhotoBank.Services.FaceRecognition.Abstractions;
using PhotoBank.Services.FaceRecognition.Aws;
using PhotoBank.Services.FaceRecognition.Azure;
using PhotoBank.Services.FaceRecognition.Local;

namespace PhotoBank.UnitTests.FaceRecognition;

[TestFixture]
public class FaceProviderFactoryTests
{
    private ServiceProvider _sp = null!;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();

        services.AddSingleton<LocalInsightFaceProvider>(_ =>
            new LocalInsightFaceProvider(
                client: Mock.Of<ILocalInsightFaceClient>(),
                embeddings: Mock.Of<IFaceEmbeddingRepository>(),
                opts: Options.Create(new LocalInsightFaceOptions()),
                log: Mock.Of<ILogger<LocalInsightFaceProvider>>()));

        services.AddSingleton<AzureFaceProvider>(_ =>
            new AzureFaceProvider(
                client: Mock.Of<Microsoft.Azure.CognitiveServices.Vision.Face.IFaceClient>(),
                opts: Options.Create(new AzureFaceOptions()),
                log: Mock.Of<ILogger<AzureFaceProvider>>()));

        services.AddSingleton<AwsFaceProvider>(_ =>
            new AwsFaceProvider(
                client: new AmazonRekognitionClient(new AnonymousAWSCredentials(), Amazon.RegionEndpoint.USEast1),
                opts: Options.Create(new RekognitionOptions()),
                log: Mock.Of<ILogger<AwsFaceProvider>>()));

        _sp = services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown() => _sp.Dispose();

    [Test]
    public void Get_WithExplicitKind_ReturnsCorrectProvider()
    {
        var opts = Options.Create(new FaceProviderOptions { Default = FaceProviderKind.Local });
        var factory = new FaceProviderFactory(_sp, opts);

        factory.Get(FaceProviderKind.Local).Should().BeOfType<LocalInsightFaceProvider>();
        factory.Get(FaceProviderKind.Azure).Should().BeOfType<AzureFaceProvider>();
        factory.Get(FaceProviderKind.Aws).Should().BeOfType<AwsFaceProvider>();
    }

    [Test]
    public void Get_WithoutKind_UsesDefaultFromOptions()
    {
        var opts = Options.Create(new FaceProviderOptions { Default = FaceProviderKind.Aws });
        var factory = new FaceProviderFactory(_sp, opts);

        factory.Get().Should().BeOfType<AwsFaceProvider>();
    }
}
