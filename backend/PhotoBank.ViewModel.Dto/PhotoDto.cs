using System;
using System.Collections.Generic;

namespace PhotoBank.ViewModel.Dto
{
    public class PhotoDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        public int Id { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public required string Name { get; set; }
        public double Scale { get; set; }
        public DateTime? TakenDate { get; set; }
        public string? PreviewUrl { get; set; }
        public GeoPointDto? Location { get; set; }
        public int? Orientation { get; set; }
        public List<FaceDto>? Faces { get; set; }
        public List<string>? Captions { get; set; }
        public List<string>? Tags { get; set; }
        public double AdultScore { get; set; }
        public double RacyScore { get; set; }
        public int Height { get; set;}
        public int Width { get; set; }
    }
}
