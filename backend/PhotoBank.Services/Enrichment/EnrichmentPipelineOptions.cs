namespace PhotoBank.Services.Enrichment;

/// <summary>
/// Pipeline execution options.
/// </summary>
public sealed class EnrichmentPipelineOptions
{
    /// <summary>Continue executing subsequent enrichers if one fails (default: false).</summary>
    public bool ContinueOnError { get; init; } = false;

    /// <summary>Log timing for each step (default: true).</summary>
    public bool LogTimings { get; init; } = true;

    /// <summary>Max parallelism for batch runs. null or <=0 means use Environment.ProcessorCount.</summary>
    public int? MaxDegreeOfParallelism { get; init; }
}

