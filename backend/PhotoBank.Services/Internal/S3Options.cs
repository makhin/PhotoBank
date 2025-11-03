namespace PhotoBank.Services.Internal;

/// <summary>Опции для S3/MinIO.</summary>
public sealed class S3Options
{
    /// <summary>Имя бакета.</summary>
    public string Bucket { get; set; } = "photobank";

    /// <summary>TTL для presigned URL, сек.</summary>
    public int UrlExpirySeconds { get; set; } = 3600;

    /// <summary>
    /// Если true, медиа-файлы будут проксироваться через /media endpoint.
    /// Если false, будут генерироваться presigned URL-ы напрямую на S3.
    /// </summary>
    public bool UseLocalProxy { get; set; } = true;
}