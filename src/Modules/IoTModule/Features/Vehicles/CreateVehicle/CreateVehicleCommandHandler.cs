using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.Modules.IoTModule.Domain.Entities;
using OmniPulse.Modules.IoTModule.Infrastructure.Persistence;

namespace OmniPulse.Modules.IoTModule.Features.Vehicles.CreateVehicle;

public class CreateVehicleCommandHandler(
    IoTDbContext dbContext,
    IUserTenantContext userTenantContext)
    : IRequestHandler<CreateVehicleCommand, CreateVehicleResponse>
{
    public async Task<CreateVehicleResponse> Handle(CreateVehicleCommand request, CancellationToken cancellationToken)
    {
        // 1. Tenant/Kiracı bağlamını alalım
        if (!userTenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Aktif bir kiracı bağlamı bulunamadı şefim! 🔐");
        }

        var tenantId = userTenantContext.TenantId.Value;
        var plateNormalized = request.PlateNumber.ToUpperInvariant().Trim();

        // 2. İş kuralları: Aynı plaka ile mükerrer araç kaydı engellenir!
        var exists = await dbContext.Vehicles
            .AnyAsync(v => v.PlateNumber == plateNormalized, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"[{plateNormalized}] plakalı araç zaten filoda kayıtlı!");
        }

        // 3. Domain Entity'i oluştur
        var vehicle = Vehicle.Create(
            tenantId,
            request.PlateNumber,
            request.Brand,
            request.DriverUserId
        );

        dbContext.Vehicles.Add(vehicle);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateVehicleResponse(
            Id: vehicle.Id,
            PlateNumber: vehicle.PlateNumber,
            Brand: vehicle.Brand,
            DriverUserId: vehicle.DriverUserId,
            Message: $"Plakası [{vehicle.PlateNumber}] olan araç başarıyla filoya eklendi! 🚛"
        );
    }
}
