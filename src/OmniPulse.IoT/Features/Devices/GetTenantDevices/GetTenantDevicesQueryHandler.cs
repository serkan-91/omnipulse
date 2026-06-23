using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.IoT.Infrastructure.Persistence;

namespace OmniPulse.IoT.Features.Devices.GetTenantDevices;

public class GetTenantDevicesQueryHandler(
    IoTDbContext dbContext,
    IUserTenantContext userTenantContext)
    : IRequestHandler<GetTenantDevicesQuery, IEnumerable<TenantDeviceDto>>
{
    public async Task<IEnumerable<TenantDeviceDto>> Handle(GetTenantDevicesQuery request, CancellationToken cancellationToken)
    {
        // 1. Tenant/Kiracı bağlamını doğrula
        if (!userTenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Aktif bir kiracı bağlamı bulunamadı şefim! 🔐");
        }

        var tenantId = userTenantContext.TenantId.Value;

        // 2. Kiracının tüm aktif cihazlarını çek
        var devices = await dbContext.Devices
            .Include(d => d.Asset)
            .Include(d => d.Category)
            .Where(d => d.TenantId == tenantId && !d.IsDeleted)
            .OrderBy(d => d.Name)
            .Select(d => new TenantDeviceDto(
                d.Id,
                d.Name,
                d.SerialNumber,
                d.IsActive,
                d.AssetId,
                d.Asset != null ? d.Asset.Name : null,
                d.CategoryId,
                d.Category != null ? d.Category.Name : null
            ))
            .ToListAsync(cancellationToken);

        return devices;
    }
}
