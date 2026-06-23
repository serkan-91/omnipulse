using System;
using MediatR;

namespace OmniPulse.IoT.Features.DeviceCategories.CreateCategory;

public record CreateCategoryCommand(
    string Name,
    string Description,
    Guid? ParentCategoryId = null
) : IRequest<CreateCategoryResponse>;

public record CreateCategoryResponse(
    Guid Id,
    string Name,
    string Description,
    Guid? ParentCategoryId,
    string Message
);
