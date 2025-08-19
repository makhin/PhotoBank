using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Linq;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Api;

namespace PhotoBank.UnitTests;

[TestFixture]
public class TokenServiceTests
{
    [Test]
    public void CreateToken_WithAdditionalClaims_ShouldContainThem()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "VerySecretKeyVerySecretKeyVerySecretKey",
                ["Jwt:Issuer"] = "test",
                ["Jwt:Audience"] = "test"
            })
            .Build();

        var service = new TokenService(configuration);
        var user = new ApplicationUser
        {
            Id = "1",
            Email = "user@example.com",
            UserName = "user"
        };

        var claims = new[] { new Claim("TestClaim", "True") };

        var token = service.CreateToken(user, false, claims);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.That(jwt.Claims.Any(c => c.Type == "TestClaim" && c.Value == "True"));
    }
}
