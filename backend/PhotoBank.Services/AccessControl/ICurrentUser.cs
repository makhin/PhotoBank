using System;
using System.Collections.Generic;

namespace PhotoBank.AccessControl;

public interface ICurrentUser
{
    string UserId { get; }
    bool IsAdmin { get; }
    IReadOnlySet<int> AllowedStorageIds { get; }
    IReadOnlySet<int> AllowedPersonGroupIds { get; }
    IReadOnlyList<(DateOnly From, DateOnly To)> AllowedDateRanges { get; }
    bool CanSeeNsfw { get; }
}
