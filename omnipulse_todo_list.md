# OmniPulse Kurulum, Teknoloji ve Yol Haritası Kılavuzu

Bu doküman, OmniPulse SaaS & IoT platformunun geliştirme ortamı kurulumunu yapabilmeniz için gereken **tüm teknolojileri, veritabanlarını, programları, bileşenleri (component)** ve bunları projenin neresinde kullandığımızı detaylandırmaktadır. Ayrıca geliştirme adımlarını takip edebilmeniz için güncel bir **TODO listesi** içermektedir.

---

## 🛠️ 1. Teknoloji Matrisi (Teknolojiler, Programlar, Veritabanları ve Bileşenler)

Aşağıdaki tabloda projenin çalışması için gereken tüm araçlar, nerede kullanıldıkları ve kurulum yapmanız gereken bileşenler ayrı satırlar halinde listelenmiştir:

| Kategori | Teknoloji / Program / Veritabanı | Nerede Kullanıldı? (Proje/Dosya Konumu) | Gerekli Program & Sürüm | Kullanılan Kütüphane / Bileşen (Nuget & npm) | Açıklama / Kurulum Notu |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Backend Çalışma Zamanı** | **.NET 10.0** | Tüm C# backend projeleri (`/src/BuildingBlocks`, `/src/Modules/*`, `/src/OmniPulse.Identity.API`) | **.NET SDK 10.0+** | Microsoft.NET.Sdk, Microsoft.NET.Sdk.Web | Uygulamanın ana çalışma zamanıdır. |
| **IDE / Editör** | **JetBrains Rider / VS Code** | Proje geliştirme ve hata ayıklama (Debug) | **Rider 2024+** veya **VS Code** | C# Dev Kit, Rider .NET eklentileri | `.slnx` (Solution) ve `.DotSettings.user` dosyaları Rider için optimize edilmiştir. |
| **İlişkisel Veritabanı** | **PostgreSQL 16** | Tenant, IoT ve Workflow temel tabloları, konfigürasyonlar | **Docker Desktop / Podman** veya Yerel PostgreSQL | `Npgsql.EntityFrameworkCore.PostgreSQL` (v10.0.9) | Docker üzerinde `omnipulse-postgres` adında PostgreSQL 16 Alpine container'ı çalışmaktadır. [docker-compose.yml](docker-compose.yml) ile ayağa kaldırılır. |
| **NoSQL Veritabanı** | **AWS DynamoDB** | Dinamik ve sektöre göre esneyebilen `WorkflowTask` şemasız verileri | LocalStack (Lokal için) veya AWS Cloud | `AWSSDK.DynamoDBv2` (v3.7.400) | `WorkflowModule` CQRS mimarisinde yüksek okuma/yazma hızı ile çalışacak şekilde kurgulanmıştır. |
| **Bulut Akış Platformu** | **AWS Kinesis Data Streams** | IoT Telemetrilerinin saniyede binlerce veri akışında tıkanmadan alınması (Asenkron Pipeline) | LocalStack (Lokal test için) veya AWS CLI / Console | Entegrasyonu asenkron arka plan Worker'ları üzerinden planlanmaktadır. | Yüksek yüklü telemetri ingestion'ı için event-driven mimaride planlanmıştır. |
| **Frontend Çalışma Zamanı** | **Node.js & React 19** | Kullanıcı arayüzü ve yönetim paneli | **Node.js v20+** ve **npm / yarn** | `react` (v19.2.4), `react-dom` (v19.2.4) | Yönetim paneli arayüz bileşenleri için altyapı oluşturur. |
| **Frontend Framework** | **Next.js 16** | Panel yönlendirme (routing), SSR/SSG rendering ve SSL entegrasyonu | `omnipulse-ui` projesi ([package.json](src/omnipulse-ui/package.json)) | `next` (v16.2.9) | `npm run dev` ile HTTPS destekli (Self-signed) lokal geliştirme sunucusu başlatır. |
| **CSS & Tasarım** | **Tailwind CSS v4** | Arayüz tasarımları ve responsive grid yapıları | `omnipulse-ui` projesi | `tailwindcss` (v4.0.0+), `@tailwindcss/postcss` | Yenilikçi ve premium temalar için modern CSS token'ları sunar. |
| **Sır / Şifre Yönetimi** | **SecretManager & systemd-creds** | JWT anahtarı ve veritabanı şifreleri | **Linux (systemd-creds)** veya **Windows Credential Manager** | Platforma özel P/Invoke (Windows) ve CLI Wrapper (Linux) | [SecretManager.cs](src/OmniPulse.Identity.API/Infrastructure/SecretManager.cs) sınıfı ile sistem düzeyinde şifrelenmiş dosyaları çözer. |
| **API Dokümantasyonu** | **Scalar & OpenAPI** | API uçlarının testi ve tarayıcı üzerinde interaktif dokümantasyon | Web Tarayıcı (API root `/` dizini) | `Scalar.AspNetCore` (v2.16.4), `Microsoft.AspNetCore.OpenApi` (v10.0.9) | Swagger alternatifi interaktif arayüz (DeepSpace temalı) sunar. |
| **Gerçek Zamanlı İletişim** | **SignalR** | Alarm eşik aşımı durumlarında ve çözülmelerinde anlık arayüz tetiklemeleri | [AlarmHub.cs](src/OmniPulse.IoT/Hubs/AlarmHub.cs) | `Microsoft.AspNetCore.SignalR` | İstemcilere `/hubs/alarms` rotası üzerinden gerçek zamanlı WebSocket yayını yapar. |
| **Arka Plan Servisleri** | **.NET Channel & BackgroundService** | Gelen telemetri kuyruklarının ana API akışını bozmadan asenkron çözümlenmesi | [TelemetryAlarmBackgroundProcessor.cs](src/OmniPulse.IoT/Features/Alarms/TelemetryAlarmBackgroundProcessor.cs) | `System.Threading.Channels` | Telemetrileri hafıza içi kuyruğa (`Channel<Telemetry>`) alarak arka planda otonom alarm kontrolü yapar. |

