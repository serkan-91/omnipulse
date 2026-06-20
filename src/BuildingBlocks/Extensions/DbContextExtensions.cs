using System;
using Microsoft.EntityFrameworkCore;
using OmniPulse.BuildingBlocks.Interfaces;

namespace OmniPulse.BuildingBlocks.Extensions;

/// <summary>
/// DbContext üzerinde otomatik veri damgalama, soft delete ve kiracı damgalama işlemlerini yöneten uzantı sınıfı! 🚀
/// </summary>
public static class DbContextExtensions
{
    public static void ApplyAuditingAndSoftDelete(this DbContext dbContext, IUserTenantContext userTenantContext)
    {
        var currentUserId = userTenantContext.UserId ?? "System";
        var utcNow = DateTime.UtcNow;

        foreach (var entry in dbContext.ChangeTracker.Entries())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    // 1. Denetim alanlarını doldur
                    if (entry.Entity is IAuditableEntity auditableAdd)
                    {
                        auditableAdd.CreatedAtUtc = utcNow;
                        auditableAdd.CreatedBy = currentUserId;
                    }

                    // 2. Kiracı (Tenant) bilgisini otomatik damgala
                    if (entry.Entity is ITenantEntity tenantEntityAdd)
                    {
                        if (tenantEntityAdd.TenantId == Guid.Empty && userTenantContext.TenantId.HasValue)
                        {
                            tenantEntityAdd.TenantId = userTenantContext.TenantId.Value;
                        }
                    }
                    break;

                case EntityState.Modified:
                    // 1. Güncelleme denetimi
                    if (entry.Entity is IAuditableEntity auditableModify)
                    {
                        // Oluşturulma bilgilerinin ezilmesini engelliyoruz!
                        entry.Property(nameof(IAuditableEntity.CreatedAtUtc)).IsModified = false;
                        entry.Property(nameof(IAuditableEntity.CreatedBy)).IsModified = false;

                        auditableModify.LastModifiedAtUtc = utcNow;
                        auditableModify.LastModifiedBy = currentUserId;
                    }

                    // 2. Güvenlik: Kiracı bilgisinin güncellenerek verinin başka kiracıya kaçırılmasını engelliyoruz!
                    if (entry.Entity is ITenantEntity)
                    {
                        entry.Property(nameof(ITenantEntity.TenantId)).IsModified = false;
                    }
                    break;

                case EntityState.Deleted:
                    // 1. Soft Delete tetiklemesi
                    if (entry.Entity is ISoftDelete softDelete)
                    {
                        entry.State = EntityState.Modified;
                        softDelete.IsDeleted = true;
                        softDelete.DeletedAtUtc = utcNow;
                        softDelete.DeletedBy = currentUserId;
                    }
                    break;
            }
        }
    }
}
