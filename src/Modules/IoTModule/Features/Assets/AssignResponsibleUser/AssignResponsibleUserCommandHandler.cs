using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniPulse.Modules.IoTModule.Domain.Entities;
using OmniPulse.Modules.IoTModule.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OmniPulse.Modules.IoTModule.Features.Assets.AssignResponsibleUser;

public class AssignResponsibleUserCommandHandler(IoTDbContext dbContext)
    : IRequestHandler<AssignResponsibleUserCommand, AssignResponsibleUserResponse>
{
    public async Task<AssignResponsibleUserResponse> Handle(
        AssignResponsibleUserCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Varlığı veri tabanından getir (Kiracı filtresi otomatik uygulanır)
        var asset = await dbContext.Assets
            .FirstOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken);

        if (asset == null)
        {
            throw new KeyNotFoundException(
                $"Sorumlu kullanıcı atanacak varlık [{request.AssetId}] bulunamadı veya bu kiracıya ait değil!");
        }

        // 2. Bu kullanıcı, rol ve varlık eşleşmesini kontrol et
        var roleNormalized = request.Role.Trim();
        var existingPermission = await dbContext.AssetPermissions
            .FirstOrDefaultAsync(ap => ap.AssetId == request.AssetId 
                                    && ap.UserId == request.UserId 
                                    && ap.Role == roleNormalized, cancellationToken);

        string status;

        if (!request.IsUnassign)
        {
            if (existingPermission != null)
            {
                // Zaten bu kullanıcı bu role atanmış
                status = "Assigned";
            }
            else
            {
                // Yeni atama kaydı oluştur (Aynı role birden fazla kişi atanabilir)
                var newPermission = AssetPermission.Create(
                    tenantId: asset.TenantId,
                    assetId: asset.Id,
                    userId: request.UserId,
                    role: roleNormalized
                );

                dbContext.AssetPermissions.Add(newPermission);
                status = "Assigned";
            }
        }
        else
        {
            // Kullanıcıyı rolden çıkar (Soft Delete)
            if (existingPermission != null)
            {
                existingPermission.IsDeleted = true;
                existingPermission.DeletedAtUtc = DateTime.UtcNow;
                existingPermission.DeletedBy = "System";
                dbContext.AssetPermissions.Update(existingPermission);
            }
            
            status = "Unassigned";
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AssignResponsibleUserResponse(
            AssetId: asset.Id,
            AssetName: asset.Name,
            UserId: request.UserId,
            Role: roleNormalized,
            Status: status
        );
    }
}
