using System;

namespace PhotoBank.Services.Enrichment;

/// <summary>
/// Optional attribute for declaring dependencies between enrichers.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class DependsOnAttribute : Attribute
{
    public DependsOnAttribute(Type enricherType) => EnricherType = enricherType;
    public Type EnricherType { get; }
}

