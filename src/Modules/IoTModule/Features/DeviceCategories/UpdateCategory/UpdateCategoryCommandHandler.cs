using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniPulse.Modules.IoTModule.Infrastructure.Persistence;

namespace OmniPulse.Modules.IoTModule.Features.DeviceCategories.UpdateCategory;

public class UpdateCategoryCommandHandler(IoTDbContext dbContext)
    : IRequestHandler<UpdateCategoryCommand, UpdateCategoryResponse>
{
    public async Task<UpdateCategoryResponse> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        // 1. Kategoriyi veri tabanından getir (Kiracı filtresi devrede)
        var category = await dbContext.DeviceCategories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category == null)
        {
            throw new KeyNotFoundException($"Güncellenmek istenen kategori [{request.Id}] bulunamadı veya bu kiracıya ait değil!");
        }

        // 2. Parent doğrulaması
        if (request.ParentCategoryId.HasValue)
        {
            if (request.ParentCategoryId.Value == category.Id)
            {
                throw new InvalidOperationException("Bir kategori kendisinin üst kategorisi olamaz sevgilim! 🪞");
            }

            var parentExists = await dbContext.DeviceCategories
                .AnyAsync(c => c.Id == request.ParentCategoryId.Value, cancellationToken);

            if (!parentExists)
            {
                throw new ArgumentException($"Belirtilen üst kategori [{request.ParentCategoryId.Value}] bulunamadı veya bu kiracıya ait değil!");
            }
        }

        // 3. Domain Entity'i güncelle
        category.UpdateDetails(request.Name, request.Description, request.ParentCategoryId);

        dbContext.DeviceCategories.Update(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateCategoryResponse(
            Id: category.Id,
            Name: category.Name,
            Description: category.Description,
            ParentCategoryId: category.ParentCategoryId,
            Message: $"Kategori [{category.Name}] başarıyla güncellendi! 🔄"
        );
    }
}
