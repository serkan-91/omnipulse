using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniPulse.Modules.IoTModule.Infrastructure.Persistence;

namespace OmniPulse.Modules.IoTModule.Features.Vehicles.MountDevice;

public class MountDeviceCommandHandler(IoTDbContext dbContext)
    : IRequestHandler<MountDeviceCommand, MountDeviceResponse>
{
    public async Task<MountDeviceResponse> Handle(MountDeviceCommand request, CancellationToken cancellationToken)
    {
        // 1. Cihazı getir (Kiracı filtresi devrede)
        var device = await dbContext.Devices
            .FirstOrDefaultAsync(d => d.Id == request.DeviceId, cancellationToken);

        if (device == null)
        {
            throw new KeyNotFoundException($"Montaj yapılacak cihaz [{request.DeviceId}] bulunamadı veya bu kiracıya ait değil!");
        }

        string message;

        // 2. Eğer araç belirtilmişse aracı getir ve doğrula
        if (request.VehicleId.HasValue)
        {
            var vehicle = await dbContext.Vehicles
                .FirstOrDefaultAsync(v => v.Id == request.VehicleId.Value, cancellationToken);

            if (vehicle == null)
            {
                throw new KeyNotFoundException($"Montaj yapılacak araç [{request.VehicleId.Value}] bulunamadı veya bu kiracıya ait değil!");
            }

            // Cihazı araca ata
            device.AssignToVehicle(vehicle.Id);
            message = $"[{device.Name}] cihazı başarıyla [{vehicle.PlateNumber}] plakalı araca monte edildi! 🔌🌡️";
        }
        else
        {
            // Cihazı araçtan sök (demonte et)
            device.AssignToVehicle(null);
            message = $"[{device.Name}] cihazı araçtan başarıyla söküldü (demonte edildi).";
        }

        dbContext.Devices.Update(device);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new MountDeviceResponse(
            DeviceId: device.Id,
            VehicleId: device.VehicleId,
            Message: message
        );
    }
}
