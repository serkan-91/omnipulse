using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniPulse.BuildingBlocks.Interfaces;

namespace OmniPulse.Identity.API.Services;

public partial class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task SendEmailAsync(string to, string subject, string body)
    {
        LogEPosta(to, subject, DateTime.UtcNow, body);

        return Task.CompletedTask;
    }

    [LoggerMessage(LogLevel.Information,
    """
            ================================================================================
            📧 E-POSTA BİLDİRİMİ GÖNDERİLDİ!
            --------------------------------------------------------------------------------
            Alıcı   : {To}
            Konu    : {Subject}
            Zaman   : {DateTime}
            Gönderilen İçerik:

            {Body}
            ================================================================================
            """)]
    partial void LogEPosta(string to, string subject, DateTime dateTime, string body);
}
