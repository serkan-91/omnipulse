using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniPulse.Modules.IoTModule.Infrastructure.Persistence;

namespace OmniPulse.Modules.IoTModule.Features.DeviceCategories.GetCategoryTree;

public class GetCategoryTreeQueryHandler(IoTDbContext dbContext)
    : IRequestHandler<GetCategoryTreeQuery, List<DeviceCategoryNodeDto>>
{
    public async Task<List<DeviceCategoryNodeDto>> Handle(GetCategoryTreeQuery request, CancellationToken cancellationToken)
    {
        // Kiracı filtresi DbContext seviyesinde otomatik uygulanır! 🛡️
        var allCategories = await dbContext.DeviceCategories
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Hiyerarşik yapıyı oluşturmak için bir harita (lookup) oluşturuyoruz
        var lookup = allCategories.ToLookup(c => c.ParentCategoryId);

        List<DeviceCategoryNodeDto> BuildTree(Guid? parentId)
        {
            return lookup[parentId]
                .Select(c => new DeviceCategoryNodeDto(
                    c.Id,
                    c.Name,
                    c.Description,
                    c.ParentCategoryId,
                    BuildTree(c.Id)
                ))
                .ToList();
        }

        // Kök kategorileri (Parent'ı null olanlar) çekip ağacı başlatıyoruz
        return BuildTree(null);
    }
}
