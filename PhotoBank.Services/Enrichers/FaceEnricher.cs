﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImageMagick;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Newtonsoft.Json;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto.Load;
using PhotoBank.Repositories;
using Gender = Microsoft.Azure.CognitiveServices.Vision.Face.Models.Gender;
using Person = PhotoBank.DbContext.Models.Person;

namespace PhotoBank.Services.Enrichers
{
    public class FaceEnricher : IEnricher
    {
        private const int MinFaceSize = 36;
        private readonly IFaceService _faceService;
        private readonly List<Person> _persons;

        public FaceEnricher(IFaceService faceService, IRepository<Person> personRepository)
        {
            _faceService = faceService;
            _persons = personRepository.GetAll().ToList();
        }

        public Type[] Dependencies => new[] { typeof(PreviewEnricher), typeof(MetadataEnricher) };

        public bool IsActive => true;

        public async Task Enrich(Photo photo, SourceDataDto sourceData)
        {
            try
            {
                var detectedFaces = await _faceService.DetectFacesAsync(photo.PreviewImage);
                if (!detectedFaces.Any())
                {
                    return;
                }

                photo.PersonFaces = new List<PersonFace>();

                var faceGuids = detectedFaces.Where(IsAbleToIdentify).Select(f => f.FaceId).ToList();
                IList<IdentifyResult> identifyResults = new List<IdentifyResult>();
                if (faceGuids.Any())
                {
                    identifyResults = await _faceService.IdentifyAsync(faceGuids);
                }

                foreach (var detectedFace in detectedFaces)
                {
                    var face = new PersonFace
                    {
                        PhotoId = photo.Id,
                        IdentityStatus = IdentityStatus.NotIdentified,
                        Age = detectedFace.FaceAttributes.Age,
                        Rectangle = GeoWrapper.GetRectangle(detectedFace.FaceRectangle, photo.Scale),
                        Gender = detectedFace.FaceAttributes.Gender == Gender.Male,
                        Smile = detectedFace.FaceAttributes.Smile,
                        FaceAttributes = JsonConvert.SerializeObject(detectedFace.FaceAttributes),
                        Image = await CreateFacePreview(detectedFace, sourceData.PreviewImage, 1)
                    };

                    var identifyResult = identifyResults.SingleOrDefault(f => f.FaceId == detectedFace.FaceId);
                    if (identifyResult != null)
                    {
                        IdentifyFace(face, identifyResult, photo.TakenDate);
                    }
                    else if (IsAbleToIdentify(detectedFace, photo.Scale))
                    {
                        face.Image = await CreateFacePreview(detectedFace, sourceData.OriginalImage, photo.Scale);
                        identifyResult = await _faceService.FaceIdentityAsync(face);
                        IdentifyFace(face, identifyResult, photo.TakenDate);
                    }

                    photo.PersonFaces.Add(face);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static async Task<byte[]> CreateFacePreview(DetectedFace detectedFace, IMagickImage<byte> image, double photoScale)
        {
            await using (var stream = new MemoryStream())
            {
                var faceImage = image.Clone();
                faceImage.Crop(GetMagickGeometry(detectedFace, photoScale));
                await faceImage.WriteAsync(stream);
                return stream.ToArray();
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

        private void IdentifyFace(PersonFace newFace, IdentifyResult identifyResult, DateTime? photoTakenDate)
        {
            foreach (var candidate in identifyResult.Candidates.OrderByDescending(x => x.Confidence))
            {
                var person = _persons.Single(p => p.ExternalGuid == candidate.PersonId);

                if (person.DateOfBirth != null && photoTakenDate > new DateTime(1990, 1, 1) && photoTakenDate < person.DateOfBirth)
                {
                    continue;
                }

                newFace.IdentityStatus = IdentityStatus.Identified;
                newFace.IdentifiedWithConfidence = candidate.Confidence;
                newFace.Person = person;
            }
        }

        private static MagickGeometry GetMagickGeometry(DetectedFace detectedFace, double photoScale)
        {
            var height = (int)(detectedFace.FaceRectangle.Height / photoScale);
            var width = (int)(detectedFace.FaceRectangle.Width / photoScale);
            var top = (int)(detectedFace.FaceRectangle.Top / photoScale);
            var left = (int)(detectedFace.FaceRectangle.Left / photoScale);

            var geometry = new MagickGeometry(width, height)
            {
                IgnoreAspectRatio = true,
                Y = top,
                X = left
            };
            return geometry;
        }
    }
}