---

## 🔑 2. Sır (Secret) Yönetimi Kurulumu ve Dosya İsimleri

Platformumuz, şifreleri `appsettings.json` gibi düz metin dosyalarında saklamak yerine platforma duyarlı [SecretManager.cs](src/OmniPulse.Identity.API/Infrastructure/SecretManager.cs) ile yönetmektedir. Kurulum yaparken işletim sisteminize göre aşağıdaki konfigürasyonları yapmalısınız:

### A. Linux (Bazzite / systemd-creds Altyapısı)
Linux üzerindeyseniz, `systemd-creds` ile şifrelenmiş credential dosyalarını oluşturabilir veya doğrudan ortam değişkeni (Environment Variable) tanımlayabilirsiniz.

1. **Sistem Düzeyinde Dosyalar (Tercih Edilen):**
   * `/etc/omnipulse-jwt.cred` -> JWT anahtarı (`jwt-key` sırrı için)
   * `/etc/omnipulse-postgres.cred` -> PostgreSQL bağlantı dizesi (`postgres-key` sırrı için)

2. **Kullanıcı Düzeyinde Alternatif Yol:**
   * `~/.config/omnipulse/omnipulse-jwt.cred`
   * `~/.config/omnipulse/omnipulse-postgres.cred`

3. **Geliştirici Ortamı Ortam Değişkenleri (Hızlı Fallback):**
   * `JWT_KEY`="SizinGüvenliJwtAnahtarınızEnAz32Karakter"
   * `POSTGRES_KEY`="Host=localhost;Port=5432;Database=omnipulse_shared;Username=omnipulse_admin;Password=StrongPassword123!;"

### B. Windows (Credential Manager)
Eğer uygulamayı ileride Windows üzerinde test edecekseniz:
* Windows Kimlik Bilgileri Yöneticisi'ne (Credential Manager) giderek `jwt-key` ve `postgres-key` adlarında **Genel Kimlik Bilgileri (Generic Credentials)** eklemelisiniz.

---

## 🚀 3. Kurulum ve Başlangıç Adımları

Geliştirme ortamınızı ayağa kaldırmak için aşağıdaki adımları sırasıyla uygulayabilirsiniz:

### Adım 1: Veritabanlarını Başlatın (Docker Compose)
Proje kök dizininde terminali açın ve PostgreSQL 16 veritabanını Docker üzerinde ayağa kaldırın:
```bash
docker compose up -d
```
> [!NOTE]
> Bu komut, `omnipulse_shared` ve `OmniPulse_IoT` veritabanlarını içeren PostgreSQL container'ını `5432` portunda başlatacaktır.

