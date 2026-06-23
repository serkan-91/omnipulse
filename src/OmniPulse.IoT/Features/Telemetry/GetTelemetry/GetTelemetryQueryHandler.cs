using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.IoT.Infrastructure.Persistence;
using OmniPulse.IoT.Features.Telemetry;

namespace OmniPulse.IoT.Features.Telemetry.GetTelemetry;

public class GetTelemetryQueryHandler(
    IoTDbContext dbContext,
    IUserTenantContext userTenantContext)
    : IRequestHandler<GetTelemetryQuery, PagedResult<TelemetryDto>>
{
    public async Task<PagedResult<TelemetryDto>> Handle(GetTelemetryQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Telemetries
            .Include(t => t.Device)
            .ThenInclude(d => d.Asset)
            .AsNoTracking()
            .ApplyAssetFilter(userTenantContext, dbContext);

        // 2. Diğer isteğe bağlı filtreleri uygula
        if (request.DeviceId.HasValue)
        {
            query = query.Where(t => t.DeviceId == request.DeviceId.Value);
        }

        if (request.AssetId.HasValue)
        {
            query = query.Where(t => t.Device.AssetId == request.AssetId.Value);
        }

        if (request.StartDate.HasValue)
        {
            query = query.Where(t => t.Timestamp >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(t => t.Timestamp <= request.EndDate.Value);
        }

        // 3. Sıralama ve Sayfalama
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.Timestamp)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TelemetryDto(
                t.Id,
                t.DeviceId,
                t.Device.Name,
                t.Device.SerialNumber,
                t.Device.AssetId,
                t.Device.Asset != null ? t.Device.Asset.Name : null,
                t.Temperature,
                t.Pressure,
                t.Timestamp
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<TelemetryDto>(items, totalCount, request.Page, request.PageSize);
    }
}
