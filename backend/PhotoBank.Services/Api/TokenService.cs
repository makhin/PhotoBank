using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PhotoBank.DbContext.Models;

namespace PhotoBank.Services.Api;

public interface ITokenService
{
    string CreateToken(ApplicationUser user, bool rememberMe = false, IEnumerable<Claim>? additionalClaims = null);
}

public class TokenService(IConfiguration configuration) : ITokenService
{
    public string CreateToken(ApplicationUser user, bool rememberMe = false, IEnumerable<Claim>? additionalClaims = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty)
        };

        if (additionalClaims != null)
        {
            claims.AddRange(additionalClaims);
        }

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: rememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

