using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniPulse.BuildingBlocks.Interfaces;

namespace OmniPulse.Identity.API.Services;

public class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task SendEmailAsync(string to, string subject, string body)
    {
        logger.LogInformation("""
            ================================================================================
            📧 E-POSTA BİLDİRİMİ GÖNDERİLDİ!
            --------------------------------------------------------------------------------
            Alıcı   : {To}
            Konu    : {Subject}
            Zaman   : {DateTime}
            Gönderilen İçerik:
            
            {Body}
            ================================================================================
            """, to, subject, DateTime.UtcNow, body);

        return Task.CompletedTask;
    }
}
