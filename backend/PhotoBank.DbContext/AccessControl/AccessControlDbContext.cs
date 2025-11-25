using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Reflection.Emit;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.UseIdentityAlwaysColumns();

        // PKs
        modelBuilder.Entity<AccessProfileStorageAllow>().HasKey(x => new { x.ProfileId, x.StorageId });
        modelBuilder.Entity<AccessProfilePersonGroupAllow>().HasKey(x => new { x.ProfileId, x.PersonGroupId });
        modelBuilder.Entity<AccessProfileDateRangeAllow>().HasKey(x => new { x.ProfileId, x.FromDate, x.ToDate });
        modelBuilder.Entity<RoleAccessProfile>().HasKey(x => new { x.RoleId, x.ProfileId });
        modelBuilder.Entity<UserAccessProfile>().HasKey(x => new { x.UserId, x.ProfileId });

        // Relations
        modelBuilder.Entity<AccessProfileStorageAllow>()
            .HasOne(x => x.Profile).WithMany(p => p.Storages).HasForeignKey(x => x.ProfileId);
        modelBuilder.Entity<AccessProfilePersonGroupAllow>()
            .HasOne(x => x.Profile).WithMany(p => p.PersonGroups).HasForeignKey(x => x.ProfileId);
        modelBuilder.Entity<AccessProfileDateRangeAllow>()
            .HasOne(x => x.Profile).WithMany(p => p.DateRanges).HasForeignKey(x => x.ProfileId);
        modelBuilder.Entity<UserAccessProfile>()
            .HasOne(x => x.Profile).WithMany(p => p.UserAssignments).HasForeignKey(x => x.ProfileId);

        // DateOnly -> date
        var dConv = new ValueConverter<DateOnly, DateTime>(
            d => d.ToDateTime(TimeOnly.MinValue),
            dt => DateOnly.FromDateTime(dt));
        modelBuilder.Entity<AccessProfileDateRangeAllow>().Property(x => x.FromDate).HasConversion(dConv).HasColumnType("date");
        modelBuilder.Entity<AccessProfileDateRangeAllow>().Property(x => x.ToDate).HasConversion(dConv).HasColumnType("date");

        modelBuilder.Entity<AccessProfile>().HasIndex(x => x.Name).IsUnique();
    }
}
