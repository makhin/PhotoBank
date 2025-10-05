using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Identity;
using PhotoBank.ViewModel.Dto;
using System.Linq;
using System.Threading.Tasks;

namespace PhotoBank.UnitTests.Identity;

[TestFixture]
public class AdminUserServiceTests
{
    [Test]
    public async Task CreateAsync_DuplicateEmail_ReturnsConflict()
    {
        await using var db = TestDbFactory.CreateInMemory();
        var userManager = IdentityTestHelpers.CreateUserManager(db);
        var roleManager = CreateRoleManager(db);
        var service = new AdminUserService(userManager, roleManager);

        var user = new ApplicationUser { UserName = "user@example.com", Email = "user@example.com" };
        await userManager.CreateAsync(user, "Str0ngP@ssw0rd!");

        var result = await service.CreateAsync(new CreateUserDto
        {
            Email = "user@example.com",
            Password = "An0therStrongPassword!"
        });

        result.Succeeded.Should().BeFalse();
        result.Conflict.Should().BeTrue();
    }

    [Test]
    public async Task UpdateAsync_InvalidTelegramId_ReturnsValidationFailure()
    {
        await using var db = TestDbFactory.CreateInMemory();
        var userManager = IdentityTestHelpers.CreateUserManager(db);
        var roleManager = CreateRoleManager(db);
        var service = new AdminUserService(userManager, roleManager);

        var user = new ApplicationUser { UserName = "user@example.com", Email = "user@example.com" };
        await userManager.CreateAsync(user, "Str0ngP@ssw0rd!");

        var result = await service.UpdateAsync(user.Id, new UpdateUserDto
        {
            TelegramUserId = "invalid"
        });

        result.ValidationFailure.Should().NotBeNull();
        result.ValidationFailure!.FieldName.Should().Be(nameof(UpdateUserDto.TelegramUserId));
    }

    [Test]
    public async Task SetRolesAsync_AddsOnlyExistingRoles()
    {
        await using var db = TestDbFactory.CreateInMemory();
        var userManager = IdentityTestHelpers.CreateUserManager(db);
        var roleManager = CreateRoleManager(db);
        await roleManager.CreateAsync(new IdentityRole("Admin"));

        var service = new AdminUserService(userManager, roleManager);

        var user = new ApplicationUser { UserName = "user@example.com", Email = "user@example.com" };
        await userManager.CreateAsync(user, "Str0ngP@ssw0rd!");

        var result = await service.SetRolesAsync(user.Id, new SetRolesDto
        {
            Roles = new[] { "Admin", "Missing" }
        });

        result.Succeeded.Should().BeTrue();

        var roles = await userManager.GetRolesAsync(user);
        roles.Should().ContainSingle().Which.Should().Be("Admin");
    }

    private static RoleManager<IdentityRole> CreateRoleManager(PhotoBank.DbContext.DbContext.PhotoBankDbContext db)
    {
        var store = new RoleStore<IdentityRole>(db);
        return new RoleManager<IdentityRole>(
            store,
            new IRoleValidator<IdentityRole>[] { new RoleValidator<IdentityRole>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null);
    }
}
