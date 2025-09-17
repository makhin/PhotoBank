using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using PhotoBank.Api.Controllers;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Api;

namespace PhotoBank.UnitTests;

[TestFixture]
public class AuthControllerTests
{
    [Test]
    public async Task TelegramExchange_InvalidServiceKey_ReturnsProblemDetails()
    {
        await using var db = TestDbFactory.CreateInMemory();
        var controller = CreateController(db, serviceKey: "expected-key", presentedKey: "wrong-key");

        var result = await controller.TelegramExchange(new AuthController.TelegramExchangeRequest(123, "user"));

        var problemResult = result.Should().BeOfType<ObjectResult>().Which;
        problemResult.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        problemResult.ContentTypes.Should().Contain("application/problem+json");

        var problem = problemResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(StatusCodes.Status401Unauthorized);
        problem.Title.Should().Be("Unauthorized");
        problem.Detail.Should().Be("Invalid service key");
    }

    [Test]
    public async Task TelegramExchange_UnknownTelegramUser_ReturnsProblemDetails()
    {
        await using var db = TestDbFactory.CreateInMemory();
        var controller = CreateController(db, serviceKey: "expected-key", presentedKey: "expected-key");

        var result = await controller.TelegramExchange(new AuthController.TelegramExchangeRequest(456, "user"));

        var problemResult = result.Should().BeOfType<ObjectResult>().Which;
        problemResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        problemResult.ContentTypes.Should().Contain("application/problem+json");

        var problem = problemResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(StatusCodes.Status403Forbidden);
        problem.Title.Should().Be("Telegram not linked");
        problem.Detail.Should().Be("Ask admin to link your Telegram");
    }

    [Test]
    public async Task TelegramExchange_AdminRoleClaim_IsPassedToTokenService()
    {
        await using var db = TestDbFactory.CreateInMemory();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:Telegram:ServiceKey"] = "expected-key"
            })
            .Build();

        var userManager = CreateUserManager(db);
        var signInManager = CreateSignInManager(userManager);

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

        var controller = new AuthController(userManager, signInManager, tokenServiceMock.Object, configuration)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        controller.ControllerContext.HttpContext!.Request.Headers["X-Service-Key"] = "expected-key";

        var result = await controller.TelegramExchange(new AuthController.TelegramExchangeRequest(user.TelegramUserId!.Value, user.UserName));

        result.Should().BeOfType<OkObjectResult>();

        capturedClaims.Should().NotBeNull();
        capturedClaims!.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        capturedClaims.Should().Contain(c => c.Type == "ExistingClaim" && c.Value == "Value");
    }

    private static AuthController CreateController(PhotoBankDbContext db, string serviceKey, string presentedKey, ITokenService? tokenService = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:Telegram:ServiceKey"] = serviceKey
            })
            .Build();

        var userManager = CreateUserManager(db);
        var signInManager = CreateSignInManager(userManager);
        tokenService ??= Mock.Of<ITokenService>();

        var controller = new AuthController(userManager, signInManager, tokenService, configuration)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        controller.ControllerContext.HttpContext!.Request.Headers["X-Service-Key"] = presentedKey;
        return controller;
    }

    private static UserManager<ApplicationUser> CreateUserManager(PhotoBankDbContext db)
    {
        var store = new UserStore<ApplicationUser>(db);
        var options = Options.Create(new IdentityOptions());
        var passwordHasher = new PasswordHasher<ApplicationUser>();
        var userValidators = new List<IUserValidator<ApplicationUser>> { new UserValidator<ApplicationUser>() };
        var passwordValidators = new List<IPasswordValidator<ApplicationUser>> { new PasswordValidator<ApplicationUser>() };
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var services = new ServiceCollection();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<UserManager<ApplicationUser>>>();

        return new UserManager<ApplicationUser>(
            store,
            options,
            passwordHasher,
            userValidators,
            passwordValidators,
            keyNormalizer,
            errors,
            serviceProvider,
            logger);
    }

    private static SignInManager<ApplicationUser> CreateSignInManager(UserManager<ApplicationUser> userManager)
    {
        var contextAccessor = new HttpContextAccessor();
        var options = Options.Create(new IdentityOptions());
        var claimsFactory = new UserClaimsPrincipalFactory<ApplicationUser>(userManager, options);

        return new SignInManager<ApplicationUser>(
            userManager,
            contextAccessor,
            claimsFactory,
            options,
            NullLogger<SignInManager<ApplicationUser>>.Instance,
            new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>().Object,
            new Mock<IUserConfirmation<ApplicationUser>>().Object);
    }
}
