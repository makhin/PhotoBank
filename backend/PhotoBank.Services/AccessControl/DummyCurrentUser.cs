using System;
using System.Collections.Generic;

namespace PhotoBank.AccessControl;

public sealed class DummyCurrentUser : ICurrentUser
{
    public string UserId => string.Empty;
    public bool IsAdmin => true;
    public IReadOnlySet<int> AllowedStorageIds { get; } = null;
    public IReadOnlySet<int> AllowedPersonGroupIds { get; } = null;
    public IReadOnlyList<(DateOnly From, DateOnly To)> AllowedDateRanges { get; } = null;
    public bool CanSeeNsfw => true;
}
