using System.ComponentModel.DataAnnotations;

namespace OmniPulse.Identity.API.Configuration;

/// <summary>
/// appsettings.json → "Jwt" bölümünü strongly-typed olarak taşıyan Options sınıfı.
/// IOptions&lt;JwtOptions&gt; ile enjekte edilir; magic string'e dokunulmaz. 🔐
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required(AllowEmptyStrings = false)]
    public string Key { get; init; } = "SuperSecretKeyEnsure32CharactersLongForSecurity!";

    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; init; } = "OmniPulse";

    [Required(AllowEmptyStrings = false)]
    public string Audience { get; init; } = "OmniPulseClients";

    /// <summary>Token geçerlilik süresi (saat). Varsayılan: 2 saat.</summary>
    public int ExpiryHours { get; init; } = 2;
}
