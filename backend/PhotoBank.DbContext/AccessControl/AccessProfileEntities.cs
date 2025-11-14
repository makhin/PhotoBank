using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhotoBank.AccessControl;

public class AccessProfile
{
    public int Id { get; set; }

    [MaxLength(128)]
    public string Name { get; set; } = default!;

    [MaxLength(512)]
    public string? Description { get; set; }

    public bool Flags_CanSeeNsfw { get; set; }

    public ICollection<AccessProfileStorageAllow> Storages { get; set; } = [];
    public ICollection<AccessProfilePersonGroupAllow> PersonGroups { get; set; } = [];
    public ICollection<AccessProfileDateRangeAllow> DateRanges { get; set; } = [];
    public ICollection<UserAccessProfile> UserAssignments { get; set; } = [];
}

public class AccessProfileStorageAllow
{
    public int ProfileId { get; set; }
    public int StorageId { get; set; }
    public AccessProfile Profile { get; set; } = default!;
}

public class AccessProfilePersonGroupAllow
{
    public int ProfileId { get; set; }
    public int PersonGroupId { get; set; }
    public AccessProfile Profile { get; set; } = default!;
}

public class AccessProfileDateRangeAllow
{
    public int ProfileId { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public AccessProfile Profile { get; set; } = default!;
}

public class RoleAccessProfile
{
    public Guid RoleId { get; set; }
    public int ProfileId { get; set; }
}

public class UserAccessProfile
{
    public Guid UserId { get; set; }
    public int ProfileId { get; set; }
    public AccessProfile Profile { get; set; } = default!;
}
