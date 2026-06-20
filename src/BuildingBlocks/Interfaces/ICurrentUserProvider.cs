namespace OmniPulse.BuildingBlocks.Interfaces;

/// <summary>
/// O anki HTTP isteğini atan kullanıcının adını/kimliğini koklayan casus! 🕵️‍♂️
/// </summary>
public interface ICurrentUserProvider
{
    // Kullanıcının kimliğini veya adını döner (Bulunamazsa varsayılan "System" döner)
    string GetCurrentUserId();
}
