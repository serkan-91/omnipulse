using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace OmniPulse.BuildingBlocks.Interfaces;

/// <summary>
/// Mimarideki JWT token üretiminden sorumlu, host bağımsız arayüz! 🎫
/// </summary>
public interface ITokenService
{
    // Kullanıcı ve aktif kiracı için imzalanmış güvenli bir JWT token üretir
    string GenerateToken(
        string userId, 
        string email, 
        Guid tenantId, 
        string tenantIdentifier, 
        IEnumerable<string> roles);

    // Süresi dolmuş token'dan iddia (claim) bilgilerini güvenle söker 🛡️
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
