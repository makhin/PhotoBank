using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Enrichers.Onnx;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichers;

/// <summary>
/// Enricher that uses YOLO ONNX model for object detection
/// </summary>
public class OnnxObjectDetectionEnricher : IEnricher
{
    private readonly IRepository<PropertyName> _propertyNameRepository;
    private readonly IYoloOnnxService _yoloService;
    private readonly float _confidenceThreshold;
    private readonly float _nmsThreshold;

    private static readonly Type[] s_dependencies = { typeof(PreviewEnricher) };

    public OnnxObjectDetectionEnricher(
        IRepository<PropertyName> propertyNameRepository,
        IYoloOnnxService yoloService,
        IOptions<YoloOnnxOptions> options)
    {
        _propertyNameRepository = propertyNameRepository ?? throw new ArgumentNullException(nameof(propertyNameRepository));
        _yoloService = yoloService ?? throw new ArgumentNullException(nameof(yoloService));

        if (options == null) throw new ArgumentNullException(nameof(options));

        // Read thresholds from configuration
        _confidenceThreshold = options.Value.ConfidenceThreshold;
        _nmsThreshold = options.Value.NmsThreshold;
    }

    public EnricherType EnricherType => EnricherType.ObjectProperty;

    public Type[] Dependencies => s_dependencies;

    public async Task EnrichAsync(Photo photo, SourceDataDto sourceData, CancellationToken cancellationToken = default)
    {
        if (photo is null) throw new ArgumentNullException(nameof(photo));
        if (sourceData is null) throw new ArgumentNullException(nameof(sourceData));

        if (sourceData.PreviewImage is null)
            return;

        // Convert IMagickImage to byte array
        var imageBytes = sourceData.PreviewImage.ToByteArray();

        // Detect objects using YOLO
        var detectedObjects = _yoloService.DetectObjects(imageBytes, _confidenceThreshold, _nmsThreshold);

        if (detectedObjects.Count == 0)
            return;

        // Get unique class names
        var classNames = detectedObjects
            .Select(d => d.ClassName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        // Get or create PropertyName entities
        var propertyNameMap = await GetOrCreatePropertyNamesAsync(classNames, cancellationToken);

        // Create ObjectProperty entities
        photo.ObjectProperties ??= new List<ObjectProperty>();

        foreach (var detectedObject in detectedObjects)
        {
            if (!propertyNameMap.TryGetValue(detectedObject.ClassName, out var propertyName))
                continue;

            var objectProperty = new ObjectProperty
            {
                PropertyName = propertyName,
                Rectangle = GeoWrapper.CreateRectangleFromOnnxDetection(
                    x: (int)(detectedObject.X / photo.Scale),
                    y: (int)(detectedObject.Y / photo.Scale),
                    width: (int)(detectedObject.Width / photo.Scale),
                    height: (int)(detectedObject.Height / photo.Scale)
                ),
                Confidence = detectedObject.Confidence
            };

            photo.ObjectProperties.Add(objectProperty);
        }
    }

    private async Task<Dictionary<string, PropertyName>> GetOrCreatePropertyNamesAsync(
        string[] classNames,
        CancellationToken cancellationToken)
    {
        // Get existing property names from database
        var existingPropertyNames = _propertyNameRepository
            .GetByCondition(p => classNames.Contains(p.Name))
            .ToList();

        var result = existingPropertyNames.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        // Create missing property names
        foreach (var className in classNames)
        {
            if (result.ContainsKey(className))
                continue;

            var newPropertyName = new PropertyName { Name = className };
            await _propertyNameRepository.InsertAsync(newPropertyName);
            result[className] = newPropertyName;
        }

        return result;
    }
}
