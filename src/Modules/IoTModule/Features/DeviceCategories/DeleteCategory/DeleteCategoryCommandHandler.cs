using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniPulse.Modules.IoTModule.Infrastructure.Persistence;

namespace OmniPulse.Modules.IoTModule.Features.DeviceCategories.DeleteCategory;

public class DeleteCategoryCommandHandler(IoTDbContext dbContext)
    : IRequestHandler<DeleteCategoryCommand, DeleteCategoryResponse>
{
    public async Task<DeleteCategoryResponse> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        // 1. Kategoriyi getir (Kiracı filtresi devrede)
        var category = await dbContext.DeviceCategories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category == null)
        {
            throw new KeyNotFoundException($"Silinmek istenen kategori [{request.Id}] bulunamadı veya bu kiracıya ait değil!");
        }

        // 2. İlişki / Bütünlük kontrolleri (Data Integrity)
        // Alt kategori var mı kontrolü
        var hasSubcategories = await dbContext.DeviceCategories
            .AnyAsync(c => c.ParentCategoryId == request.Id, cancellationToken);

        if (hasSubcategories)
        {
            throw new InvalidOperationException("Bu kategoriye bağlı alt kategoriler mevcut! Önce alt kategorileri silmeli veya taşımalısınız.");
        }

        // Kategoriye bağlı cihaz var mı kontrolü
        var hasDevices = await dbContext.Devices
            .AnyAsync(d => d.CategoryId == request.Id, cancellationToken);

        if (hasDevices)
        {
            throw new InvalidOperationException("Bu kategoriye atanmış cihazlar/sensörler mevcut! Önce cihazların kategorisini değiştirmelisiniz.");
        }

        // 3. Remove metodu soft-delete tetikler
        dbContext.DeviceCategories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new DeleteCategoryResponse(
            IsSuccess: true,
            Message: $"Kategori [{category.Name}] başarıyla silindi. 🗑️"
        );
    }
}
