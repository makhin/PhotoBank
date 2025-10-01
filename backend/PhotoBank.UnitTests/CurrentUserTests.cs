using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;
using PhotoBank.AccessControl;

namespace PhotoBank.UnitTests;

[TestFixture]
public class CurrentUserTests
{
    [Test]
    public void Constructor_ShouldPopulateProperties_WhenAuthenticated()
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user1") }, "auth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        var http = new Mock<IHttpContextAccessor>();
        http.Setup(x => x.HttpContext).Returns(httpContext);

        var access = new EffectiveAccess(
            new HashSet<int> { 1 },
            new HashSet<int> { 2 },
            Array.Empty<(DateOnly, DateOnly)>(),
            true,
            true);

        var provider = new Mock<IEffectiveAccessProvider>();
        provider.Setup(p => p.GetAsync("user1", principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(access);

        var current = new CurrentUser(http.Object, provider.Object);

        current.UserId.Should().Be("user1");
        current.IsAdmin.Should().BeTrue();
        current.AllowedStorageIds.Should().Contain(1);
        current.AllowedPersonGroupIds.Should().Contain(2);
        current.CanSeeNsfw.Should().BeTrue();
        provider.Verify(p => p.GetAsync("user1", principal, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void Constructor_ShouldReturnAnonymousUser_WhenUnauthenticated()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var httpContext = new DefaultHttpContext { User = principal };
        var http = new Mock<IHttpContextAccessor>();
        http.Setup(x => x.HttpContext).Returns(httpContext);

        var provider = new Mock<IEffectiveAccessProvider>(MockBehavior.Strict);

        var current = new CurrentUser(http.Object, provider.Object);

        current.UserId.Should().BeEmpty();
        current.IsAdmin.Should().BeFalse();
        current.AllowedStorageIds.Should().BeEmpty();
        current.AllowedPersonGroupIds.Should().BeEmpty();
        current.AllowedDateRanges.Should().BeEmpty();
        current.CanSeeNsfw.Should().BeFalse();
        provider.Verify(p => p.GetAsync(It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void Constructor_ShouldFallbackToSubClaim_WhenNameIdentifierMissing()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(JwtRegisteredClaimNames.Sub, "jwt-sub") },
            authenticationType: "auth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        var http = new Mock<IHttpContextAccessor>();
        http.Setup(x => x.HttpContext).Returns(httpContext);

        var access = new EffectiveAccess(
            new HashSet<int>(),
            new HashSet<int>(),
            Array.Empty<(DateOnly, DateOnly)>(),
            false,
            false);

        var provider = new Mock<IEffectiveAccessProvider>();
        provider.Setup(p => p.GetAsync("jwt-sub", principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(access);

        var current = new CurrentUser(http.Object, provider.Object);

        current.UserId.Should().Be("jwt-sub");
        provider.Verify(p => p.GetAsync("jwt-sub", principal, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void Constructor_ShouldThrowUnauthorized_WhenAuthenticatedWithoutIdentifier()
    {
        var identity = new ClaimsIdentity(authenticationType: "auth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        var http = new Mock<IHttpContextAccessor>();
        http.Setup(x => x.HttpContext).Returns(httpContext);

        var provider = new Mock<IEffectiveAccessProvider>(MockBehavior.Strict);

        var act = () => new CurrentUser(http.Object, provider.Object);

        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("Authenticated user missing identifier claim");
    }
}

