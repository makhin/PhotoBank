using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;
using PhotoBank.AccessControl;

namespace PhotoBank.UnitTests;

[TestFixture]
public class CurrentUserAccessorTests
{
    [Test]
    public async Task GetCurrentUserAsync_ShouldPopulateProperties_WhenAuthenticated()
    {
        var userId = Guid.NewGuid();
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "auth");
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
        provider.Setup(p => p.GetAsync(userId.ToString(), principal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(access);

        var accessor = new HttpContextCurrentUserAccessor(http.Object, provider.Object);

        var current = await accessor.GetCurrentUserAsync();

        current.UserId.Should().Be(userId);
        current.IsAdmin.Should().BeTrue();
        current.AllowedStorageIds.Should().Contain(1);
        current.AllowedPersonGroupIds.Should().Contain(2);
        current.CanSeeNsfw.Should().BeTrue();
        provider.Verify(p => p.GetAsync(userId.ToString(), principal, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetCurrentUserAsync_ShouldReturnAnonymousUser_WhenUnauthenticated()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var httpContext = new DefaultHttpContext { User = principal };
        var http = new Mock<IHttpContextAccessor>();
        http.Setup(x => x.HttpContext).Returns(httpContext);

        var provider = new Mock<IEffectiveAccessProvider>(MockBehavior.Strict);

        var accessor = new HttpContextCurrentUserAccessor(http.Object, provider.Object);

        var current = await accessor.GetCurrentUserAsync();

        current.UserId.Should().Be(Guid.Empty);
        current.IsAdmin.Should().BeFalse();
        current.AllowedStorageIds.Should().BeEmpty();
        current.AllowedPersonGroupIds.Should().BeEmpty();
        current.AllowedDateRanges.Should().BeEmpty();
        current.CanSeeNsfw.Should().BeFalse();
        provider.Verify(p => p.GetAsync(It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetCurrentUserAsync_ShouldFallbackToSubClaim_WhenNameIdentifierMissing()
    {
        var userId = Guid.NewGuid();
        var identity = new ClaimsIdentity(
            new[] { new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()) },
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
        provider.Setup(p => p.GetAsync(userId.ToString(), principal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(access);

        var accessor = new HttpContextCurrentUserAccessor(http.Object, provider.Object);

        var current = await accessor.GetCurrentUserAsync();

        current.UserId.Should().Be(userId);
        provider.Verify(p => p.GetAsync(userId.ToString(), principal, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void GetCurrentUserAsync_ShouldThrowUnauthorized_WhenAuthenticatedWithoutIdentifier()
    {
        var identity = new ClaimsIdentity(authenticationType: "auth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        var http = new Mock<IHttpContextAccessor>();
        http.Setup(x => x.HttpContext).Returns(httpContext);

        var provider = new Mock<IEffectiveAccessProvider>(MockBehavior.Strict);
        var accessor = new HttpContextCurrentUserAccessor(http.Object, provider.Object);

        Func<Task> act = () => accessor.GetCurrentUserAsync().AsTask();

        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Authenticated user missing identifier claim");
    }

    [Test]
    public async Task GetCurrentUserAsync_ShouldCacheResultInHttpContextItems()
    {
        var userId = Guid.NewGuid();
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "auth");
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
        provider.Setup(p => p.GetAsync(userId.ToString(), principal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(access);

        var accessor = new HttpContextCurrentUserAccessor(http.Object, provider.Object);

        var first = await accessor.GetCurrentUserAsync();
        var second = accessor.CurrentUser;

        first.Should().BeSameAs(second);
        provider.Verify(p => p.GetAsync(userId.ToString(), principal, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetCurrentUserAsync_ShouldNotCompleteSynchronously_WhenEffectiveAccessProviderDelays()
    {
        var userId = Guid.NewGuid();
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "auth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        var http = new Mock<IHttpContextAccessor>();
        http.Setup(x => x.HttpContext).Returns(httpContext);

        var tcs = new TaskCompletionSource<EffectiveAccess>();

        var provider = new Mock<IEffectiveAccessProvider>();
        provider.Setup(p => p.GetAsync(userId.ToString(), principal, It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        var accessor = new HttpContextCurrentUserAccessor(http.Object, provider.Object);

        var task = accessor.GetCurrentUserAsync().AsTask();
        task.IsCompleted.Should().BeFalse();

        tcs.SetResult(new EffectiveAccess(
            new HashSet<int>(),
            new HashSet<int>(),
            Array.Empty<(DateOnly, DateOnly)>(),
            false,
            false));

        var result = await task;
        result.UserId.Should().Be(userId);
    }
}
