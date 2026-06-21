using System.ComponentModel.DataAnnotations;

namespace OmniPulse.BuildingBlocks.Configuration;

/// <summary>
/// appsettings.json → "ConnectionStrings" bölümünü strongly-typed olarak taşıyan Options sınıfı.
/// TenantModule ve IoTModule, IOptions&lt;DatabaseOptions&gt; ile enjekte ederek
/// connection string'lere magic string olmadan erişir. 🗄️
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";

    [Required(AllowEmptyStrings = false)]
    public string DefaultConnection { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string IoTConnection { get; init; } = string.Empty;
}
