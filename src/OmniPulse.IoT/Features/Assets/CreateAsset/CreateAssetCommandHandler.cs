using MediatR;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.IoT.Domain.Entities;
using OmniPulse.IoT.Infrastructure.Persistence;

namespace OmniPulse.IoT.Features.Assets.CreateAsset;

public class CreateAssetCommandHandler(
    IoTDbContext dbContext,
    IUserTenantContext userTenantContext)
    : IRequestHandler<CreateAssetCommand, CreateAssetResponse>
{
    public async Task<CreateAssetResponse> Handle(CreateAssetCommand request, CancellationToken cancellationToken)
    {
        // 1. Tenant/Kiracı bağlamını alalım
        if (!userTenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Aktif bir kiracı bağlamı bulunamadı! 🔐");
        }

        var tenantId = userTenantContext.TenantId.Value;

        // 2. Domain Entity'i oluştur (factory metodu)
        var asset = Asset.Create(
            tenantId,
            request.Name,
            request.Type,
            request.ParentAssetId,
            request.ResponsibleUserId,
            request.MetadataJson
        );

        dbContext.Assets.Add(asset);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateAssetResponse(
            Id: asset.Id,
            Name: asset.Name,
            Type: asset.Type,
            ParentAssetId: asset.ParentAssetId,
            ResponsibleUserId: asset.ResponsibleUserId,
            MetadataJson: asset.MetadataJson,
            CreatedAtUtc: asset.CreatedAtUtc
        );
    }
}
