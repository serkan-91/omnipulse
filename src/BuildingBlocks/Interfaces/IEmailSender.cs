using System.Threading.Tasks;

namespace OmniPulse.BuildingBlocks.Interfaces;

/// <summary>
/// Küresel ölçekte (Google/Microsoft) olduğu gibi, bildirim kanallarını
/// soyutlayan e-posta gönderim arayüzü! ✉️🚀
/// </summary>
public interface IEmailSender
{
    Task SendEmailAsync(string to, string subject, string body);
}
