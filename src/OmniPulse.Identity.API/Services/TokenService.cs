using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OmniPulse.BuildingBlocks.Interfaces;

namespace OmniPulse.Identity.API.Services;

/// <summary>
/// Sistem genelinde JWT üretimi ve şifrelenmesini üstlenen güvenli servis! 🔐🎫
/// </summary>
public class TokenService(IConfiguration configuration) : ITokenService
{
    public string GenerateToken(
        string userId, 
        string email, 
        Guid tenantId, 
        string tenantIdentifier, 
        IEnumerable<string> roles)
    {
        var jwtKey = configuration["Jwt:Key"] ?? "SuperSecretKeyEnsure32CharactersLongForSecurity!";
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "OmniPulse";
        var jwtAudience = configuration["Jwt:Audience"] ?? "OmniPulseClients";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creeds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Microsoft ve AWS standartlarında token içeriklerini mühürlüyoruz
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("tid", tenantId.ToString()), // Microsoft Entra ID standardı: Tenant ID (Guid)
            new("tenant_identifier", tenantIdentifier) // Aktif kiracı takma adı (Örn: "pandaberry")
        };

        // Kullanıcının bu kiracı üzerindeki yetki rollerini ekleyelim
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2), // Kısa ömürlü token (2 saat)
            signingCredentials: creeds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var jwtKey = configuration["Jwt:Key"] ?? "SuperSecretKeyEnsure32CharactersLongForSecurity!";
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidAudience = configuration["Jwt:Audience"] ?? "OmniPulseClients",
            ValidIssuer = configuration["Jwt:Issuer"] ?? "OmniPulse",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = false // Süresi dolmuş olsa da içini okumak istiyoruz!
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
            return principal;
        }
        catch
        {
            return null;
        }
    }
}
