using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using PhotoBank.Services;

namespace PhotoBank.UnitTests;

[TestFixture]
public class RetryHelperTests
{
    [Test]
    public async Task RetryAsync_ShouldRetryWhenPredicateTrue()
    {
        var callCount = 0;

        var result = await RetryHelper.RetryAsync(
            action: () =>
            {
                callCount++;
                if (callCount < 3) throw new HttpRequestException();
                return Task.FromResult("ok");
            },
            attempts: 3,
            delay: TimeSpan.Zero,
            shouldRetry: ex => ex is HttpRequestException);

        result.Should().Be("ok");
        callCount.Should().Be(3);
    }

    [Test]
    public async Task RetryAsync_ShouldThrowWhenPredicateFalse()
    {
        var callCount = 0;

        Func<Task<string>> act = () => RetryHelper.RetryAsync<string>(
            action: () =>
            {
                callCount++;
                throw new InvalidOperationException();
            },
            attempts: 3,
            delay: TimeSpan.Zero,
            shouldRetry: ex => ex is HttpRequestException);

        await act.Should().ThrowAsync<InvalidOperationException>();
        callCount.Should().Be(1);
    }

    [Test]
    public async Task RetryAsync_ShouldThrowAfterMaxAttempts()
    {
        var callCount = 0;

        Func<Task<string>> act = () => RetryHelper.RetryAsync<string>(
            action: () =>
            {
                callCount++;
                throw new HttpRequestException();
            },
            attempts: 2,
            delay: TimeSpan.Zero,
            shouldRetry: ex => ex is HttpRequestException);

        await act.Should().ThrowAsync<HttpRequestException>();
        callCount.Should().Be(2);
    }
}

