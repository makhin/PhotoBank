using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;
using PhotoBank.Repositories;
using PhotoBank.Services.Api;
using PhotoBank.DbContext.Models;
using Microsoft.EntityFrameworkCore.Query;

namespace PhotoBank.Services;

public interface IS3ResourceService
{
    Task<PhotoPreviewResult?> GetAsync<TEntity>(
        IRepository<TEntity> repository,
        int id,
        Expression<Func<TEntity, string?>> keySelector,
        Expression<Func<TEntity, string>> etagSelector,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryCustomizer = null)
        where TEntity : class, IEntityBase, new();
}

public class S3ResourceService : IS3ResourceService
{
    private readonly IMinioClient _minioClient;

    public S3ResourceService(IMinioClient minioClient)
    {
        _minioClient = minioClient;
    }

    public async Task<PhotoPreviewResult?> GetAsync<TEntity>(
        IRepository<TEntity> repository,
        int id,
        Expression<Func<TEntity, string?>> keySelector,
        Expression<Func<TEntity, string>> etagSelector,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryCustomizer = null)
        where TEntity : class, IEntityBase, new()
    {
        var keyName = GetPropertyName(keySelector);
        var etagName = GetPropertyName(etagSelector);

        S3Info? info;
        var baseQuery = repository.GetAll().AsNoTracking();

        if (queryCustomizer != null)
        {
            baseQuery = queryCustomizer(baseQuery);
        }

        baseQuery = baseQuery.Where(e => e.Id == id);
        if (baseQuery.Provider is IAsyncQueryProvider)
        {
            info = await baseQuery
                .Select(e => new S3Info(
                    EF.Property<string?>(e, keyName),
                    EF.Property<string>(e, etagName)))
                .SingleOrDefaultAsync();
        }
        else
        {
            var entity = baseQuery.SingleOrDefault();
            info = entity == null ? null : new S3Info(keySelector.Compile()(entity), etagSelector.Compile()(entity));
        }

        if (info == null || string.IsNullOrEmpty(info.Key))
            return null;

        var url = await GetPresignedUrlAsync(info.Key);
        if (url != null)
            return new PhotoPreviewResult(info.ETag, url, null);

        var data = await GetObjectAsync(info.Key);
        return new PhotoPreviewResult(info.ETag, null, data);
    }

    private static string GetPropertyName<T, TProp>(Expression<Func<T, TProp>> expr)
    {
        return expr.Body switch
        {
            MemberExpression m => m.Member.Name,
            UnaryExpression u when u.Operand is MemberExpression m => m.Member.Name,
            _ => throw new ArgumentException("Invalid expression", nameof(expr))
        };
    }

    protected virtual async Task<string?> GetPresignedUrlAsync(string key)
    {
        try
        {
            return await _minioClient.PresignedGetObjectAsync(new PresignedGetObjectArgs()
                .WithBucket("photobank")
                .WithObject(key)
                .WithExpiry(60 * 60));
        }
        catch
        {
            return null;
        }
    }

    protected virtual async Task<byte[]> GetObjectAsync(string key)
    {
        using var ms = new MemoryStream();
        await _minioClient.GetObjectAsync(new GetObjectArgs()
            .WithBucket("photobank")
            .WithObject(key)
            .WithCallbackStream(s => s.CopyTo(ms)));
        return ms.ToArray();
    }

    private record S3Info(string? Key, string ETag);
}