### Adım 2: Çevre Değişkenlerini (Environment Variables) Ayarlayın
IDE'nizde (Rider/VS Code) veya sisteminizde yukarıdaki **3. Geliştirici Ortamı Ortam Değişkenleri** başlığındaki değişkenleri tanımlayın.

### Adım 3: EF Core Göçlerini (Migrations) Uygulayın
Veritabanı tablolarını oluşturmak için terminalden migrations komutlarını çalıştırın:
```bash
# Tenant Modülü Göçleri
dotnet ef database update --project src/OmniPulse.Tenant/OmniPulse.Tenant.csproj --startup-project src/OmniPulse.Identity.API/OmniPulse.Identity.API.csproj --context IdentityDbContext

# IoT Modülü Göçleri
dotnet ef database update --project src/OmniPulse.IoT/OmniPulse.IoT.csproj --startup-project src/OmniPulse.Identity.API/OmniPulse.Identity.API.csproj --context IoTDbContext

# Workflow Modülü Göçleri
dotnet ef database update --project src/OmniPulse.Workflow/OmniPulse.Workflow.csproj --startup-project src/OmniPulse.Identity.API/OmniPulse.Identity.API.csproj --context WorkflowDbContext
```

### Adım 4: Backend API Projesini Çalıştırın
API katmanını başlatmak için Rider üzerinden veya terminalden aşağıdaki komutu verin:
```bash
dotnet run --project src/OmniPulse.Identity.API/OmniPulse.Identity.API.csproj
```
Tarayıcınızdan `http://localhost:5294` (veya `https://localhost:7122`) adresini ziyaret ederek Scalar API dokümantasyonuna erişebilirsiniz.

---

## 📋 4. Geliştirme Yol Haritası & TODO Listesi

Geliştirme süresince tamamlanmış işler ve gelecek planları aşağıdaki gibidir. Yapacağınız kurulumlara göre bu listeyi takip edebilirsiniz.

### 🏁 Tamamlanmış Görevler (Completed)
- [x] **Modüler Monolit Altyapısı**: API katmanında endpoint ayrıştırmaları ve EF Core bağımlılıkları tamamlandı.
- [x] **Kimlik Doğrulama Güvenlik Kalkanı**: JWT Token üretimi, Login, IP & Account Lockout (15 dk kilit), Rotasyonlu Refresh Token ve `TenantHydrationMiddleware` yazıldı.
- [x] **Tenant Davet Akışı**: Owner/Admin rolleri için e-posta ile davetiye oluşturma mantığı kuruldu.
- [x] **IoT Filo Modeli**: Araç, Cihaz, Sensör ve Kategori self-referencing veri modelleri ve `IngestTelemetry` API endpoint'i yazıldı.
- [x] **Otonom Alarm Motoru**: Eşik kuralları, `AlarmService` değerlendiricisi, `BackgroundService` ile asenkron kontrol kanalları (`Channel<T>`) ve `SignalR` (`AlarmHub`) anlık alarm yayını entegre edildi.
- [x] **Soğuk Zincir Analitik Raporu**: Canan Hanım (Müşteri Temsilcisi) için min/max/ortalama sıcaklık ve zaman aralıklı geçmiş rapor sorguları yazıldı.
- [x] **AWS Kinesis Multi-Shard Tüketici Çarkı**: Tüketici servisi (BackgroundService) tek bir shard üzerinde tıkanıp kalmayacak şekilde concurrent (eşzamanlı) çalışacak şekilde refaktör edildi. Biten ve hata alan görevlerin çift yönlü temizlik mekanizması kuruldu.
- [x] **SignalR & WebSocket Canlı Akış Kapısı**: `/hubs/telemetry` uç noktası üzerinden kiracı bazlı izole WebSocket akışları ve demo misafir grubu desteği eklendi.
- [x] **SIEM Güvenlik ve Bağlantı Takip Entegrasyonu**: Kayıt dışı/askıya alınmış cihazların veri sızma denemeleri Kinesis üzerinden yakalanarak SIEM alarm paneline (Real-time flashing warning banner) ve konsola yönlendirildi. Cihaz online/offline durumları Kinesis aracılığıyla gerçek zamanlı LED göstergelerine bağlandı.

