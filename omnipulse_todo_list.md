# OmniPulse Geliştirme Yol Haritası ve Görev Takip Listesi

Bu doküman, OmniPulse SaaS & IoT platformunda tamamlanmış olan ve gelecekte yapılması planlanan tüm geliştirmeleri, mimari kararları ve iş kalemlerini listelemektedir.

* **İlgili PDF Belgesi:** [omnipulse_todo_list.pdf](file:///var/home/Chronic_Panda/RiderProjects/omnipulse/omnipulse_todo_list.pdf) (Proje kök dizinine otomatik derlenip kaydedilmiştir).

---

## 1. Mimarinin Modülerleştirilmesi ve Temizlik (Completed)
- [x] **IoT ve Tenant Modüllerinin Ayrıştırılması**: IoT ve Tenant modülü endpoint'lerini API katmanında [TenantEndpoints.cs](file:///var/home/Chronic_Panda/RiderProjects/omnipulse/src/OmniPulse.Identity.API/Endpoints/TenantEndpoints.cs) ve [IoTEndpoints.cs](file:///var/home/Chronic_Panda/RiderProjects/omnipulse/src/OmniPulse.Identity.API/Endpoints/IoTEndpoints.cs) olarak ayrıştırdık.
- [x] **IoT DbContext Hatalarının Giderilmesi**: [IoTDbContext.cs](file:///var/home/Chronic_Panda/RiderProjects/omnipulse/src/Modules/IoTModule/Infrastructure/Persistence/IoTDbContext.cs) sınıfını oluşturduk, EF Core paket bağımlılıklarını ekledik ve model kısıtlamalarını `TelemetryConfiguration` ile tanımladık.
- [x] **Git & Sürüm Kontrolü Temizliği**: appsettings gibi hassas dosyaları takipten çıkardık, [.gitignore](file:///var/home/Chronic_Panda/RiderProjects/omnipulse/.gitignore) dosyasını güncelledik ve depoyu temizledik.

---

## 2. Kimlik Doğrulama ve Güvenlik Altyapısı (Completed)
- [x] **JWT Token Üretim Altyapısı**: [TokenService.cs](file:///var/home/Chronic_Panda/RiderProjects/omnipulse/src/OmniPulse.Identity.API/Services/TokenService.cs) ile JWT üretimini ve doğrulanmasını sağladık.
- [x] **Kullanıcı Giriş Akışı (Login)**: Hatalı giriş sayacı içeren ve kaba kuvvet saldırılarına karşı korumalı olan [LoginCommandHandler.cs](file:///var/home/Chronic_Panda/RiderProjects/omnipulse/src/Modules/TenantModule/Features/Auth/Login/LoginCommandHandler.cs) geliştirildi.
- [x] **Rotasyonlu Refresh Token Mekanizması**: Tek kullanımlık, rotasyonlu refresh token sistemi kuruldu. Token hırsızlığını engellemek için mükerrer kullanım durumunda kullanıcının tüm açık oturumlarını kapatan güvenlik mekanizması eklendi ([RefreshCommandHandler.cs](file:///var/home/Chronic_Panda/RiderProjects/omnipulse/src/Modules/TenantModule/Features/Auth/Refresh/RefreshCommandHandler.cs)).
- [x] **Tenant Hydration Middleware (Token Okuyucu Kalkan)**: Gelen isteklerdeki JWT veya başlık bilgilerinden kiracıyı (Tenant) veri tabanından sorgulayıp doğrulayan ve bağlama enjekte eden [TenantHydrationMiddleware.cs](file:///var/home/Chronic_Panda/RiderProjects/omnipulse/src/OmniPulse.Identity.API/Middleware/TenantHydrationMiddleware.cs) yazıldı.
- [x] **IP ve Hesap Kilitleme (Account Lockout)**: Aynı IP'den 15 dakika içinde 5 kez başarısız deneme yapıldığında o IP'yi 15 dakika kilitleyen güvenlik kalkanı login sürecine entegre edildi.

---

## 3. Kiracı ve Kullanıcı İlişkileri (Completed)
- [x] **Kullanıcı Davet Akışı (Tenant Invitation Flow)**: Sadece `Owner` veya `Admin` yetkisindeki kullanıcıların kiracıya e-posta ile üye davet etmesini sağlayan [InviteUserCommandHandler.cs](file:///var/home/Chronic_Panda/RiderProjects/omnipulse/src/Modules/TenantModule/Features/Tenants/InviteUser/InviteUserCommandHandler.cs) yazıldı. Mevcut kullanıcılar doğrudan eklenirken, sisteme kaydı olmayanlar için pasif davetli profili oluşturuluyor.

---

## 4. IoT ve Filo Mantıksal Modellemesi (Completed)
- [x] **Araç (Vehicle) Entity'si**: Cihazları gruplamak ve sürücüye zimmetlemek için [Vehicle.cs](file:///var/home/Chronic_Panda/RiderProjects/omnipulse/src/Modules/IoTModule/Domain/Entities/Vehicle.cs) eklendi. (Decoupled: `DriverUserId` referansı ile modüller arası bağımsızlık korundu).
- [x] **Dinamik Kategori Ağacı (DeviceCategory)**: Cihazların sınırsız hiyerarşik kırılımda gruplanabilmesi için self-referencing (kendi kendini işaret eden) [DeviceCategory.cs](file:///var/home/Chronic_Panda/RiderProjects/omnipulse/src/Modules/IoTModule/Domain/Entities/DeviceCategory.cs) eklendi.
- [x] **Sensör Donanımı (Device) Entegrasyonu**: Plaka grubuna ve dinamik kategoriye bağlanabilen [Device.cs](file:///var/home/Chronic_Panda/RiderProjects/omnipulse/src/Modules/IoTModule/Domain/Entities/Device.cs) eklendi.
- [x] **Telemetri (Telemetry) Relational Geçişi**: Telemetri verilerini seri numarası eşleştirmesi ile [Device.cs](file:///var/home/Chronic_Panda/RiderProjects/omnipulse/src/Modules/IoTModule/Domain/Entities/Device.cs) nesnesine Guid tabanlı bağladık ([Telemetry.cs](file:///var/home/Chronic_Panda/RiderProjects/omnipulse/src/Modules/IoTModule/Domain/Entities/Telemetry.cs)).
- [x] **Ham Telemetri Ingest Entegrasyonu**: [IngestTelemetryCommandHandler.cs](file:///var/home/Chronic_Panda/RiderProjects/omnipulse/src/Modules/IoTModule/Features/Telemetry/IngestTelemetry/IngestTelemetryCommandHandler.cs) güncellenerek, gelen seri numarasına sahip cihazın aktiflik durumu ve kiracı eşleşmesi sorgulanıp veri tabanına mühürlenmesi sağlandı.

---

## 5. Gelecekte Yapılacaklar (Planned Backlog)
- [ ] **Sürücü Bazlı Telemetri Kısıtlaması (Driver Row-Level Filter)**: Hasan Kaptan'ın (sürücü) sadece kendisine zimmetli tırın telemetrilerini görebilmesi için API sorgularına `DriverUserId` filtresinin entegre edilmesi.
- [ ] **Dinamik Kategori Yönetim API'leri**: Kiracıların panel üzerinden kendi cihaz kategorilerini oluşturabilmesi, düzenleyebilmesi ve ağaç yapısını yönetebilmesi için CRUD endpoint'lerinin yazılması.
- [ ] **Araç ve Donanım Yönetim API'leri**: Yeni araç ekleme, araca sürücü atama, araca sensör/cihaz montajı yapma işlemleri için yönetim endpoint'lerinin geliştirilmesi.
- [ ] **Anlık Alarm ve Bildirim Sistemi**: Telemetri verisinde kritik eşik aşıldığında (Örn: sıcaklık > 60 derece) Mehmet Usta'ya (teknisyen) otomatik bildirim (Websocket/SignalR veya E-posta) gönderilmesi.
- [ ] **Geçmiş Telemetri Raporlama Arayüzü**: Canan Hanım (Müşteri Hizmetleri) için soğuk zincir kırılma raporları ve sensör geçmişi dökümü yapacak analitik API sorgularının (Örn: Min/Max/Ortalama sıcaklık) kurgulanması.

---
**Tarih:** 2026-06-20  
**Yazar:** Antigravity (MomoYuki)  
**Durum:** %100 Uyumlu, Derleme Başarılı, DB Güncel.
