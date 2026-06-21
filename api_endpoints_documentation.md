# 🌌 OmniPulse API Uç Noktaları (Endpoints) Dokümantasyonu

Bu doküman, OmniPulse SaaS & IoT platformunun dikey dilim mimarisi (Vertical Slice Architecture) ile yapılandırılmış tüm Minimal API uç noktalarını (endpoints), SignalR hub'larını, veri şemalarını ve güvenlik gereksinimlerini modüler olarak listelemektedir.

---

## 🔐 Genel Güvenlik ve Tenant Mekanizması

Uygulamada yer alan uç noktalarının büyük kısmı kimlik doğrulama gerektirir.
* **Kimlik Doğrulama**: `Authorization: Bearer <JWT_TOKEN>` başlığı ile taşınır.
* **Çoklu Kiracılık (Multi-Tenancy)**: `TenantHydrationMiddleware` aracılığıyla, gelen JWT token içerisindeki `TenantId` claim'inden veya anonim/token öncesi isteklerde `X-Tenant-Id` header değerinden otomatik olarak kiracı doğrulaması yapılır.
* **Veri İzolasyonu (Tenant Isolation)**: Varlıklar (`Assets`), Cihaz Kategorileri (`DeviceCategories`), Alarm Kuralları (`AlarmRules`), Görevler (`Tasks`) ve Telemetri (`Telemetry`) verileri veri tabanında `TenantId` alanına sahiptir. EF Core seviyesindeki **Global Query Filter** (`e => e.TenantId == context.TenantId`) sayesinde her kiracının verisi tamamen kendi sınırları içerisinde korunur. Kayıt ekleme esnasında kiracı damgası (`TenantId`) otomatik basılır; güncelleme esnasında ise kiracı verisinin başka bir kiracıya sızdırılmasını engellemek için `TenantId` alanı değiştirilemez olarak kilitlenir.

---

## 🧑‍🤝‍🧑 1. Tenant Modülü (Identity & Auth)

Kullanıcı kaydı, girişi, JWT token yenileme ve kiracı davet akışlarını yönetir.

### 🔑 POST `/api/auth/login`
* **Açıklama**: Kullanıcı girişi gerçekleştirir. Hatalı girişlerde IP bazlı veya hesap bazlı kilitlenme (Account Lockout) koruması (15 dakika) devrededir.
* **Güvenlik**: Anonim (Herkese Açık)
* **İstek Gövdesi (Request Body)**:
  ```json
  {
    "email": "user@example.com",
    "password": "Password123!",
    "tenantIdentifier": "tenant-slug-veya-id"
  }
  ```
* **Yanıt Şeması (Success Response)**:
  ```json
  {
    "isSuccess": true,
    "token": "eyJhbGciOi...",
    "refreshToken": "rf-tkn-xyz...",
    "expiresAt": "2026-06-21T23:59:59Z"
  }
  ```

### 🔄 POST `/api/auth/refresh`
* **Açıklama**: Süresi dolan JWT token'ı yeni bir JWT token ve döngüsel (rotasyonlu) refresh token ile günceller.
* **Güvenlik**: Anonim (Herkese Açık)
* **İstek Gövdesi (Request Body)**:
  ```json
  {
    "token": "suresi-dolan-jwt-token",
    "refreshToken": "mevcut-refresh-token"
  }
  ```
* **Yanıt Şeması (Success Response)**:
  ```json
  {
    "isSuccess": true,
    "token": "yeni-jwt-token",
    "refreshToken": "yeni-refresh-token"
  }
  ```

### 🔍 GET `/api/tenants/current-status`
* **Açıklama**: Aktif JWT token üzerinden çözümlenen aktif Tenant (Kiracı) durumunu ve paket/durum limitlerini döner.
* **Güvenlik**: Yetkilendirme Gerekli (`Bearer JWT`)
* **Yanıt Şeması (Success Response)**:
  ```json
  {
    "tenantId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    "name": "MomoYuki Lojistik",
    "identifier": "momoyuki-logistics",
    "isActive": true,
    "createdAt": "2026-06-21T12:00:00Z"
  }
  ```