### 🚧 Aktif Planlanan İşler (Planned Backlog)

#### A. Evrensel Varlık (Asset) Refaktörü (Öncelikli)
*Lojistik odaklı araç modelinin, fabrika bantları ve laboratuvar cihazlarını da destekleyecek şekilde güncellenmesi.*
- [x] **`AssetType` Enum Tanımı**: `Vehicle`, `ConveyorBelt`, `FreezerIncubator`, `ValveInfrastructure`, `RoomArea` tiplerinin eklenmesi.
- [x] **`Asset` Entity Dönüşümü**: `Vehicle` tablosunun `Asset` olarak adlandırılması, parent-child hiyerarşisi eklenmesi ve şemasız ek veriler için `MetadataJson` (PostgreSQL JSONB) alanının getirilmesi.
- [x] **EF Core Konfigürasyonları ve Göç**: `IoTDbContext` güncellenerek `TransitionToUnifiedAssetModel` göçünün veritabanına uygulanması.
- [x] **API Endpoint Güncellemeleri**: `/api/iot/vehicles/*` uçlarının `/api/iot/assets/*` olarak güncellenmesi ve `MountDevice` akışının Varlıklara bağlanması.
- [x] **Row-Level Güvenlik Filtresi**: Sürücü bazlı filtrelemenin `Asset.ResponsibleUserId` ile evrenselleştirilmesi.

#### B. İş Akışı (Workflow Task Engine) Entegrasyonu
*Platformun olay güdümlü görev yönetim sistemine kavuşturulması.*
- [x] **`WorkflowModule` Tanımlanması**: Modülün bağımsız projeler halinde C# katmanına eklenmesi.
- [x] **AWS DynamoDB Bağlantısı**: `WorkflowTask` NoSQL doküman yapısının C# sınıfları olarak modellenmesi ve DI kaydı.
- [x] **AWS Kinesis Entegrasyonu**: Gelen telemetrileri anlık Kinesis Stream'e basacak API entegrasyonu ve Kinesis verilerini arka planda okuyacak Worker servisinin kurulması.
- [x] **Görev Yönetim API'leri**: `/api/workflow/tasks/*` uçlarının yazılması (görev oluşturma, aksiyon tamamlama, aktif görev listeleme).

#### C. Entegrasyon ve Panel Arayüzü (Frontend)
- [ ] **Gerçek SMTP/SendGrid Entegrasyonu**: Mock olan `ConsoleEmailSender` sınıfının çalışan bir mail servisi ile değiştirilmesi.
- [x] **`omnipulse-ui` Yönetim Paneli Ekranları (Canlı Akış & Simülatörler)**:
  - Canlı takip ve telemetri grafik ekranlarının Next.js 16 + Tailwind CSS v4 + SignalR ile geliştirilmesi.
  - Entegre cihaz bağlantı ve telemetri simulator panelleri ile sızma denemesi (SIEM) test mekanizmaları.
- [ ] **`omnipulse-ui` Yönetim Paneli Ekranları (Kullanıcı & Alarm Yönetimi)**:
  - Login ve Session yönetimi entegrasyonu.
  - Alarm kuralları yönetim arayüzü.
  - Aktif görevler (Task) listesi ve aksiyon tamamlama butonları.
- [ ] **End-to-End Test Paketi**: Kimlik doğrulama, Ingestion ve Alarm tetikleme akışları için entegrasyon testlerinin hazırlanması.

#### D. Canlı Ortam Hazırlığı (Production Readiness)
- [x] **Kinesis Checkpoint Mekanizması (KCL)**: Geliştirme ortamında kullanılan `LATEST` iterator tipi yerine, canlı ortamda veri kaybını önlemek adına DynamoDB tabanlı checkpointing (`AFTER_SEQUENCE_NUMBER` / `TRIM_HORIZON`) ve lease (kilit) yönetim yapısının entegre edilmesi.

---
**Tarih:** 2026-06-22  
**Yazar:** Antigravity (MomoYuki)  
**Durum:** %100 Uyumlu, Kinesis GSI sorguları optimize edildi, Canlı Geçiş Notları eklendi. Geliştirme testlerine hazır.
