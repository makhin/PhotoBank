using System;
using System.Collections.Generic;
using System.Linq;

namespace PhotoBank.AccessControl;

public sealed class CurrentUser : ICurrentUser
{
    private CurrentUser(
        Guid userId,
        bool isAdmin,
        IReadOnlySet<int> allowedStorageIds,
        IReadOnlySet<int> allowedPersonGroupIds,
        IReadOnlyList<(DateOnly From, DateOnly To)> allowedDateRanges,
        bool canSeeNsfw)
    {
        UserId = userId;
        IsAdmin = isAdmin;
        AllowedStorageIds = allowedStorageIds;
        AllowedPersonGroupIds = allowedPersonGroupIds;
        AllowedDateRanges = allowedDateRanges;
        CanSeeNsfw = canSeeNsfw;
    }

    public Guid UserId { get; }
    public bool IsAdmin { get; }
    public IReadOnlySet<int> AllowedStorageIds { get; }
    public IReadOnlySet<int> AllowedPersonGroupIds { get; }
    public IReadOnlyList<(DateOnly From, DateOnly To)> AllowedDateRanges { get; }
    public bool CanSeeNsfw { get; }

    public static CurrentUser CreateAnonymous()
        => new(
            Guid.Empty,
            isAdmin: false,
            Array.Empty<int>().ToHashSet(),
            Array.Empty<int>().ToHashSet(),
            Array.Empty<(DateOnly From, DateOnly To)>(),
            canSeeNsfw: false);

    public static CurrentUser FromEffectiveAccess(Guid userId, EffectiveAccess access)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User identifier cannot be empty", nameof(userId));
        }

        if (access is null)
        {
            throw new ArgumentNullException(nameof(access));
        }

        return new CurrentUser(
            userId,
            access.IsAdmin,
            CloneSet(access.StorageIds),
            CloneSet(access.PersonGroupIds),
            access.DateRanges?.ToList() ?? new List<(DateOnly From, DateOnly To)>(),
            access.CanSeeNsfw);
    }

    private static IReadOnlySet<int> CloneSet(IReadOnlySet<int> source)
        => source is HashSet<int> hashSet
            ? new HashSet<int>(hashSet)
            : source?.ToHashSet() ?? new HashSet<int>();
}
