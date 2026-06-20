using System;
using System.Collections.Generic;
using MediatR;

namespace OmniPulse.Modules.TenantModule.Features.Auth.Login;

/// <summary>
/// Giriş denemesi komutu! 🔐
/// </summary>
public record LoginCommand(
    string Email, 
    string Password, 
    string? TenantIdentifier = null, // Kullanıcı doğrudan hedef bir kiracı seçebilir (Örn: "pandaberry")
    string? IpAddress = null,        // Güvenlik ve denetim günlüğü için IP adresi
    string? UserAgent = null         // Güvenlik ve denetim günlüğü için sistem bilgisi
) : IRequest<LoginResponse>;

/// <summary>
/// Giriş denemesi sonucu!
/// </summary>
public record LoginResponse(
    bool IsSuccess, 
    string? Token, 
    string? RefreshToken,
    string Message, 
    IEnumerable<TenantDto>? AvailableTenants = null // Eğer TenantIdentifier belirtilmediyse ve kullanıcının birden fazla kiracısı varsa seçmesi için liste döneriz!
);

public record TenantDto(Guid Id, string Name, string Identifier);
