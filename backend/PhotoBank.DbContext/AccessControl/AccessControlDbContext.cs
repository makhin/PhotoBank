using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PhotoBank.AccessControl;

/// <summary>
/// DbContext only for ACL tables (to break dependency cycles).
/// Maps to the same database as the main context.
/// </summary>
public class AccessControlDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public AccessControlDbContext(DbContextOptions<AccessControlDbContext> options) : base(options)
    {
    }

    public DbSet<AccessProfile> AccessProfiles => Set<AccessProfile>();
    public DbSet<AccessProfileStorageAllow> AccessProfileStorages => Set<AccessProfileStorageAllow>();
    public DbSet<AccessProfilePersonGroupAllow> AccessProfilePersonGroups => Set<AccessProfilePersonGroupAllow>();
    public DbSet<AccessProfileDateRangeAllow> AccessProfileDateRanges => Set<AccessProfileDateRangeAllow>();
    public DbSet<RoleAccessProfile> RoleAccessProfiles => Set<RoleAccessProfile>();
    public DbSet<UserAccessProfile> UserAccessProfiles => Set<UserAccessProfile>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // PKs
        b.Entity<AccessProfileStorageAllow>().HasKey(x => new { x.ProfileId, x.StorageId });
        b.Entity<AccessProfilePersonGroupAllow>().HasKey(x => new { x.ProfileId, x.PersonGroupId });
        b.Entity<AccessProfileDateRangeAllow>().HasKey(x => new { x.ProfileId, x.FromDate, x.ToDate });
        b.Entity<RoleAccessProfile>().HasKey(x => new { x.RoleId, x.ProfileId });
        b.Entity<UserAccessProfile>().HasKey(x => new { x.UserId, x.ProfileId });

        // Relations
        b.Entity<AccessProfileStorageAllow>()
            .HasOne(x => x.Profile).WithMany(p => p.Storages).HasForeignKey(x => x.ProfileId);
        b.Entity<AccessProfilePersonGroupAllow>()
            .HasOne(x => x.Profile).WithMany(p => p.PersonGroups).HasForeignKey(x => x.ProfileId);
        b.Entity<AccessProfileDateRangeAllow>()
            .HasOne(x => x.Profile).WithMany(p => p.DateRanges).HasForeignKey(x => x.ProfileId);

        // DateOnly -> date
        var dConv = new ValueConverter<DateOnly, DateTime>(
            d => d.ToDateTime(TimeOnly.MinValue),
            dt => DateOnly.FromDateTime(dt));
        b.Entity<AccessProfileDateRangeAllow>().Property(x => x.FromDate).HasConversion(dConv).HasColumnType("date");
        b.Entity<AccessProfileDateRangeAllow>().Property(x => x.ToDate).HasConversion(dConv).HasColumnType("date");

        b.Entity<AccessProfile>().HasIndex(x => x.Name).IsUnique();
    }
}
