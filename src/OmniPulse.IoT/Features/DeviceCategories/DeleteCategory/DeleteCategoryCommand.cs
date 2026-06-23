using System;
using MediatR;

namespace OmniPulse.IoT.Features.DeviceCategories.DeleteCategory;

public record DeleteCategoryCommand(Guid Id) : IRequest<DeleteCategoryResponse>;

public record DeleteCategoryResponse(
    bool IsSuccess,
    string Message
);
