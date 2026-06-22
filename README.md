# OmniPulse Platform

OmniPulse, IoT telemetri verilerini gerçek zamanlı olarak izleyen, otonom alarm kurallarını değerlendiren ve sektöre göre özelleştirilebilir iş akışı/görev motoru barındıran modüler bir SaaS platformudur.

## 📖 Proje Dokümanları ve Yol Haritaları

Geliştirici ortamınızı kurmak, kullanılan teknolojileri incelemek ve proje üzerinde çalışmaya başlamak için aşağıdaki kılavuzları inceleyebilirsiniz:

1. 📋 **[Kurulum, Teknoloji ve Yol Haritası Kılavuzu (omnipulse_todo_list.md)](omnipulse_todo_list.md)**: Projede kullanılan tüm teknolojiler (ve nerede kullanıldıkları), gerekli IDE/programlar, veritabanları, NuGet/npm bileşenleri ve güncel **TODO / Geliştirme Takip Listesi**.
2. 🚗 **[Evrensel Varlık (Asset) Refaktör Yol Haritası (asset_refactoring_plan.md)](asset_refactoring_plan.md)**: Araç (Vehicle) modelinden genel varlık (Asset) yapısına geçiş için aşamalı teknik refaktör planı.
3. ⚙️ **[İş Akışı (Workflow Task Engine) Mimari Planı (workflow_strategy_plan.md)](workflow_strategy_plan.md)**: Olay güdümlü ve NoSQL (AWS DynamoDB) tabanlı iş akışı / görev takip modülü mimari tasarımı.

## 🛠️ Hızlı Başlangıç

Docker üzerinden PostgreSQL veritabanını başlatmak için:
```bash
docker compose up -d
```

Veritabanı şemalarını güncellemek için:
```bash
dotnet ef database update --project src/Modules/TenantModule/TenantModule.csproj --startup-project src/OmniPulse.Identity.API/OmniPulse.Identity.API.csproj --context IdentityDbContext
dotnet ef database update --project src/Modules/IoTModule/IoTModule.csproj --startup-project src/OmniPulse.Identity.API/OmniPulse.Identity.API.csproj --context IoTDbContext
```

Backend API uygulamasını çalıştırmak için:
```bash
dotnet run --project src/OmniPulse.Identity.API/OmniPulse.Identity.API.csproj
```

Arayüz (Frontend) uygulamasını çalıştırmak için:
```bash
cd src/omnipulse-ui
npm install
npm run dev
```
