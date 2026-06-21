using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.Modules.IoTModule.Infrastructure.Persistence;

namespace OmniPulse.Modules.IoTModule.Infrastructure.Services;

public class IotAssetService(IoTDbContext dbContext) : IIotAssetService
{
    public async Task<AssetDetails?> GetAssetByDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        var device = await dbContext.Devices
            .Include(d => d.Asset)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == deviceId && !d.IsDeleted, cancellationToken);

        if (device?.Asset == null || device.Asset.IsDeleted)
        {
            return null;
        }

        return new AssetDetails(
            device.Asset.Id,
            device.Asset.Name,
            device.Asset.Type.ToString(),
            device.Asset.ResponsibleUserId,
            device.Asset.ParentAssetId,
            device.Asset.MetadataJson,
            device.Name,
            device.SerialNumber
        );
    }

    public async Task<AssetDetails?> GetAssetByIdAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        var asset = await dbContext.Assets
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == assetId && !a.IsDeleted, cancellationToken);

        if (asset == null)
        {
            return null;
        }

        return new AssetDetails(
            asset.Id,
            asset.Name,
            asset.Type.ToString(),
            asset.ResponsibleUserId,
            asset.ParentAssetId,
            asset.MetadataJson
        );
    }
}
