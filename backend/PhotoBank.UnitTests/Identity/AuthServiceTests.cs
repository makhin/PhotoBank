using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Api;
using PhotoBank.Services.Identity;
using PhotoBank.ViewModel.Dto;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PhotoBank.UnitTests.Identity;

[TestFixture]
public class AuthServiceTests
{
    [Test]
    public async Task TelegramExchange_InvalidServiceKey_ReturnsProblem()
    {
        await using var db = TestDbFactory.CreateInMemory();
        var service = CreateAuthService(db, serviceKey: "expected");

        var result = await service.ExchangeTelegramAsync("123", "user", "en", "wrong");

        result.Succeeded.Should().BeFalse();
        result.Problem.Should().NotBeNull();
        result.Problem!.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        result.Problem.Detail.Should().Be("Invalid service key");
    }

    [Test]
    public async Task TelegramExchange_UnknownTelegramUser_CreatesNewUser()
    {
        await using var db = TestDbFactory.CreateInMemory();
        var service = CreateAuthService(db, serviceKey: "expected");

        var result = await service.ExchangeTelegramAsync("456", "user", "ru", "expected");

        result.Succeeded.Should().BeTrue();
        result.Response.Should().NotBeNull();
        result.Response!.AccessToken.Should().NotBeNullOrEmpty();

        // Verify user was created in database
        var createdUser = db.Users.FirstOrDefault(u => u.TelegramUserId == 456);
        createdUser.Should().NotBeNull();
        createdUser!.TelegramLanguageCode.Should().Be("ru");
        createdUser.UserName.Should().Be("telegram_456");
        createdUser.Email.Should().Be("telegram_456@photobank.local");
    }

    [Test]
    public async Task TelegramExchange_AdminRoleClaim_IsPassedToTokenService()
    {
        await using var db = TestDbFactory.CreateInMemory();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:Telegram:ServiceKey"] = "expected"
            })
            .Build();

        var userManager = IdentityTestHelpers.CreateUserManager(db);
        var signInManager = IdentityTestHelpers.CreateSignInManager(userManager);

        var adminRole = new IdentityRole("Admin") { NormalizedName = "ADMIN" };
        await db.Roles.AddAsync(adminRole);
        await db.SaveChangesAsync();

        var user = new ApplicationUser
        {
            UserName = "admin@example.com",
            Email = "admin@example.com",
            TelegramUserId = 789
        };

        await userManager.CreateAsync(user, "Str0ngP@ssw0rd!");
        await userManager.AddClaimAsync(user, new Claim("ExistingClaim", "Value"));

        db.UserRoles.Add(new IdentityUserRole<string>
        {
            RoleId = adminRole.Id,
            UserId = user.Id
        });
        await db.SaveChangesAsync();

        var tokenServiceMock = new Mock<ITokenService>();
        List<Claim>? capturedClaims = null;

        tokenServiceMock
            .Setup(ts => ts.CreateToken(
                It.Is<ApplicationUser>(u => u.Id == user.Id),
                false,
                It.IsAny<IEnumerable<Claim>>()))
            .Returns("token")
            .Callback<ApplicationUser, bool, IEnumerable<Claim>>((_, _, claims) =>
            {
                capturedClaims = claims?.ToList();
            });

        var authService = new AuthService(userManager, signInManager, tokenServiceMock.Object, new TelegramServiceKeyValidator(configuration));

        var result = await authService.ExchangeTelegramAsync(
            user.TelegramUserId!.Value.ToString(CultureInfo.InvariantCulture),
            user.UserName,
            "en",
            "expected");

        result.Succeeded.Should().BeTrue();
        capturedClaims.Should().NotBeNull();
        capturedClaims!.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        capturedClaims.Should().Contain(c => c.Type == "ExistingClaim" && c.Value == "Value");
    }

    [Test]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        await using var db = TestDbFactory.CreateInMemory();
        var userManager = IdentityTestHelpers.CreateUserManager(db);
        var signInManager = IdentityTestHelpers.CreateSignInManager(userManager);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Auth:Telegram:ServiceKey"] = "expected"
        }).Build();

        var user = new ApplicationUser
        {
            UserName = "user@example.com",
            Email = "user@example.com"
        };

        await userManager.CreateAsync(user, "CorrectP@ssword1!");

        var authService = new AuthService(userManager, signInManager, Mock.Of<ITokenService>(), new TelegramServiceKeyValidator(configuration));

        var result = await authService.LoginAsync(new LoginRequestDto
        {
            Email = "user@example.com",
            Password = "WrongPassword",
            RememberMe = false
        });

        result.Succeeded.Should().BeFalse();
    }

    [Test]
    public async Task GetTelegramSubscriptions_InvalidKey_ReturnsProblem()
    {
        await using var db = TestDbFactory.CreateInMemory();
        var service = CreateAuthService(db, serviceKey: "expected");

        var result = await service.GetTelegramSubscriptionsAsync("wrong");

        result.Succeeded.Should().BeFalse();
        result.Problem.Should().NotBeNull();
        result.Problem!.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    private static AuthService CreateAuthService(PhotoBankDbContext db, string serviceKey)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:Telegram:ServiceKey"] = serviceKey
            })
            .Build();

        var userManager = IdentityTestHelpers.CreateUserManager(db);
        var signInManager = IdentityTestHelpers.CreateSignInManager(userManager);

        var tokenServiceMock = new Mock<ITokenService>();
        tokenServiceMock
            .Setup(ts => ts.CreateToken(
                It.IsAny<ApplicationUser>(),
                It.IsAny<bool>(),
                It.IsAny<IEnumerable<Claim>>()))
            .Returns("test_token");

        return new AuthService(userManager, signInManager, tokenServiceMock.Object, new TelegramServiceKeyValidator(configuration));
    }
}
