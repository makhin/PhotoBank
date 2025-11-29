using System;
using ImageMagick;

namespace PhotoBank.Services;

public static class ImageHashHelper
{
    public static string ComputeHash(IMagickImage<byte> image)
    {
        if (image == null)
            throw new ArgumentNullException(nameof(image));

        var hash = image.PerceptualHash();
        return hash.ToString();
    }

    public static double HammingDistance(string hash1, string hash2)
    {
        if (string.IsNullOrEmpty(hash1) || string.IsNullOrEmpty(hash2))
            return double.MaxValue;

        var h1 = new PerceptualHash(hash1);
        return HammingDistance(h1, hash2);
    }

    public static double HammingDistance(PerceptualHash referenceHash, string? hash2)
    {
        if (referenceHash == null || string.IsNullOrEmpty(hash2))
            return double.MaxValue;

        var h2 = new PerceptualHash(hash2);
        return referenceHash.SumSquaredDistance(h2);
    }
}
