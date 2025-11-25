using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Models;
using PhotoBank.Services.ObjectDetection.Abstractions;

namespace PhotoBank.Services.ObjectDetection;

/// <summary>
/// Unified object detection enricher that works with any object detection provider
/// (Azure Computer Vision, YOLO ONNX, etc.) through the IObjectDetectionProvider abstraction.
/// </summary>
public class UnifiedObjectPropertyEnricher : IEnricher
{
    private readonly IObjectDetectionProvider _provider;
    private readonly IRepository<PropertyName> _propertyNameRepository;
    private readonly ILogger<UnifiedObjectPropertyEnricher> _logger;

    public UnifiedObjectPropertyEnricher(
        IObjectDetectionProvider provider,
        IRepository<PropertyName> propertyNameRepository,
        ILogger<UnifiedObjectPropertyEnricher> logger)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _propertyNameRepository = propertyNameRepository ?? throw new ArgumentNullException(nameof(propertyNameRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public EnricherType EnricherType => EnricherType.ObjectProperty;

    // Dependencies depend on the provider type:
    // - Azure provider needs AnalyzeEnricher (uses ImageAnalysis data from SourceDataDto)
    // - YOLO ONNX provider needs PreviewEnricher (uses PreviewImage to detect objects)
    public Type[] Dependencies => _provider.Kind switch
    {
        ObjectDetectionProviderKind.Azure => [typeof(AnalyzeEnricher)],
        ObjectDetectionProviderKind.YoloOnnx => [typeof(PreviewEnricher)],
        _ => []
    };

    public async Task EnrichAsync(Photo photo, SourceDataDto sourceData, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DetectedObjectDto> detectedObjects;

        try
        {
            // Get detected objects from the provider
            detectedObjects = _provider.Kind switch
            {
                ObjectDetectionProviderKind.Azure => GetObjectsFromAzureProvider(sourceData, (float)photo.Scale),
                ObjectDetectionProviderKind.YoloOnnx => GetObjectsFromYoloProvider(sourceData, (float)photo.Scale),
                _ => throw new NotSupportedException($"Provider kind {_provider.Kind} is not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect objects for photo {PhotoId} using provider {ProviderKind}",
                photo.Id, _provider.Kind);
            throw;
        }

        if (detectedObjects.Count == 0)
        {
            _logger.LogDebug("No objects detected for photo {PhotoId} using provider {ProviderKind}",
                photo.Id, _provider.Kind);
            return;
        }

        _logger.LogInformation("Detected {ObjectCount} object(s) for photo {PhotoId} using provider {ProviderKind}",
            detectedObjects.Count, photo.Id, _provider.Kind);

        // Get unique class names
        var classNames = detectedObjects
            .Select(d => d.ClassName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        // Get or create PropertyName entities
        var propertyNameMap = await GetOrCreatePropertyNamesAsync(classNames);

        // Create ObjectProperty entities
        photo.ObjectProperties ??= [];

        foreach (var detectedObject in detectedObjects)
        {
            if (!propertyNameMap.TryGetValue(detectedObject.ClassName, out var propertyName))
            {
                _logger.LogWarning("PropertyName not found for class {ClassName}, skipping", detectedObject.ClassName);
                continue;
            }

            var objectProperty = new ObjectProperty
            {
                PropertyName = propertyName,
                Rectangle = GeoWrapper.CreateRectangleFromOnnxDetection(
                    x: detectedObject.X,
                    y: detectedObject.Y,
                    width: detectedObject.Width,
                    height: detectedObject.Height
                ),
                Confidence = detectedObject.Confidence
            };

            photo.ObjectProperties.Add(objectProperty);
        }
    }

    /// <summary>
    /// Gets detected objects from Azure provider using ImageAnalysis data.
    /// </summary>
    private IReadOnlyList<DetectedObjectDto> GetObjectsFromAzureProvider(SourceDataDto sourceData, float scale)
    {
        if (_provider is not AzureObjectDetectionProvider azureProvider)
        {
            throw new InvalidOperationException(
                $"Provider is {_provider.GetType().Name} but expected AzureObjectDetectionProvider");
        }

        return azureProvider.GetDetectedObjectsFromAnalysis(sourceData.ImageAnalysis, scale);
    }

    /// <summary>
    /// Gets detected objects from YOLO ONNX provider using preview image.
    /// </summary>
    private IReadOnlyList<DetectedObjectDto> GetObjectsFromYoloProvider(SourceDataDto sourceData, float scale)
    {
        if (sourceData.PreviewImage == null)
        {
            _logger.LogDebug("No preview image available for YOLO object detection");
            return [];
        }

        var imageBytes = sourceData.PreviewImage.ToByteArray();
        return _provider.DetectObjects(imageBytes, scale);
    }

    /// <summary>
    /// Gets or creates PropertyName entities for detected object classes.
    /// Implements the same case-insensitive lookup logic as the old enrichers.
    /// </summary>
    private async Task<Dictionary<string, PropertyName>> GetOrCreatePropertyNamesAsync(string[] classNames)
    {
        // Normalize class names to lowercase for case-insensitive comparison
        var normalizedClassNames = classNames.Select(cn => cn.ToLowerInvariant()).ToList();

        // Get existing property names from database (case-insensitive lookup)
        var existingPropertyNames = _propertyNameRepository
            .GetByCondition(p => normalizedClassNames.Contains(p.Name.ToLower()))
            .ToList();

        // Attach existing entities so EF knows they already exist in the database
        foreach (var propertyName in existingPropertyNames)
        {
            _propertyNameRepository.Attach(propertyName);
        }

        var result = existingPropertyNames.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        // Create missing property names
        foreach (var className in classNames)
        {
            if (result.ContainsKey(className))
                continue;

            var newPropertyName = new PropertyName { Name = className };
            await _propertyNameRepository.InsertAsync(newPropertyName);
            // Attach newly inserted entity to ensure it's marked as Unchanged
            _propertyNameRepository.Attach(newPropertyName);
            result[className] = newPropertyName;
        }

        return result;
    }
}
