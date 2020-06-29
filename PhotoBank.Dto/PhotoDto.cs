﻿using System;
using System.Text.Json.Serialization;

namespace PhotoBank.Dto
{
    public class PhotoDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime? TakenDate { get; set; }
        public byte[] PreviewImage { get; set; }
    }
}
