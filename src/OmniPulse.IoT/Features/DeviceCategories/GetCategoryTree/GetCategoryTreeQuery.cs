using System;
using System.Collections.Generic;
using MediatR;

namespace OmniPulse.IoT.Features.DeviceCategories.GetCategoryTree;

public record GetCategoryTreeQuery : IRequest<List<DeviceCategoryNodeDto>>;

public record DeviceCategoryNodeDto(
    Guid Id,
    string Name,
    string Description,
    Guid? ParentCategoryId,
    List<DeviceCategoryNodeDto> Children
);
