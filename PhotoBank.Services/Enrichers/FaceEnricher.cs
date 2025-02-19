﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Newtonsoft.Json;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Enrichers.Services;
using PhotoBank.Services.Models;
using Gender = Microsoft.Azure.CognitiveServices.Vision.Face.Models.Gender;
using Person = PhotoBank.DbContext.Models.Person;

namespace PhotoBank.Services.Enrichers
{
    public class FaceEnricher : IEnricher
    {
        private const int MinFaceSize = 36;
        private readonly IFaceService _faceService;
        private readonly IFacePreviewService _facePreviewService;
        private readonly List<Person> _persons;

        public FaceEnricher(IFaceService faceService, IRepository<Person> personRepository, IFacePreviewService facePreviewService)
        {
            _faceService = faceService;
            _facePreviewService = facePreviewService;
            _persons = personRepository.GetAll().ToList();
        }

        public EnricherType EnricherType => EnricherType.Face;
        public Type[] Dependencies => new[] { typeof(PreviewEnricher), typeof(MetadataEnricher) };

        public async Task EnrichAsync(Photo photo, SourceDataDto sourceData)
        {
            try
            {
                var detectedFaces = await _faceService.DetectFacesAsync(photo.PreviewImage);
                if (!detectedFaces.Any())
                {
                    photo.FaceIdentifyStatus = FaceIdentifyStatus.NotDetected;
                    return;
                }

                photo.FaceIdentifyStatus = FaceIdentifyStatus.Detected;
                photo.Faces = new List<Face>();

                var faceGuids = detectedFaces.Where(IsAbleToIdentify).Select(f => f.FaceId).ToList();
                IList<IdentifyResult> identifyResults = new List<IdentifyResult>();
                if (faceGuids.Count != 0)
                {
                    identifyResults = await _faceService.IdentifyAsync(faceGuids);
                }

                foreach (var detectedFace in detectedFaces)
                {
                    var face = new Face
                    {
                        PhotoId = photo.Id,
                        IdentityStatus = IdentityStatus.NotIdentified,
                        Image = await _facePreviewService.CreateFacePreview(detectedFace, sourceData.PreviewImage, 1),
                        Rectangle = GeoWrapper.GetRectangle(detectedFace.FaceRectangle, photo.Scale)
                    };

                    var attributes = detectedFace.FaceAttributes;
                    if (attributes != null)
                    {
                        face.Age = attributes.Age;
                        face.Gender = attributes.Gender == Gender.Male;
                        face.Smile = attributes.Smile;
                        face.FaceAttributes = JsonConvert.SerializeObject(attributes);
                    }

                    var identifyResult = identifyResults.SingleOrDefault(f => f.FaceId == detectedFace.FaceId);
                    if (identifyResult != null)
                    {
                        IdentifyFace(face, identifyResult, photo.TakenDate);
                    }
                    else if (IsAbleToIdentify(detectedFace, photo.Scale))
                    {
                        face.Image = await _facePreviewService.CreateFacePreview(detectedFace, sourceData.OriginalImage, photo.Scale);
                        identifyResult = await _faceService.FaceIdentityAsync(face);
                        IdentifyFace(face, identifyResult, photo.TakenDate);
                    }

                    photo.Faces.Add(face);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static bool IsAbleToIdentify(DetectedFace detectedFace)
        {
            return IsAbleToIdentify(detectedFace, 1);
        }

        private static bool IsAbleToIdentify(DetectedFace detectedFace, in double scale)
        {
            return Math.Round(detectedFace.FaceRectangle.Height / scale) >= MinFaceSize && Math.Round(detectedFace.FaceRectangle.Width / scale) >= MinFaceSize;
        }

        private void IdentifyFace(Face face, IdentifyResult identifyResult, DateTime? photoTakenDate)
        {
            foreach (var candidate in identifyResult.Candidates.OrderByDescending(x => x.Confidence))
            {
                var person = _persons.Single(p => p.ExternalGuid == candidate.PersonId);

                if (person.DateOfBirth != null && photoTakenDate > new DateTime(1990, 1, 1) && photoTakenDate < person.DateOfBirth)
                {
                    continue;
                }

                face.IdentityStatus = IdentityStatus.Identified;
                face.IdentifiedWithConfidence = candidate.Confidence;
                face.Person = person;
            }
        }
    }
}
