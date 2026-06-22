using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.Modules.TenantModule.Domain.Entities;
using OmniPulse.Modules.TenantModule.Infrastructure.Persistence;

namespace OmniPulse.Modules.TenantModule.Features.Tenants.AcceptInvite;

public class AcceptInviteCommandHandler(
    IdentityDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokenService)
    : IRequestHandler<AcceptInviteCommand, AcceptInviteResponse>
{
    public async Task<AcceptInviteResponse> Handle(AcceptInviteCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.ToLowerInvariant().Trim();

        // 1. Kullanıcıyı ve ilişkili olduğu kiracıları getir
        var user = await dbContext.Users
            .Include(u => u.TenantUsers)
            .ThenInclude(tu => tu.Tenant)
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, cancellationToken);

        if (user == null)
        {
            return new AcceptInviteResponse(false, null, "Geçersiz veya süresi dolmuş davet.");
        }

        // 2. Kullanıcı zaten aktif mi ve şifresi belirlenmiş mi kontrol et
        if (user.IsActive && user.PasswordHash != "INVITED_PLACEHOLDER_NO_PASSWORD")
        {
            return new AcceptInviteResponse(false, null, "Bu hesap zaten aktif durumda ve şifresi belirlenmiş.");
        }

        // 3. Şifreyi hashle ve profili güncelle
        var passwordHash = passwordHasher.HashPassword(user, request.Password);
        user.UpdatePassword(passwordHash);
        user.UpdateProfile(request.FirstName, request.LastName);
        user.Activate(); // Hesabı aktif ediyoruz!

        // 4. Kullanıcının davet edildiği kiracı ilişkisini bul
        var tenantUser = user.TenantUsers.FirstOrDefault(tu => !tu.IsDeleted);
        if (tenantUser == null)
        {
            return new AcceptInviteResponse(false, null, "Bu kullanıcıya ait herhangi bir şirket daveti bulunamadı.");
        }

        // Veritabanını güncelliyoruz
        await dbContext.SaveChangesAsync(cancellationToken);

        // 5. Giriş yapması için token üretiyoruz
        var token = tokenService.GenerateToken(
            user.Id.ToString(),
            user.Email,
            tenantUser.TenantId,
            tenantUser.Tenant.Identifier,
            new[] { tenantUser.Role }
        );

        return new AcceptInviteResponse(true, token, "Hesabınız başarıyla oluşturuldu ve aktif edildi.");
    }
}
