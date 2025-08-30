using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace PhotoBank.Services;

internal static class S3KeyBuilder
{
    public static string BuildFaceKey(long id)
        => $"faces/{id:0000000000}.jpg";

    public static string BuildPreviewKey(string storageNameOrCode, string? relativePath, long id)
        => BuildPhotoScopedKey("preview", storageNameOrCode, relativePath, $"{id:0000000000}_preview.jpg");

    public static string BuildThumbnailKey(string storageNameOrCode, string? relativePath, long id)
        => BuildPhotoScopedKey("thumbnail", storageNameOrCode, relativePath, $"{id:0000000000}_thumbnail.jpg");

    private static string BuildPhotoScopedKey(string scope, string storageNameOrCode, string? relativePath, string fileName)
    {
        var storage = SlugifySegment(storageNameOrCode);
        var rel = (relativePath ?? string.Empty).Replace('\\', '/');

        var segments = rel.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                          .Select(SlugifySegment)
                          .Where(s => s.Length > 0)
                          .ToList();

        var basePrefix = new StringBuilder(scope).Append('/').Append(storage);
        if (segments.Count > 0) basePrefix.Append('/').Append(string.Join('/', segments));

        var key = $"{basePrefix}/{fileName}";

        if (Encoding.UTF8.GetByteCount(key) > 1024)
        {
            var hash8 = ShortHash(key);
            var shortened = ShortenPath(basePrefix.ToString(), fileName, 1024 - (1 + hash8.Length));
            key = $"{shortened}-{hash8}";
        }
        return key;
    }

    private static string SlugifySegment(string value)
    {
        var src = value.Trim().Normalize(NormalizationForm.FormKC);
        src = new string(src.Where(ch => !char.IsControl(ch)).ToArray());
        src = Regex.Replace(src, @"[^\p{L}\p{Nd}\-_.]+", "-");
        src = Regex.Replace(src, @"-+", "-").Trim('-');
        if (src.Length > 100) src = src[..100];
        return src.Length == 0 ? "_" : src;
    }

    private static string ShortHash(string s)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
        return Convert.ToHexString(bytes, 0, 4).ToLowerInvariant();
    }

    private static string ShortenPath(string prefix, string fileName, int limitBytes)
    {
        var segs = prefix.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (segs.Count == 0) return prefix;

        for (int i = 1; i < segs.Count; i++)
        {
            while (Encoding.UTF8.GetByteCount(string.Join('/', segs) + "/" + fileName) > limitBytes && segs[i].Length > 6)
            {
                segs[i] = segs[i][..Math.Max(3, segs[i].Length - 1)];
            }
            if (Encoding.UTF8.GetByteCount(string.Join('/', segs) + "/" + fileName) <= limitBytes) break;
        }
        return string.Join('/', segs);
    }
}
