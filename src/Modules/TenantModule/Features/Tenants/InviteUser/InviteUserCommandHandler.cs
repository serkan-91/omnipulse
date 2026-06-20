using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.Modules.TenantModule.Domain.Entities;
using OmniPulse.Modules.TenantModule.Infrastructure.Persistence;

namespace OmniPulse.Modules.TenantModule.Features.Tenants.InviteUser;

/// <summary>
/// Kullanıcı davet etme iş mantığı (Command Handler) 🤝📩
/// Şirket yöneticisinin yetkisini sorgular, davet edilen kullanıcının sistemdeki varlığını
/// analiz ederek ara tabloya ekler veya geçici pasif profil oluşturur.
/// </summary>
public class InviteUserCommandHandler(
    IdentityDbContext dbContext,
    IUserTenantContext userTenantContext)
    : IRequestHandler<InviteUserCommand, InviteUserResponse>
{
    public async Task<InviteUserResponse> Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        // 1. İstek atan kişinin kimliği ve kiracı bağlamı doğrulanmış mı?
        if (!userTenantContext.IsAuthenticated || 
            string.IsNullOrEmpty(userTenantContext.UserId) || 
            !userTenantContext.TenantId.HasValue)
        {
            return new InviteUserResponse(false, "Oturum açmamış veya kiracı seçmemişsiniz.");
        }

        var callerUserId = Guid.Parse(userTenantContext.UserId);
        var callerTenantId = userTenantContext.TenantId.Value;

        // 2. Rol kontrolü: İstek atan kişi bu kiracıda yönetici (Owner veya Admin) mi?
        var callerMembership = await dbContext.TenantUsers
            .FirstOrDefaultAsync(tu => tu.UserId == callerUserId && tu.TenantId == callerTenantId && !tu.IsDeleted, cancellationToken);

        if (callerMembership == null || 
            (callerMembership.Role != "Owner" && callerMembership.Role != "Admin"))
        {
            return new InviteUserResponse(false, "Sadece şirket sahibi veya yöneticileri davet gönderebilir!");
        }

        // 3. Atanmak istenen rolün doğrulanması
        var role = request.Role.Trim();
        var allowedRoles = new[] { "Owner", "Admin", "Member" };
        if (!allowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            return new InviteUserResponse(false, "Geçersiz üyelik rolü! Sadece 'Owner', 'Admin' veya 'Member' atanabilir.");
        }

        var invitedEmail = request.Email.ToLowerInvariant().Trim();

        // 4. Davet edilecek kullanıcı sistemde var mı? (Soft-delete durumları dahil)
        var targetUser = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == invitedEmail, cancellationToken);

        if (targetUser != null)
        {
            // Eğer silinmişse, hesabı kurtar/aktif et
            if (targetUser.IsDeleted)
            {
                targetUser.IsDeleted = false;
                targetUser.DeletedAtUtc = null;
                targetUser.DeletedBy = null;
                targetUser.Activate();
            }

            // Kiracı-Kullanıcı ara tablosundaki ilişkiyi kontrol et
            var existingMembership = await dbContext.TenantUsers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(tu => tu.UserId == targetUser.Id && tu.TenantId == callerTenantId, cancellationToken);

            if (existingMembership != null)
            {
                if (existingMembership.IsDeleted)
                {
                    // Eski silinmiş daveti canlandır!
                    existingMembership.IsDeleted = false;
                    existingMembership.DeletedAtUtc = null;
                    existingMembership.DeletedBy = null;
                    existingMembership.UpdateRole(role);

                    await dbContext.SaveChangesAsync(cancellationToken);
                    return new InviteUserResponse(true, $"Kullanıcının bu şirketteki eski kaydı silinmişti, başarıyla '{role}' rolüyle canlandırıldı.");
                }

                return new InviteUserResponse(false, "Bu kullanıcı zaten şirketinizde kayıtlı bir üye şefim!");
            }

            // Kullanıcı sistemde var ama bu kiracıda yok, doğrudan bağla!
            var newTenantUser = TenantUser.Create(callerTenantId, targetUser.Id, role);
            dbContext.TenantUsers.Add(newTenantUser);

            await dbContext.SaveChangesAsync(cancellationToken);
            return new InviteUserResponse(true, $"Kullanıcı sistemde kayıtlıydı, şirketiniz bünyesine '{role}' rolüyle dahil edildi.");
        }
        else
        {
            // 5. Kullanıcı sistemde hiç yok! ✉️
            // Ona geçici bir pasif profil oluşturuyoruz. Şifresi başlangıçta boştur.
            var placeholderUser = User.Create(
                firstName: "Davetli",
                lastName: "Kullanıcı",
                email: invitedEmail,
                passwordHash: "INVITED_PLACEHOLDER_NO_PASSWORD"
            );

            placeholderUser.Deactivate(); // Şifre belirleyene kadar pasif kalmalı!
            dbContext.Users.Add(placeholderUser);

            // İlişki ara tablosuna hemen ekliyoruz
            var newTenantUser = TenantUser.Create(callerTenantId, placeholderUser.Id, role);
            dbContext.TenantUsers.Add(newTenantUser);

            await dbContext.SaveChangesAsync(cancellationToken);
            return new InviteUserResponse(true, $"Yeni kullanıcı için davet kaydı oluşturuldu ve '{role}' rolüyle şirketiniz altına eklendi.");
        }
    }
}
