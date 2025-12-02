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

    /// <summary>
    /// Pre-prepared 640x640 letterboxed image for ONNX models (YOLO, NudeNet).
    /// Created once in PreviewEnricher and reused by all ONNX-based enrichers.
    /// </summary>
    public IMagickImage<byte> LetterboxedImage640 { get; set; }

    /// <summary>
    /// Scale factor used for letterboxing to 640x640.
    /// </summary>
    public float LetterboxScale { get; set; }

    /// <summary>
    /// Horizontal padding used for letterboxing to 640x640.
    /// </summary>
    public int LetterboxPadX { get; set; }

    /// <summary>
    /// Vertical padding used for letterboxing to 640x640.
    /// </summary>
    public int LetterboxPadY { get; set; }

    public byte[] ThumbnailImage { get; set; }
    public List<byte[]> FaceImages { get; } = new();

    /// <summary>
    /// If duplicate photo is found during enrichment, stores the ID of existing photo.
    /// Used by DuplicateEnricher to communicate with PhotoProcessor.
    /// </summary>
    public int? DuplicatePhotoId { get; set; }

    /// <summary>
    /// Human-readable information about the duplicate photo.
    /// </summary>
    public string? DuplicatePhotoInfo { get; set; }
}
