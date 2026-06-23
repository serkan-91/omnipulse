# OmniPulse Evrensel Varlık (Asset) Refaktör Yol Haritası

Bu doküman, lojistik odaklı `Vehicle` (Araç) yapısından, sektör bağımsız `Asset` (Varlık) yapısına geçiş için kod tabanında gerçekleştirilecek olan **adım adım teknik refaktör planını** içermektedir.

---

## 🚀 Refaktör Adımları ve Kontrol Listesi

### 1. Kademe: Domain Modelleri ve Enum Tanımlamaları
Bu aşamada veri yapımızın evrenselleşmesi için gerekli temel sınıfları ve enum türlerini hazırlayacağız.

* [ ] **`AssetType` Enum Tanımı:**
  * Dosya: `src/OmniPulse.IoT/Domain/Enums/AssetType.cs`
  * Değerler: `Vehicle`, `ConveyorBelt`, `FreezerIncubator`, `ValveInfrastructure`, `RoomArea`, `Custom`
* [ ] **`Asset` Sınıfının Oluşturulması (Eski `Vehicle` yerine):**
  * Dosya: `src/OmniPulse.IoT/Domain/Entities/Asset.cs`
  * Alanlar: 
    * `Name` (Varlık Adı - örn: "Konveyör Bant-1", "34 ABC 123")
    * `Type` (`AssetType` enum)
    * `ParentAssetId` / `ParentAsset` (Hiyerarşik ağaç yapısı için self-referencing)
    * `ResponsibleUserId` (Sürücü, Operatör veya Laborant Guid'i)
    * `MetadataJson` (Sektörel özelliklerin şemasız saklanacağı JSON string alanı)
* [ ] **`Device` Sınıfının Güncellenmesi:**
  * Dosya: [Device.cs](src/OmniPulse.IoT/Domain/Entities/Device.cs)
  * Değişiklik: `VehicleId` ve `Vehicle` referansları yerine `AssetId` ve `Asset` eklenecek.

---

### 2. Kademe: Altyapı ve Veri Tabanı Katmanı (Infrastructure)
Entity Framework Core konfigürasyonlarının güncellenmesi ve veri tabanı göçünün (migration) hazırlanması.

* [ ] **EF Core Konfigürasyonlarının Güncellenmesi:**
  * Dosya: `src/OmniPulse.IoT/Infrastructure/Persistence/Configurations/AssetConfiguration.cs` (Eski `VehicleConfiguration` yerine oluşturulacak).
  * `MetadataJson` alanı için PostgreSQL JSON/JSONB eşleşmesi veya string dönüşümü tanımlanacak.
* [ ] **`IoTDbContext` Güncellemesi:**
  * Dosya: [IoTDbContext.cs](src/OmniPulse.IoT/Infrastructure/Persistence/IoTDbContext.cs)
  * Değişiklik: `public DbSet<Vehicle> Vehicles` kaldırılacak, yerine `public DbSet<Asset> Assets` eklenecek.
* [ ] **Göç (Migration) İşlemleri:**
  * Terminalden yeni bir migration üretilecek:
    ```bash
    dotnet ef migrations add TransitionToUnifiedAssetModel --project src/OmniPulse.IoT/OmniPulse.IoT.csproj --startup-project src/OmniPulse.Identity.API/OmniPulse.Identity.API.csproj --context IoTDbContext
    ```
  * Veri tabanı güncellenecek:
    ```bash
    dotnet ef database update --project src/OmniPulse.IoT/OmniPulse.IoT.csproj --startup-project src/OmniPulse.Identity.API/OmniPulse.Identity.API.csproj --context IoTDbContext
    ```

---

### 3. Kademe: İş Mantığı (Application & MediatR Command-Queries)
Mevcut iş akışlarının (araç yaratma, sürücü atama) yeni genel yapıya adapte edilmesi.

* [ ] **`CreateVehicle` $\rightarrow$ `CreateAsset` Refaktörü:**
  * Klasör: `src/OmniPulse.IoT/Features/Vehicles/CreateVehicle` ismi `Features/Assets/CreateAsset` yapılacak.
  * Request nesnesi `AssetType` ve opsiyonel `Metadata` (sözlük/JSON) kabul edecek şekilde genişletilecek.
* [ ] **`AssignDriver` $\rightarrow$ `AssignResponsibleUser` Refaktörü:**
  * Klasör: `Features/Vehicles/AssignDriver` ismi `Features/Assets/AssignResponsibleUser` yapılacak.
  * Sadece sürücü değil, herhangi bir sorumlu kullanıcı ID'sinin atanması sağlanacak.
* [ ] **`GetVehicles` $\rightarrow$ `GetAssets` Refaktörü:**
  * Sektör veya türe (`AssetType`) göre filtreleme desteği eklenecek.
* [ ] **Sensör Montaj Akışı (`MountDevice`):**
  * Sensörün araca değil, doğrudan bir Varlığa (`Asset`) monte edilmesi sağlanacak.

---

### 4. Kademe: Güvenlik ve Raporlama Filtreleri
Kullanıcıların yetki sınırlarının (ABAC - Varlık Bazlı Erişim Kontrolü) uygulanması.

* [ ] **Sürücü/Operatör Telemetri Filtresi:**
  * Dosya: [TelemetryQueryExtensions.cs](src/OmniPulse.IoT/Features/Telemetry/TelemetryQueryExtensions.cs)
  * Değişiklik: `DriverUserId` kontrolü, `Asset.ResponsibleUserId` eşleşmesine dönüştürülecek. Böylece fabrika operatörü de sadece kendi bandındaki sensör verilerini görebilecek.
* [ ] **Raporlama API'si:**
  * Dosya: [GetTelemetryReportQueryHandler.cs](src/OmniPulse.IoT/Features/Telemetry/GetTelemetryReport/GetTelemetryReportQueryHandler.cs)
  * Değişiklik: SQL sorguları `v."DriverUserId"` yerine `a."ResponsibleUserId"` kolonuna göre filtreleme yapacak.

---

### 5. Kademe: API Uçları (Presentation)
Dış dünyaya sunulan endpoint'lerin isimlendirme ve parametre güncellemeleri.

* [ ] **`IoTEndpoints.cs` Güncellemesi:**
  * Dosya: [IoTEndpoints.cs](src/OmniPulse.Identity.API/Endpoints/IoTEndpoints.cs)
  * Değişiklik: `/api/iot/vehicles/*` rotaları, `/api/iot/assets/*` olarak güncellenecek.

---

## 🛠️ Tahmini Efor ve Sıralama

Bu refaktör işlemi uygulamanın derleme (compile) sağlığını bozmadan parça parça yapılabilir:
1. **Adım 1:** `AssetType` enum ve `Asset` entity tanımlanır (Derleme bozulmaz).
2. **Adım 2:** `Device` referansı güncellenir ve veri tabanı göçü yapılır. (Bu adımda eski `Vehicle` kodları derleme hatası verir).
3. **Adım 3:** Hata veren `CreateVehicle` vb. komutlar sırayla `Asset` yapılarına dönüştürülür.
4. **Adım 4:** API endpoint'leri ve sorgu filtreleri güncellenerek refaktör tamamlanır.
