using FluentAssertions;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Identity;
using PhotoBank.ViewModel.Dto;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PhotoBank.UnitTests.Identity;

[TestFixture]
public class UserProfileServiceTests
{
    [Test]
    public async Task Update_WithOnlyTelegramSendTime_KeepsExistingTelegramUserId()
    {
        await using var db = TestDbFactory.CreateInMemory();
        var userManager = IdentityTestHelpers.CreateUserManager(db);
        var service = new UserProfileService(userManager);

        var user = new ApplicationUser
        {
            UserName = "user@example.com",
            Email = "user@example.com",
            TelegramUserId = 12345,
            TelegramSendTimeUtc = System.TimeSpan.FromHours(8)
        };

        await userManager.CreateAsync(user, "Str0ngP@ssw0rd!");

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        }, "Test"));

        var result = await service.UpdateCurrentUserAsync(claimsPrincipal, new UpdateUserDto
        {
            TelegramSendTimeUtc = System.TimeSpan.FromHours(9)
        });

        result.Succeeded.Should().BeTrue();

        var updated = await userManager.FindByIdAsync(user.Id);
        updated!.TelegramUserId.Should().Be(12345);
        updated.TelegramSendTimeUtc.Should().Be(System.TimeSpan.FromHours(9));
    }

    [Test]
    public async Task Update_InvalidTelegramId_ReturnsValidationFailure()
    {
        await using var db = TestDbFactory.CreateInMemory();
        var userManager = IdentityTestHelpers.CreateUserManager(db);
        var service = new UserProfileService(userManager);

        var user = new ApplicationUser
        {
            UserName = "user@example.com",
            Email = "user@example.com"
        };

        await userManager.CreateAsync(user, "Str0ngP@ssw0rd!");

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        }, "Test"));

        var result = await service.UpdateCurrentUserAsync(claimsPrincipal, new UpdateUserDto
        {
            TelegramUserId = "abc"
        });

        result.ValidationFailure.Should().NotBeNull();
        result.ValidationFailure!.FieldName.Should().Be(nameof(UpdateUserDto.TelegramUserId));
    }

    [Test]
    public async Task GetCurrentUser_NotFound_ReturnsNull()
    {
        await using var db = TestDbFactory.CreateInMemory();
        var userManager = IdentityTestHelpers.CreateUserManager(db);
        var service = new UserProfileService(userManager);

        var user = await service.GetCurrentUserAsync(new ClaimsPrincipal(new ClaimsIdentity()));

        user.Should().BeNull();
    }
}