### ✉️ POST `/api/tenants/invite`
* **Açıklama**: Kiracının sahibi (Owner) veya yöneticisi (Admin) tarafından sisteme yeni bir kullanıcı davet etmek için kullanılır. Davet edilen kişiye e-posta gönderilir (Konsola loglanır).
* **Güvenlik**: Yetkilendirme Gerekli (`Bearer JWT`)
* **İstek Gövdesi (Request Body)**:
  ```json
  {
    "email": "candidate@example.com",
    "role": "Operator"
  }
  ```
* **Yanıt Şeması (Success Response)**:
  ```json
  {
    "isSuccess": true,
    "invitationLink": "https://localhost:7122/invite/verify?code=inv-code-123",
    "message": "Davet başarıyla oluşturuldu."
  }
  ```

---

## 📡 2. IoT Modülü (Assets, Devices & Telemetry)

Varlık hiyerarşisi, cihaz atamaları, telemetri yollama, alarm eşikleri kurma ve analitik raporlamaları içerir.

### 🚛 POST `/api/iot/assets`
* **Açıklama**: Evrensel varlık (Asset) modelinde yeni bir varlık ekler. Varlıklar tırlar, fabrika bantları, soğutucu üniteler, vanalar veya odalar olabilir. Hiyerarşik ağaç yapısı (`ParentAssetId`) destekler.
* **Güvenlik**: Yetkilendirme Gerekli (`Bearer JWT`)
* **İstek Gövdesi (Request Body)**:
  ```json
  {
    "type": "Vehicle", // Enums: Vehicle, ConveyorBelt, FreezerIncubator, ValveInfrastructure, RoomArea
    "name": "Volvo FH16 - 34ABC123",
    "parentAssetId": null,
    "responsibleUserId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "metadataJson": "{\"Plate\": \"34ABC123\", \"MaxPayload\": \"25T\"}"
  }
  ```
* **Yanıt Şeması (Success Response)**:
  ```json
  {
    "id": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    "name": "Volvo FH16 - 34ABC123",
    "isSuccess": true
  }
  ```

### 🧑‍✈️ POST `/api/iot/assets/{id:guid}/assign-responsible`
* **Açıklama**: Bir varlığa (örn. tır veya fabrika bandı) sorumlu kullanıcı atar (örn. Driver, Operator).
* **Güvenlik**: Yetkilendirme Gerekli (`Bearer JWT`)
* **İstek Gövdesi (Request Body)**:
  ```json
  {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "role": "Driver",
    "isUnassign": false
  }
  ```

### 🔌 POST `/api/iot/assets/{assetId:guid}/devices/{deviceId:guid}/mount`
* **Açıklama**: IoT cihazını belirli bir varlığa monte eder. Böylece cihazın telemetrileri o varlıkla ilişkilendirilir.
* **Güvenlik**: Yetkilendirme Gerekli (`Bearer JWT`)

### 🌳 GET `/api/iot/assets`
* **Açıklama**: Kiracıya ait tüm varlık listesini döner. Hiyerarşik ağaç yapısında veri çekmek için recursive CTE desteği sunar.
* **Güvenlik**: Yetkilendirme Gerekli (`Bearer JWT`)
* **Sorgu Parametreleri (Query Params)**:
  * `typeFilter` (string, opsiyonel): Belirli tipleri filtreler.
  * `parentAssetId` (guid, opsiyonel): Direkt bir varlığın alt dallarını getirir.
  * `rootAssetId` (guid, opsiyonel): Tek köklü hiyerarşi ağacını indirir.
  * `responsibleUserId` (guid, opsiyonel): Sorumlu kullanıcının ağaçlarını getirir.
  * `includeDescendants` (boolean, opsiyonel): Alt kırılımların hiyerarşik getirilip getirilmeyeceğini belirler.

