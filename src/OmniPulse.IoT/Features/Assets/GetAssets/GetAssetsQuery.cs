using MediatR;

namespace OmniPulse.IoT.Features.Assets.GetAssets;

public record GetAssetsQuery(
    string? TypeFilter = null,
    Guid? ParentAssetId = null,

    /// <summary>
    /// Recursive CTE modu: Bu ID'li varlıktan başlayarak tüm alt ağacı (tüm nesiller) döner.
    /// Null ise normal filtreleme çalışır.
    /// </summary>
    Guid? RootAssetId = null,

    /// <summary>
    /// Çok-köklü Recursive CTE modu: Bu kullanıcının DOĞRUDAN sorumlu olduğu
    /// tüm varlıkları ANCHOR olarak alır ve hepsinin alt ağaçlarını tek sorguda çeker.
    /// Hüseyin Bey hem Bant-1 hem Bant-2 şefiyse, ikisinin de tüm alt dalları gelir.
    /// </summary>
    Guid? ResponsibleUserId = null,

    /// <summary>
    /// true → WITH RECURSIVE CTE çalışır (RootAssetId veya ResponsibleUserId ile birlikte kullanılır).
    /// false → Yalnızca direkt eşleşme / 1 seviye çocuk döner.
    /// </summary>
    bool IncludeDescendants = false

) : IRequest<IEnumerable<AssetDto>>;

public record AssetDto(
    Guid Id,
    string Name,
    string Type,
    string TypeName,
    Guid? ParentAssetId,
    Guid? ResponsibleUserId,
    string? MetadataJson,
    int DeviceCount,

    /// <summary>
    /// Recursive sorgularda varlığın ağaçtaki derinliği (0 = kök).
    /// Normal sorgularda her zaman 0 döner.
    /// </summary>
    int Depth = 0
);
