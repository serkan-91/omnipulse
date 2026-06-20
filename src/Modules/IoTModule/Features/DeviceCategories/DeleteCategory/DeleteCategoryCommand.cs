using System;
using MediatR;

namespace OmniPulse.Modules.IoTModule.Features.DeviceCategories.DeleteCategory;

public record DeleteCategoryCommand(Guid Id) : IRequest<DeleteCategoryResponse>;

public record DeleteCategoryResponse(
    bool IsSuccess,
    string Message
);
