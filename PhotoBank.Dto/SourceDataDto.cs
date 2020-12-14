﻿using System.Drawing;
using System.IO;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace PhotoBank.Dto
{
    public class SourceDataDto
    {
        public string Path { get; set; }

        public byte[] Image { get; set; }

        public ImageAnalysis ImageAnalysis { get; set; }
    }
}