using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.Modules.IoTModule.Domain.Entities;
using OmniPulse.Modules.IoTModule.Infrastructure.Persistence;

namespace OmniPulse.Modules.IoTModule.Features.DeviceCategories.CreateCategory;

public class CreateCategoryCommandHandler(
    IoTDbContext dbContext,
    IUserTenantContext userTenantContext)
    : IRequestHandler<CreateCategoryCommand, CreateCategoryResponse>
{
    public async Task<CreateCategoryResponse> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        // 1. Tenant/Kiracı bağlamını alalım
        if (!userTenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Aktif bir kiracı bağlamı bulunamadı şefim! 🔐");
        }

        var tenantId = userTenantContext.TenantId.Value;

        // 2. Parent Category doğrulaması (belirtilmişse)
        if (request.ParentCategoryId.HasValue)
        {
            var parentExists = await dbContext.DeviceCategories
                .AnyAsync(c => c.Id == request.ParentCategoryId.Value, cancellationToken);

            if (!parentExists)
            {
                throw new ArgumentException($"Belirtilen üst kategori [{request.ParentCategoryId.Value}] bulunamadı veya bu kiracıya ait değil!");
            }
        }

        // 3. Domain Entity'i oluşturup kaydetme
        var category = DeviceCategory.Create(
            tenantId,
            request.Name,
            request.Description,
            request.ParentCategoryId
        );

        dbContext.DeviceCategories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateCategoryResponse(
            Id: category.Id,
            Name: category.Name,
            Description: category.Description,
            ParentCategoryId: category.ParentCategoryId,
            Message: $"Kategori [{category.Name}] başarıyla oluşturuldu! 🏷️"
        );
    }
}
