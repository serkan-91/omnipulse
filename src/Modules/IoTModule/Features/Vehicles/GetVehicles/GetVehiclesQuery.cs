using System;
using System.Collections.Generic;
using MediatR;

namespace OmniPulse.Modules.IoTModule.Features.Vehicles.GetVehicles;

public record GetVehiclesQuery(
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedVehiclesResult>;

public record PagedVehiclesResult(
    IReadOnlyList<VehicleDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record VehicleDto(
    Guid Id,
    string PlateNumber,
    string Brand,
    Guid? DriverUserId,
    List<DeviceDto> MountedDevices
);

public record DeviceDto(
    Guid Id,
    string Name,
    string SerialNumber,
    bool IsActive,
    Guid? CategoryId,
    string? CategoryName
);
