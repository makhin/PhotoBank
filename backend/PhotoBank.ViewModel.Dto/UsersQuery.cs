using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhotoBank.ViewModel.Dto;

public sealed class UsersQuery : IValidatableObject
{
    public const string SortEmail = "email";
    public const string SortPhone = "phone";
    public const string SortTelegram = "telegram";

    private const int DefaultLimit = 50;
    private const int MaxLimit = 200;

    private static readonly HashSet<string> AllowedSorts = new(StringComparer.OrdinalIgnoreCase)
    {
        SortEmail,
        $"-{SortEmail}",
        SortPhone,
        $"-{SortPhone}",
        SortTelegram,
        $"-{SortTelegram}"
    };

    [Range(1, MaxLimit)]
    public int Limit { get; init; } = DefaultLimit;

    [Range(0, int.MaxValue)]
    public int Offset { get; init; }

    public string? Sort { get; init; } = SortEmail;

    public string? Search { get; init; }

    public bool? HasTelegram { get; init; }

    private string SortOrDefault => string.IsNullOrWhiteSpace(Sort) ? SortEmail : Sort;

    public string SortField
    {
        get
        {
            var value = SortOrDefault;
            return (value.StartsWith("-", StringComparison.Ordinal) ? value[1..] : value).ToLowerInvariant();
        }
    }

    public bool SortDescending => SortOrDefault.StartsWith("-", StringComparison.Ordinal);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!AllowedSorts.Contains(SortOrDefault))
        {
            yield return new ValidationResult($"Sort '{Sort}' is not supported.", new[] { nameof(Sort) });
        }
    }
}
