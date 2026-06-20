using System;
using MediatR;

namespace OmniPulse.Modules.IoTModule.Features.Vehicles.AssignDriver;

public record AssignDriverCommand(
    Guid VehicleId,
    Guid? DriverUserId
) : IRequest<AssignDriverResponse>;

public record AssignDriverResponse(
    Guid VehicleId,
    Guid? DriverUserId,
    string Message
);
