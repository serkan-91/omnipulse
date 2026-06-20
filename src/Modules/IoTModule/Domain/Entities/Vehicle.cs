using System;
using System.Collections.Generic;
using OmniPulse.BuildingBlocks.Common;
using OmniPulse.BuildingBlocks.Interfaces;

namespace OmniPulse.Modules.IoTModule.Domain.Entities;

/// <summary>
/// Lojistik filosundaki kamyon veya tır gibi araçların dijital ikizi! 🚛
/// Bu araçlar bir sürücüye (DriverUserId) zimmetlenir.
/// </summary>
public class Vehicle : BaseEntity, IAuditableEntity, ITenantEntity, ISoftDelete
{
    public Guid TenantId { get; set; }
    
    // Araç plakası (Örn: "06 PANDA 01")
    public string PlateNumber { get; private set; } = null!;
    
    // Araç markası/modeli (Örn: "Volvo FH16")
    public string Brand { get; private set; } = null!;

    // Zimmetli sürücünün kimliği (Decoupled: TenantModule'deki User'ın Guid ID'si!)
    public Guid? DriverUserId { get; private set; }

    // Bu araca takılı durumdaki ham sensörler/cihazlar
    public ICollection<Device> Devices { get; private set; } = new List<Device>();

    // IAuditableEntity - Denetim/İzleme alanları
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    // ISoftDelete - Güvenli/Yumuşak silme alanları
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }

    // EF Core için boş constructor
    private Vehicle() { }

    public static Vehicle Create(Guid tenantId, string plateNumber, string brand, Guid? driverUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Kiracı ID'si boş bırakılamaz şefim!", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(plateNumber))
            throw new ArgumentException("Plaka numarası boş bırakılamaz!", nameof(plateNumber));

        if (string.IsNullOrWhiteSpace(brand))
            throw new ArgumentException("Marka bilgisi boş bırakılamaz!", nameof(brand));

        return new Vehicle
        {
            TenantId = tenantId,
            PlateNumber = plateNumber.ToUpperInvariant().Trim(),
            Brand = brand.Trim(),
            DriverUserId = driverUserId
        };
    }

    public void AssignDriver(Guid? driverUserId)
    {
        DriverUserId = driverUserId;
    }

    public void UpdateDetails(string plateNumber, string brand)
    {
        PlateNumber = plateNumber.ToUpperInvariant().Trim();
        Brand = brand.Trim();
    }
}
