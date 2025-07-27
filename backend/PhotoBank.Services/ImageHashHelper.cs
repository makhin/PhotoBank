using System;
using ImageMagick;

namespace PhotoBank.Services;

public static class ImageHashHelper
{
    public static string ComputeHash(byte[] data)
    {
        using var image = new MagickImage(data);
        image.Resize(8, 8);
        image.ColorSpace = ColorSpace.Gray;

        var pixels = image.GetPixels();
        ulong total = 0;
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                total += pixels.GetPixel(x, y).GetChannel(0);
            }
        }

        ulong avg = total / 64;
        ulong hash = 0;
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                if (pixels.GetPixel(x, y).GetChannel(0) >= avg)
                {
                    hash |= 1UL << (y * 8 + x);
                }
            }
        }

        return hash.ToString("X16");
    }

    public static int HammingDistance(string hash1, string hash2)
    {
        if (string.IsNullOrEmpty(hash1) || string.IsNullOrEmpty(hash2))
            return int.MaxValue;
        ulong h1 = Convert.ToUInt64(hash1, 16);
        ulong h2 = Convert.ToUInt64(hash2, 16);
        ulong x = h1 ^ h2;
        int setBits = 0;
        while (x != 0)
        {
            setBits += (int)(x & 1);
            x >>= 1;
        }
        return setBits;
    }
}
