using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ImageMagick;
using PhotoBank.Services.ImageAnalysis;

namespace PhotoBank.Services.Models;

public class SourceDataDto
{
    [Required]
    public string AbsolutePath { get; set; }

    [Required]
    public ImageAnalysisResult ImageAnalysis { get; set; }
    [Required]
    public IMagickImage<byte> OriginalImage { get; set; }
    [Required]
    public IMagickImage<byte> PreviewImage { get; set; }

    private byte[] _previewImageBytes;

    /// <summary>
    /// Cached byte array representation of PreviewImage.
    /// Computed once and reused across enrichers to avoid multiple ToByteArray() conversions.
    /// </summary>
    public byte[] PreviewImageBytes
    {
        get => _previewImageBytes ??= PreviewImage?.ToByteArray();
        set => _previewImageBytes = value;
    }

    public byte[] ThumbnailImage { get; set; }
    public List<byte[]> FaceImages { get; } = new();
}
