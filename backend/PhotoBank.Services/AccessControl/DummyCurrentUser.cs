using System;
using System.Collections.Generic;

namespace PhotoBank.AccessControl;

public sealed class DummyCurrentUser : ICurrentUser
{
    public Guid UserId => Guid.Empty;
    public bool IsAdmin => true;
    public IReadOnlySet<int>? AllowedStorageIds => null;
    public IReadOnlySet<int>? AllowedPersonGroupIds => null;
    public IReadOnlyList<(DateOnly From, DateOnly To)>? AllowedDateRanges => null;
    public bool CanSeeNsfw => true;
}
