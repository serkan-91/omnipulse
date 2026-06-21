using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.Identity.API.Configuration;

namespace OmniPulse.Identity.API.Services;

/// <summary>
/// Sistem genelinde JWT üretimi ve şifrelenmesini üstlenen güvenli servis! 🔐🎫
/// </summary>
public class TokenService(IOptions<JwtOptions> jwtOptions) : ITokenService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public string GenerateToken(
        string userId,
        string email,
        Guid tenantId,
        string tenantIdentifier,
        IEnumerable<string> roles)
    {
        var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var creeds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Microsoft ve AWS standartlarında token içeriklerini mühürlüyoruz
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new("tid",               tenantId.ToString()),        // Microsoft Entra ID standardı: Tenant ID (Guid)
            new("tenant_identifier", tenantIdentifier)            // Aktif kiracı takma adı (Örn: "pandaberry")
        };

        // Kullanıcının bu kiracı üzerindeki yetki rollerini ekleyelim
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer:            _jwt.Issuer,
            audience:          _jwt.Audience,
            claims:            claims,
            expires:           DateTime.UtcNow.AddHours(_jwt.ExpiryHours),
            signingCredentials: creeds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience         = true,
            ValidateIssuer           = true,
            ValidAudience            = _jwt.Audience,
            ValidIssuer              = _jwt.Issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)),
            ValidateLifetime         = false // Süresi dolmuş olsa da içini okumak istiyoruz!
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
