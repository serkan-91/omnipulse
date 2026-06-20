using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniPulse.Modules.IoTModule.Infrastructure.Persistence;

namespace OmniPulse.Modules.IoTModule.Features.Vehicles.AssignDriver;

public class AssignDriverCommandHandler(IoTDbContext dbContext)
    : IRequestHandler<AssignDriverCommand, AssignDriverResponse>
{
    public async Task<AssignDriverResponse> Handle(AssignDriverCommand request, CancellationToken cancellationToken)
    {
        // 1. Aracı veri tabanından getir (Kiracı filtresi otomatik uygulanır)
        var vehicle = await dbContext.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId, cancellationToken);

        if (vehicle == null)
        {
            throw new KeyNotFoundException($"Sürücü atanacak araç [{request.VehicleId}] bulunamadı veya bu kiracıya ait değil!");
        }

        // 2. Sürücüyü ata
        vehicle.AssignDriver(request.DriverUserId);

        dbContext.Vehicles.Update(vehicle);
        await dbContext.SaveChangesAsync(cancellationToken);

        var message = request.DriverUserId.HasValue
            ? $"Sürücü [{request.DriverUserId.Value}] başarıyla [{vehicle.PlateNumber}] plakalı araca atandı! 🪪"
            : $"[{vehicle.PlateNumber}] plakalı aracın sürücü zimmeti kaldırıldı!";

        return new AssignDriverResponse(
            VehicleId: vehicle.Id,
            DriverUserId: vehicle.DriverUserId,
            Message: message
        );
    }
}
