using MediatR;

namespace OmniPulse.Modules.TenantModule.Features.Auth.Refresh;

/// <summary>
/// Süresi dolan JWT token'ı refresh token ile tazelemek için kullanılan komut! 🔄
/// </summary>
public record RefreshCommand(
    string Token, 
    string RefreshToken, 
    string? IpAddress = null, 
    string? UserAgent = null
) : IRequest<RefreshResponse>;

/// <summary>
/// Token tazeleme sonucu!
/// </summary>
public record RefreshResponse(
    bool IsSuccess, 
    string? Token, 
    string? RefreshToken, 
    string Message
);
