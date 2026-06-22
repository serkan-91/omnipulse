using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.Modules.IoTModule.Infrastructure.Persistence;

namespace OmniPulse.Modules.IoTModule.Features.Devices.GetTestBenchDevices;

public class GetTestBenchDevicesQueryHandler(
    IoTDbContext dbContext,
    IUserTenantContext userTenantContext)
    : IRequestHandler<GetTestBenchDevicesQuery, IEnumerable<TestBenchDeviceItem>>
{
    public async Task<IEnumerable<TestBenchDeviceItem>> Handle(GetTestBenchDevicesQuery request, CancellationToken cancellationToken)
    {
        if (!userTenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Aktif bir kiracı bağlamı bulunamadı şefim! 🔐");
        }

        var tenantId = userTenantContext.TenantId.Value;

        // 1. AssetId değeri null olan (yani montajı yapılmamış, depoda bekleyen) aktif cihazları sorgula
        var query = dbContext.Devices
            .Include(d => d.Category)
            .Where(d => d.TenantId == tenantId && d.AssetId == null);

        var deviceList = await query
            .OrderByDescending(d => d.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return deviceList.Select(d => new TestBenchDeviceItem(
            Id: d.Id,
            Name: d.Name,
            SerialNumber: d.SerialNumber,
            CategoryId: d.CategoryId,
            CategoryName: d.Category?.Name,
            IsActive: d.IsActive,
            CreatedAtUtc: d.CreatedAtUtc
        ));
    }
}
