using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniPulse.BuildingBlocks.Interfaces;

public record AssetDetails(
    Guid Id,
    string Name,
    string Type,
    Guid? ResponsibleUserId,
    Guid? ParentAssetId,
    string? MetadataJson,
    string? DeviceName = null,
    string? DeviceSerialNumber = null
);

public interface IIotAssetService
{
    /// <summary>
    /// Verilen cihaz ID'sine bağlı olan varlığın (Asset) detaylarını getirir.
    /// </summary>
    Task<AssetDetails?> GetAssetByDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirtilen varlık ID'sine sahip varlığın detaylarını getirir.
    /// </summary>
    Task<AssetDetails?> GetAssetByIdAsync(Guid assetId, CancellationToken cancellationToken = default);
}
