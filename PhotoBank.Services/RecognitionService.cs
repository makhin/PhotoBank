using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace PhotoBank.Services
{
    public interface IRecognitionService
    {
        Task DetectFaceExtract(Stream stream, string imageFileName);
        Task FindSimilar(string url);
        Task IdentifyInPersonGroup(string url);
    }

    public class RecognitionService : IRecognitionService
    {
        private readonly IFaceClient _client;
        const string RecognitionModel3 = RecognitionModel.Recognition03;

        public RecognitionService(IFaceClient client)
        {
            _client = client;

            _client.LargeFaceList.CreateAsync("All_faces", null, null, RecognitionModel3);
        }

        /* 
         * DETECT FACES
         * Detects features from faces and IDs them.
         */
        public async Task DetectFaceExtract(Stream stream, string imageFileName)
        {
            Console.WriteLine("========DETECT FACES========");
            Console.WriteLine();

                IList<DetectedFace> detectedFaces;

                try
                {
                    // Detect faces with all attributes from image url.
                    detectedFaces = await _client.Face.DetectWithStreamAsync(stream,
                        returnFaceAttributes: new List<FaceAttributeType?>
                        {
                            FaceAttributeType.Accessories, FaceAttributeType.Age,
                            FaceAttributeType.Blur, FaceAttributeType.Emotion, FaceAttributeType.Exposure,
                            FaceAttributeType.FacialHair,
                            FaceAttributeType.Gender, FaceAttributeType.Glasses, FaceAttributeType.Hair,
                            FaceAttributeType.HeadPose,
                            FaceAttributeType.Makeup, FaceAttributeType.Noise, FaceAttributeType.Occlusion,
                            FaceAttributeType.Smile
                        },
                        // We specify detection model 1 because we are retrieving attributes.
                        detectionModel: DetectionModel.Detection01,
                        recognitionModel: RecognitionModel3);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }

                Console.WriteLine($"{detectedFaces.Count} face(s) detected from image `{imageFileName}`.");

                // Parse and print all attributes of each detected face.
                foreach (var face in detectedFaces)
                {
                    Console.WriteLine($"Face attributes for {imageFileName}:");

                    // GetAsync bounding box of the faces
                    Console.WriteLine(
                        $"Rectangle(Left/Top/Width/Height) : {face.FaceRectangle.Left} {face.FaceRectangle.Top} {face.FaceRectangle.Width} {face.FaceRectangle.Height}");

                    // GetAsync accessories of the faces
                    List<Accessory> accessoriesList = (List<Accessory>) face.FaceAttributes.Accessories;
                    int count = face.FaceAttributes.Accessories.Count;
                    string accessory;
                    string[] accessoryArray = new string[count];
                    if (count == 0)
                    {
                        accessory = "NoAccessories";
                    }
                    else
                    {
                        for (int i = 0; i < count; ++i)
                        {
                            accessoryArray[i] = accessoriesList[i].Type.ToString();
                        }

                        accessory = string.Join(",", accessoryArray);
                    }

                    Console.WriteLine($"Accessories : {accessory}");

                    // GetAsync face other attributes
                    Console.WriteLine($"Age : {face.FaceAttributes.Age}");
                    Console.WriteLine($"Blur : {face.FaceAttributes.Blur.BlurLevel}");

                    // GetAsync emotion on the face
                    string emotionType = string.Empty;
                    double emotionValue = 0.0;
                    Emotion emotion = face.FaceAttributes.Emotion;
                    if (emotion.Anger > emotionValue)
                    {
                        emotionValue = emotion.Anger;
                        emotionType = "Anger";
                    }

                    if (emotion.Contempt > emotionValue)
                    {
                        emotionValue = emotion.Contempt;
                        emotionType = "Contempt";
                    }

                    if (emotion.Disgust > emotionValue)
                    {
                        emotionValue = emotion.Disgust;
                        emotionType = "Disgust";
                    }

                    if (emotion.Fear > emotionValue)
                    {
                        emotionValue = emotion.Fear;
                        emotionType = "Fear";
                    }

                    if (emotion.Happiness > emotionValue)
                    {
                        emotionValue = emotion.Happiness;
                        emotionType = "Happiness";
                    }

                    if (emotion.Neutral > emotionValue)
                    {
                        emotionValue = emotion.Neutral;
                        emotionType = "Neutral";
                    }

                    if (emotion.Sadness > emotionValue)
                    {
                        emotionValue = emotion.Sadness;
                        emotionType = "Sadness";
                    }

                    if (emotion.Surprise > emotionValue)
                    {
                        emotionType = "Surprise";
                    }

                    Console.WriteLine($"Emotion : {emotionType}");

                    // GetAsync more face attributes
                    Console.WriteLine($"Exposure : {face.FaceAttributes.Exposure.ExposureLevel}");
                    Console.WriteLine(
                        $"FacialHair : {string.Format("{0}", face.FaceAttributes.FacialHair.Moustache + face.FaceAttributes.FacialHair.Beard + face.FaceAttributes.FacialHair.Sideburns > 0 ? "Yes" : "No")}");
                    Console.WriteLine($"Gender : {face.FaceAttributes.Gender}");
                    Console.WriteLine($"Glasses : {face.FaceAttributes.Glasses}");

                    // GetAsync hair color
                    Hair hair = face.FaceAttributes.Hair;
                    string color = null;
                    if (hair.HairColor.Count == 0)
                    {
                        if (hair.Invisible)
                        {
                            color = "Invisible";
                        }
                        else
                        {
                            color = "Bald";
                        }
                    }

                    HairColorType returnColor = HairColorType.Unknown;
                    double maxConfidence = 0.0f;
                    foreach (HairColor hairColor in hair.HairColor)
                    {
                        if (hairColor.Confidence <= maxConfidence)
                        {
                            continue;
                        }

                        maxConfidence = hairColor.Confidence;
                        returnColor = hairColor.Color;
                        color = returnColor.ToString();
                    }

                    Console.WriteLine($"Hair : {color}");

                    // GetAsync more attributes
                    Console.WriteLine(
                        $"HeadPose : {string.Format("Pitch: {0}, Roll: {1}, Yaw: {2}", Math.Round(face.FaceAttributes.HeadPose.Pitch, 2), Math.Round(face.FaceAttributes.HeadPose.Roll, 2), Math.Round(face.FaceAttributes.HeadPose.Yaw, 2))}");
                    Console.WriteLine(
                        $"Makeup : {string.Format("{0}", (face.FaceAttributes.Makeup.EyeMakeup || face.FaceAttributes.Makeup.LipMakeup) ? "Yes" : "No")}");
                    Console.WriteLine($"Noise : {face.FaceAttributes.Noise.NoiseLevel}");
                    Console.WriteLine(
                        $"Occlusion : {string.Format("EyeOccluded: {0}", face.FaceAttributes.Occlusion.EyeOccluded ? "Yes" : "No")} " +
                        $" {string.Format("ForeheadOccluded: {0}", face.FaceAttributes.Occlusion.ForeheadOccluded ? "Yes" : "No")}   {string.Format("MouthOccluded: {0}", face.FaceAttributes.Occlusion.MouthOccluded ? "Yes" : "No")}");
                    Console.WriteLine($"Smile : {face.FaceAttributes.Smile}");
                    Console.WriteLine();

                }
        }

        private async Task<List<DetectedFace>> DetectFaceRecognize(IFaceClient faceClient, string url, string recognition_model)
        {
            // Detect faces from image URL. Since only recognizing, use the recognition model 1.
            // We use detection model 2 because we are not retrieving attributes.
            IList<DetectedFace> detectedFaces = await faceClient.Face.DetectWithUrlAsync(url, recognitionModel: recognition_model, detectionModel: DetectionModel.Detection02);
            Console.WriteLine($"{detectedFaces.Count} face(s) detected from image `{Path.GetFileName(url)}`");
            return detectedFaces.ToList();
        }

        /*
         * FIND SIMILAR
         * This example will take an image and find a similar one to it in another image.
         */
        public async Task FindSimilar(string url)
        {
            Console.WriteLine("========FIND SIMILAR========");
            Console.WriteLine();

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
            IList<Guid?> targetFaceIds = new List<Guid?>();
            foreach (var targetImageFileName in targetImageFileNames)
            {
                // Detect faces from target image url.
                var faces = await DetectFaceRecognize(_client, $"{url}{targetImageFileName}", RecognitionModel3);
                // Add detected faceId to list of GUIDs.
                targetFaceIds.Add(faces[0].FaceId.Value);
            }

            // Detect faces from source image url.
            IList<DetectedFace> detectedFaces =
                await DetectFaceRecognize(_client, $"{url}{sourceImageFileName}", RecognitionModel3);
            Console.WriteLine();

            // Find a similar face(s) in the list of IDs. Comapring only the first in list for testing purposes.
            IList<SimilarFace> similarResults =
                await _client.Face.FindSimilarAsync(detectedFaces[0].FaceId.Value, null, null, targetFaceIds);

            foreach (var similarResult in similarResults)
            {
                Console.WriteLine(
                    $"Faces from {sourceImageFileName} & ID:{similarResult.FaceId} are similar with confidence: {similarResult.Confidence}.");
            }

            Console.WriteLine();
        }

        static string personGroupId = Guid.NewGuid().ToString();

        public async Task IdentifyInPersonGroup(string url)
        {
            Console.WriteLine("========IDENTIFY FACES========");
            Console.WriteLine();

            // Create a dictionary for all your images, grouping similar ones under the same key.
            Dictionary<string, string[]> personDictionary =
                new Dictionary<string, string[]>
                {
                    {"Family1-Dad", new[] {"Family1-Dad1.jpg", "Family1-Dad2.jpg"}},
                    {"Family1-Mom", new[] {"Family1-Mom1.jpg", "Family1-Mom2.jpg"}},
                    {"Family1-Son", new[] {"Family1-Son1.jpg", "Family1-Son2.jpg"}},
                    {"Family1-Daughter", new[] {"Family1-Daughter1.jpg", "Family1-Daughter2.jpg"}},
                    {"Family2-Lady", new[] {"Family2-Lady1.jpg", "Family2-Lady2.jpg"}},
                    {"Family2-Man", new[] {"Family2-Man1.jpg", "Family2-Man2.jpg"}}
                };
            // A group photo that includes some of the persons you seek to identify from your dictionary.
            string sourceImageFileName = "identification1.jpg";


            // Create a person group. 
            Console.WriteLine($"Create a person group ({personGroupId}).");
            await _client.PersonGroup.CreateAsync(personGroupId, personGroupId, recognitionModel: RecognitionModel3);
            // The similar faces will be grouped into a single person group person.
            foreach (var groupedFace in personDictionary.Keys)
            {
                // Limit TPS
                await Task.Delay(250);
                Person person =
                    await _client.PersonGroupPerson.CreateAsync(personGroupId: personGroupId, name: groupedFace);
                Console.WriteLine($"Create a person group person '{groupedFace}'.");

                // Add face to the person group person.
                foreach (var similarImage in personDictionary[groupedFace])
                {
                    Console.WriteLine(
                        $"Add face to the person group person({groupedFace}) from image `{similarImage}`");
                    PersistedFace face = await _client.PersonGroupPerson.AddFaceFromUrlAsync(personGroupId,
                        person.PersonId,
                        $"{url}{similarImage}", similarImage);
                }
            }

            // Start to train the person group.
            Console.WriteLine();
            Console.WriteLine($"Train person group {personGroupId}.");

            await _client.PersonGroup.TrainAsync(personGroupId);

            // Wait until the training is completed.
            while (true)
            {
                await Task.Delay(1000);
                var trainingStatus = await _client.PersonGroup.GetTrainingStatusAsync(personGroupId);
                Console.WriteLine($"Training status: {trainingStatus.Status}.");
                if (trainingStatus.Status == TrainingStatusType.Succeeded) { break; }
            }
            Console.WriteLine();

            List<Guid?> sourceFaceIds = new List<Guid?>();
            // Detect faces from source image url.
            List<DetectedFace> detectedFaces = await DetectFaceRecognize(_client, $"{url}{sourceImageFileName}", RecognitionModel3);

            // Add detected faceId to sourceFaceIds.
            foreach (var detectedFace in detectedFaces) { sourceFaceIds.Add(detectedFace.FaceId.Value); }

            // Identify the faces in a person group. 
            var identifyResults = await _client.Face.IdentifyAsync(sourceFaceIds, personGroupId);

            foreach (var identifyResult in identifyResults)
            {
                Person person = await _client.PersonGroupPerson.GetAsync(personGroupId, identifyResult.Candidates[0].PersonId);
                Console.WriteLine($"Person '{person.Name}' is identified for face in: {sourceImageFileName} - {identifyResult.FaceId}," +
                                  $" confidence: {identifyResult.Candidates[0].Confidence}.");
            }
            Console.WriteLine();
        }
    }
}
