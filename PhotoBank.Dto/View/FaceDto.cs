using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Newtonsoft.Json;

namespace PhotoBank.Dto.View
{
    public class FaceDto 
    {
        public int Id { get; set; }
        public int? PersonId { get; set; }
        public double? Age { get; set; }
        public bool? Gender { get; set; }
        public string FaceAttributes { get; set; }
        public FaceBoxDto FaceBox { get; set; }

        public string FriendlyFaceAttributes
        {
            get
            {
                if (string.IsNullOrEmpty(FaceAttributes))
                {
                    return "Not available";
                }

                var faceAttributes = JsonConvert.DeserializeObject<FaceAttributes>(FaceAttributes);
                var stringBuilder = new StringBuilder();

                // Get accessories of the faces
                if (faceAttributes.Accessories != null)
                {
                    var accessoriesList = (List<Accessory>)faceAttributes.Accessories;
                    var count = faceAttributes.Accessories.Count;
                    string accessory;
                    var accessoryArray = new string[count];
                    if (count == 0)
                    {
                        accessory = "NoAccessories";
                    }
                    else
                    {
                        for (var i = 0; i < count; ++i)
                        {
                            accessoryArray[i] = accessoriesList[i].Type.ToString();
                        }

                        accessory = string.Join(",", accessoryArray);
                    }

                    stringBuilder.AppendLine($"Accessories : {accessory}<br/>");
                }

                // Get face other attributes
                stringBuilder.AppendLine($"Age : {faceAttributes.Age}<br/>");
                stringBuilder.AppendLine($"Blur : {faceAttributes.Blur?.BlurLevel}<br/>");

                // Get emotion on the face
                if (faceAttributes.Emotion != null)
                {
                    var emotionType = string.Empty;
                    var emotionValue = 0.0;
                    var emotion = faceAttributes.Emotion;
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

                    stringBuilder.AppendLine($"Emotion : {emotionType}<br/>");
                }

                // Get more face attributes
                if (faceAttributes.Exposure != null)
                {
                    stringBuilder.AppendLine($"Exposure : {faceAttributes.Exposure.ExposureLevel}<br/>");
                }

                if (faceAttributes.FacialHair != null)
                {
                    stringBuilder.AppendLine($"FacialHair : {(faceAttributes.FacialHair.Moustache + faceAttributes.FacialHair.Beard + faceAttributes.FacialHair.Sideburns > 0 ? "Yes" : "No")}<br/>");
                }
                stringBuilder.AppendLine($"Gender : {faceAttributes.Gender}<br/>");
                stringBuilder.AppendLine($"Glasses : {faceAttributes.Glasses}<br/>");

                // Get hair color
                if (faceAttributes.Hair != null)
                {
                    var hair = faceAttributes.Hair;
                    string color = null;
                    if (hair.HairColor.Count == 0)
                    {
                        color = hair.Invisible ? "Invisible" : "Bald";
                    }

                    double maxConfidence = 0.0f;
                    foreach (var hairColor in hair.HairColor)
                    {
                        if (hairColor.Confidence <= maxConfidence)
                        {
                            continue;
                        }

                        maxConfidence = hairColor.Confidence;
                        var returnColor = hairColor.Color;
                        color = returnColor.ToString();
                    }

                    stringBuilder.AppendLine($"Hair : {color}<br/>");
                }

                // Get more attributes
                if (faceAttributes.HeadPose != null)
                {
                    stringBuilder.AppendLine($"HeadPose : Pitch: {Math.Round(faceAttributes.HeadPose.Pitch, 2)}, Roll: {Math.Round(faceAttributes.HeadPose.Roll, 2)}, Yaw: {Math.Round(faceAttributes.HeadPose.Yaw, 2)}<br/>");
                }

                if (faceAttributes.HeadPose != null)
                {
                    stringBuilder.AppendLine($"Makeup : {((faceAttributes.Makeup.EyeMakeup || faceAttributes.Makeup.LipMakeup) ? "Yes" : "No")}<br/>");
                }
                if (faceAttributes.Noise != null)
                {
                    stringBuilder.AppendLine($"Noise : {faceAttributes.Noise.NoiseLevel}<br/>");
                }
                if (faceAttributes.Occlusion != null)
                {
                    stringBuilder.AppendLine($"Occlusion : EyeOccluded: {(faceAttributes.Occlusion.EyeOccluded ? "Yes" : "No")} " + $" ForeheadOccluded: {(faceAttributes.Occlusion.ForeheadOccluded ? "Yes" : "No")}   MouthOccluded: {(faceAttributes.Occlusion.MouthOccluded ? "Yes" : "No")}<br/>");
                }
                stringBuilder.AppendLine($"Smile : {faceAttributes.Smile}<br/>");

                return stringBuilder.ToString();
            }
        }
    }
}
