using System;
using ImageMagick;

namespace PhotoBank.Services;

public static class ImageHashHelper
{
    public static string ComputeHash(byte[] data)
    {
        using var image = new MagickImage(data);
        var hash = image.PerceptualHash();
        return hash.ToString();
    }

    public static double HammingDistance(string hash1, string hash2)
    {
        if (string.IsNullOrEmpty(hash1) || string.IsNullOrEmpty(hash2))
            return double.MaxValue;

        var h1 = new PerceptualHash(hash1);
        var h2 = new PerceptualHash(hash2);
        return h1.SumSquaredDistance(h2);
    }
}
