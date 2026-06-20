using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniPulse.Modules.IoTModule.Infrastructure.Persistence;

namespace OmniPulse.Modules.IoTModule.Features.Vehicles.GetVehicles;

public class GetVehiclesQueryHandler(IoTDbContext dbContext)
    : IRequestHandler<GetVehiclesQuery, PagedVehiclesResult>
{
    public async Task<PagedVehiclesResult> Handle(GetVehiclesQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Vehicles
            .Include(v => v.Devices)
            .ThenInclude(d => d.Category)
            .AsNoTracking();

        // Arama filtresi
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToUpperInvariant().Trim();
            query = query.Where(v => v.PlateNumber.Contains(search) || v.Brand.Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(v => v.PlateNumber)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(v => new VehicleDto(
                v.Id,
                v.PlateNumber,
                v.Brand,
                v.DriverUserId,
                v.Devices.Select(d => new DeviceDto(
                    d.Id,
                    d.Name,
                    d.SerialNumber,
                    d.IsActive,
                    d.CategoryId,
                    d.Category != null ? d.Category.Name : null
                )).ToList()
            ))
            .ToListAsync(cancellationToken);

        return new PagedVehiclesResult(items, totalCount, request.Page, request.PageSize);
    }
}