### 🌳 GET `/api/iot/categories/tree`
* **Açıklama**: Cihaz kategorilerini hiyerarşik ağaç formatında döner.
* **Güvenlik**: Anonim / Yetkilendirme Gerekli değil

### 🏷️ POST / PUT / DELETE `/api/iot/categories`
* **Açıklama**: Cihaz kategorilerinin CRUD işlemlerini yürütür.
* **Güvenlik**: Yetkilendirme Gerekli (`Bearer JWT`)

### 📈 GET `/api/iot/telemetry`
* **Açıklama**: Belirtilen cihaz veya varlığa ait geçmiş telemetri listesini döner.
* **Güvenlik**: Yetkilendirme Gerekli (`Bearer JWT`)

### 📊 GET `/api/iot/reports/cold-chain`
* **Açıklama**: Soğuk zincir analitiği ve sıcaklık sınır ihlallerini çıkaran derin rapor sorgusu. Min/Max/Ortalama sıcaklık değerlerini döner.
* **Güvenlik**: Yetkilendirme Gerekli (`Bearer JWT`)
* **Sorgu Parametreleri (Query Params)**:
  * `deviceId` (guid, opsiyonel)
  * `assetId` (guid, opsiyonel)
  * `startDate` (datetime, varsayılan son 30 gün)
  * `endDate` (datetime, varsayılan şimdi)
  * `metricKey` (string, varsayılan "Temperature")
  * `coldChainThreshold` (double, varsayılan 60.0)

### 📲 POST `/api/iot/telemetry`
* **Açıklama**: IoT cihazlarının doğrudan ham veri gönderdiği (ingest) uç noktadır. Gelen veri önce veritabanına yazılır, ardıl olarak Kinesis Stream'e yönlendirilir. 
* **Ek Güvenlik (SIEM)**: Kayıt dışı veya pasif bir cihaz veri göndermeye çalışırsa sistem hata döner ve **Kinesis üzerinden SIEM Hub'ına anlık güvenlik sızma uyarısı (Security Alert)** fırlatır!
* **Güvenlik**: Anonim (Cihazların erişimi için)
* **İstek Gövdesi (Request Body)**:
  ```json
  {
    "deviceId": "SN-AUPANDA-TEMP",
    "temperature": 24.5,
    "pressure": 1012.3,
    "timestamp": "2026-06-21T23:50:00Z"
  }
  ```

### 🔌 POST `/api/iot/devices/status`
* **Açıklama**: Cihazların online/offline bağlantı durumlarını günceller. Kinesis üzerinden SignalR göstergelerine anlık yansıtılır.
* **Güvenlik**: Anonim
* **İstek Gövdesi (Request Body)**:
  ```json
  {
    "deviceId": "SN-AUPANDA-TEMP",
    "isOnline": true,
    "timestamp": "2026-06-21T23:50:00Z"
  }
  ```

### 🔔 GET & POST `/api/iot/alarms/rules`
* **Açıklama**: Telemetri verilerinde hangi değer aşımında alarm oluşacağını belirleyen eşik değer kurallarını (Alarm Rules) ekler ve listeler.
* **Güvenlik**: Yetkilendirme Gerekli (`Bearer JWT`)

---

## ⚙️ 3. Workflow Modülü (Olay Güdümlü İş Akışları)

Telemetrilerden tetiklenen veya el ile oluşturulan otonom iş akışı görevlerini (Task) yönetir. Veriler şemasız DynamoDB üzerinde saklanır.

### 📝 POST `/api/workflows/definitions`
* **Açıklama**: Yeni bir iş akışı tanımı (Definition) oluşturur. Hangi telemetri aşımında hangi şablonun tetikleneceğini tutar.
* **Güvenlik**: Yetkilendirme Gerekli (`Bearer JWT`)
* **İstek Gövdesi (Request Body)**:
  ```json
  {
    "name": "Yüksek Sıcaklık Acil Müdahale",
    "description": "Sıcaklık değeri belirlenen kuralların dışına çıktığında tetiklenir.",
    "triggerCondition": "temperature > 60",
    "defaultTaskDescription": "Konteyner kapağını kontrol et ve havalandırmayı aç."
  }
  ```

