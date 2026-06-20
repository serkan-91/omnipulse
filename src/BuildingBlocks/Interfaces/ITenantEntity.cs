using System;

namespace OmniPulse.BuildingBlocks.Interfaces;

/// <summary>
/// Verinin hangi kiracıya (Tenant) ait olduğunu belirten ve veri izolasyonunu garantileyen kalkan! 🛡️
/// </summary>
public interface ITenantEntity
{
    // Kiracının benzersiz kimliği
    Guid TenantId { get; set; }
}
