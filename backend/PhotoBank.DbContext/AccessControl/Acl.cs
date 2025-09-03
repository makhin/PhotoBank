using System;
using System.Linq;

namespace PhotoBank.AccessControl;

public sealed record Acl(
    int[] StorageIds,
    DateTime? FromDate,
    DateTime? ToDate,
    int[] AllowedPersonGroupIds,
    bool CanSeeNsfw
)
{
    public static Acl FromUser(ICurrentUser u)
    {
        var storageIds = u.AllowedStorageIds?.Select(i => (int)i).ToArray() ?? Array.Empty<int>();
        var groups = u.AllowedPersonGroupIds?.Select(i => (int)i).ToArray() ?? Array.Empty<int>();
        DateTime? from = null, to = null;
        if (u.AllowedDateRanges?.Count > 0)
        {
            from = u.AllowedDateRanges.Min(r => r.From).ToDateTime(TimeOnly.MinValue);
            to = u.AllowedDateRanges.Max(r => r.To).ToDateTime(TimeOnly.MaxValue);
        }
        return new(storageIds, from, to, groups, u.CanSeeNsfw);
    }
}