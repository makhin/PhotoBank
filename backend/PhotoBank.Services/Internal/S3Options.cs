namespace PhotoBank.Services.Internal;

/// <summary>Опции для S3/MinIO.</summary>
public sealed class S3Options
{
    /// <summary>Имя бакета.</summary>
    public string Bucket { get; set; } = "photobank";

    /// <summary>TTL для presigned URL, сек.</summary>
    public int UrlExpirySeconds { get; set; } = 3600;

    /// <summary>Публичный URL для доступа к MinIO через Nginx (например, https://makhin.ddns.net/s3).</summary>
    public string? PublicUrl { get; set; }
}