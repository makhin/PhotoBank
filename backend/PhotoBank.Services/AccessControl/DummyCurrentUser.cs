using System;
using System.Collections.Generic;

namespace PhotoBank.AccessControl;

public sealed class DummyCurrentUser : ICurrentUser
{
    public string UserId => string.Empty;
    public bool IsAdmin => true;
    public IReadOnlySet<int> AllowedStorageIds { get; } = new HashSet<int>();
    public IReadOnlySet<int> AllowedPersonGroupIds { get; } = new HashSet<int>();
    public IReadOnlyList<(DateOnly From, DateOnly To)> AllowedDateRanges { get; } = new List<(DateOnly From, DateOnly To)>();
    public bool CanSeeNsfw => true;
}
