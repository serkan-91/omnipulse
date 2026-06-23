using MediatR;

namespace OmniPulse.Tenant.Features.Tenants.InviteUser;

/// <summary>
/// Şirket yöneticisinin e-posta ile yeni bir kullanıcıyı kendi kiracısına davet etme komutu! ✉️🤝
/// </summary>
public record InviteUserCommand(string Email, string Role) : IRequest<InviteUserResponse>;

public record InviteUserResponse(bool IsSuccess, string Message);