### ⚖️ POST `/api/workflows/policies`
* **Açıklama**: Bir iş akışı görevi tetiklendiğinde görevin hangi kullanıcıya veya role atanacağını belirleyen kural setini (Assignment Policy) atar.
* **Güvenlik**: Yetkilendirme Gerekli (`Bearer JWT`)
* **İstek Gövdesi (Request Body)**:
  ```json
  {
    "workflowDefinitionId": "dddddddd-dddd-dddd-dddd-dddddddddddd",
    "rulesetJson": "{\"Role\": \"Driver\", \"AssetType\": \"Vehicle\"}"
  }
  ```

### 📥 GET `/api/workflows/tasks`
* **Açıklama**: Kiracıya atanmış tüm iş akışı görevlerini (Workflow Tasks) DynamoDB üzerinden listeler.
* **Güvenlik**: Yetkilendirme Gerekli (`Bearer JWT`)
* **Yanıt Şeması (Success Response)**:
  ```json
  [
    {
      "taskId": "7fa85f64-5717-4562-b3fc-2c963f66afa6",
      "tenantId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      "definitionId": "dddddddd-dddd-dddd-dddd-dddddddddddd",
      "assignedToUserId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "status": "Pending", // Pending, InProgress, Completed, Cancelled
      "description": "Konteyner kapağını kontrol et ve havalandırmayı aç.",
      "createdAt": "2026-06-21T23:51:00Z"
    }
  ]
  ```

### 🛠️ PATCH `/api/workflows/tasks/{taskId:guid}/accept`
* **Açıklama**: Görevi üstlenir ve durumunu `InProgress` yapar.
* **Güvenlik**: Yetkilendirme Gerekli (`Bearer JWT`)

### 🏁 PATCH `/api/workflows/tasks/{taskId:guid}/complete`
* **Açıklama**: Görevi tamamlar (`Completed`).
* **Güvenlik**: Yetkilendirme Gerekli (`Bearer JWT`)

### ❌ PATCH `/api/workflows/tasks/{taskId:guid}/cancel`
* **Açıklama**: Görevi iptal eder (`Cancelled`).
* **Güvenlik**: Yetkilendirme Gerekli (`Bearer JWT`)

### 🧪 POST `/api/workflows/demo`
* **Açıklama**: Test amaçlı anında yapay bir iş akışı görevi (Task) tetikler ve bunu DynamoDB'ye yazar.
* **Güvenlik**: Anonim (Kolay test edilebilmesi amacıyla)

---

## 🚨 4. SignalR Real-Time Kanalları (WebSockets)

İstemcilerin gerçek zamanlı veri akışlarını dinlemesi için kullanılan WebSocket kanallarıdır.

### 📢 `/hubs/alarms`
* **Açıklama**: Eşik aşımı durumlarında ve oluşan acil alarmlarda anlık bildirim yayını yapar.
* **Dinlenebilecek Metotlar**:
  * `ReceiveAlarm`: Eşik aşımı ve alarm detaylarını fırlatır.

### 📊 `/hubs/telemetry`
* **Açıklama**: Kinesis Stream üzerinden okunan verileri anlık olarak tarayıcı paneline iletir. Token ile veya JWT yoksa anonim/misafir olarak `"demo-tenant"` grubuna bağlanmayı destekler.
* **Dinlenebilecek Metotlar**:
  * `ReceiveTelemetry`: Canlı telemetri verisi (sıcaklık, basınç).
  * `ReceiveDeviceStatus`: Cihazın online/offline bağlantı durumları.
  * `ReceiveSecurityAlert`: SIEM sızma ve yetkisiz erişim alarmları (flashing warning banner tetikler).
