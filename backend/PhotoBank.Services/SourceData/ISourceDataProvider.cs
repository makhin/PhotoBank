using System.Threading;
using System.Threading.Tasks;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.SourceData;

/// <summary>
/// Provides source data for photo enrichment from various sources (original file, preview, etc.)
/// </summary>
public interface ISourceDataProvider
{
    /// <summary>
    /// Checks if this provider can supply source data for the given photo
    /// </summary>
    /// <param name="photo">The photo to check</param>
    /// <param name="storage">The storage containing the photo</param>
    /// <returns>True if the provider can supply data, false otherwise</returns>
    bool CanProvideData(Photo photo, Storage storage);

    /// <summary>
    /// Gets the source data for photo enrichment
    /// </summary>
    /// <param name="photo">The photo to get source data for</param>
    /// <param name="storage">The storage containing the photo</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Source data DTO for enrichment pipeline</returns>
    Task<SourceDataDto> GetSourceDataAsync(Photo photo, Storage storage, CancellationToken cancellationToken = default);
}
