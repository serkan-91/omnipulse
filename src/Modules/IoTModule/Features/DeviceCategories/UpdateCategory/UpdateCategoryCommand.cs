using System;
using MediatR;

namespace OmniPulse.Modules.IoTModule.Features.DeviceCategories.UpdateCategory;

public record UpdateCategoryCommand(
    Guid Id,
    string Name,
    string Description,
    Guid? ParentCategoryId = null
) : IRequest<UpdateCategoryResponse>;

public record UpdateCategoryResponse(
    Guid Id,
    string Name,
    string Description,
    Guid? ParentCategoryId,
    string Message
);
