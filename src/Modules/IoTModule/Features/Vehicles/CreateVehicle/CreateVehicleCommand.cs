using System;
using MediatR;

namespace OmniPulse.Modules.IoTModule.Features.Vehicles.CreateVehicle;

public record CreateVehicleCommand(
    string PlateNumber,
    string Brand,
    Guid? DriverUserId = null
) : IRequest<CreateVehicleResponse>;

public record CreateVehicleResponse(
    Guid Id,
    string PlateNumber,
    string Brand,
    Guid? DriverUserId,
    string Message
);
