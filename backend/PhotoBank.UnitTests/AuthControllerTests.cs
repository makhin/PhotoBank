using System.Collections.Generic;
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

    private static AuthController CreateController(PhotoBankDbContext db, string serviceKey, string presentedKey)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:Telegram:ServiceKey"] = serviceKey
            })
            .Build();

        var userManager = CreateUserManager(db);
        var signInManager = CreateSignInManager(userManager);
        var tokenService = Mock.Of<ITokenService>();

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
