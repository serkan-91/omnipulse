using System;

namespace OmniPulse.Modules.TenantModule.Domain.Entities;

/// <summary>
/// Çoklu veritabanı (Multi-Tenant) mimarimizin beyni ve haritası!
/// Her kiracının hangi veritabanına, hangi şifreyle bağlanacağını burada mühürlüyoruz.
/// </summary>
public class Tenant
{
    // Kiracının benzersiz kurumsal kimliği (Haritanın anahtarı!)
    public Guid Id { get; private set; }

    // Kiracının görünen adı (Örn: "PandaBerry Şirketi")
    public string Name { get; private set; } = null!;

    // URL'den veya istekten kiracıyı yakalayacağımız sinsi takma ad (Örn: "pandaberry")
    public string Identifier { get; private set; } = null!;

    // Kiracıya özel izole PostgreSQL connection string'i!
    public string? ConnectionString { get; private set; }

    // Kiracının lisans durumu veya sistemde aktif olup olmadığı
    public bool IsActive { get; private set; }
    
    // Sistem abonelik bitiş tarihi
    public DateTime SubscriptionEndDate { get; private set; }

    // Entity Framework Core'un sinsi arka planı için boş constructor
    private Tenant() { }

    // Domain Driven Design (DDD) kurallarına uygun, tertemiz ve güvenli bir nesne yaratma metodu!
    public static Tenant Create(string name, string identifier, string? connectionString, DateTime subscriptionEndDate)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Müşteri adı boş bırakılamaz, tutkum!", nameof(name));

        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Tenant identifier boş olamaz, sinsi bir şeyler dönüyor!", nameof(identifier));

        // BAK SEVGİLİM! Private set kurallarını ezmemek için nesneyi böyle doğuruyoruz! 😉🏎️💨
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            Identifier = identifier.ToLowerInvariant().Trim(),
            ConnectionString = connectionString,
            IsActive = true,
            SubscriptionEndDate = subscriptionEndDate
        };

        return tenant;
    }

    // Kiracıyı dondurmak veya aktif etmek için tatlı bir metod
    public void UpdateStatus(bool isActive)
    {
        IsActive = isActive;
    }
}
