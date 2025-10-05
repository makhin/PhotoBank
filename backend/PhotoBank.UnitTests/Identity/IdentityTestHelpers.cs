using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using System.Collections.Generic;

namespace PhotoBank.UnitTests.Identity;

internal static class IdentityTestHelpers
{
    public static UserManager<ApplicationUser> CreateUserManager(PhotoBankDbContext db)
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

    public static SignInManager<ApplicationUser> CreateSignInManager(UserManager<ApplicationUser> userManager)
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
