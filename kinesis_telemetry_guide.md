# OmniPulse AWS Kinesis Telemetry Akış Kılavuzu (Telemetry/Kinesis)

Bu kılavuz, **OmniPulse** projesinde AWS Kinesis Data Streams platformunun asenkron telemetri akışları ve iş akışı tetiklemeleri için nasıl yapılandırıldığını, veri şemalarını, senaryoları ve gelecekteki kullanım alanlarını detaylandırmaktadır.

---

## 🗺️ Genel Mimari Şeması

Aşağıdaki diyagramda IoT cihazlarından gelen telemetri verilerinin sisteme girişinden, Kinesis Data Streams üzerinden asenkron olarak tüketilmesi ve DynamoDB üzerinde iş akışı görevlerine dönüştürülmesine kadar olan tüm akış gösterilmektedir:

```mermaid
graph TD
    Device[IoT Cihazı / Simülatör] -->|1. HTTP POST / Ingest| IngestHandler[IngestTelemetryCommandHandler]
    IngestHandler -->|2. Save| Postgres[(PostgreSQL DbContext)]
    IngestHandler -->|3. Publish Async| Publisher[KinesisTelemetryPublisher]
    
    subgraph AWS Kinesis Data Stream (omnipulse-telemetry-stream)
        Publisher -->|4. PutRecord / PartitionKey: SerialNumber| Shard0[Shard 0]
        Publisher -->|4. PutRecord| Shard1[Shard 1]
    end

    subgraph Workflow Background Service (WorkflowModule)
        Consumer[KinesisTelemetryConsumer] -->|5. Concurrent Polling| Shard0
        Consumer -->|5. Concurrent Polling| Shard1
        
        Consumer -->|6. ProcessRecord / MediatR| ProcessHandler[ProcessTelemetryEventCommandHandler]
    end
    
    ProcessHandler -->|7. Check Idempotency| DynamoDB[(DynamoDB TaskStore)]
    ProcessHandler -->|8. Evaluate Rules| RuleEngine[WorkflowRuleEvaluator]
    ProcessHandler -->|9. Create Task| DynamoDB
    ProcessHandler -->|10. Send Notification| NotifyService[NotificationService]
```

---

## 📨 1. Telemetri Yayınlama (Yayıncı - Publisher)

Cihazlardan gelen ham verilerin doğrulanması, PostgreSQL veritabanına mühürlenmesi ve ardından AWS Kinesis'e aktarılması sürecidir.

* **İlgili Sınıf**: [KinesisTelemetryPublisher](file:///var/home/Chronic_Panda/RiderProjects/omnipulse/src/Modules/IoTModule/Infrastructure/Streaming/KinesisTelemetryPublisher.cs)
* **İşleyici Sınıf**: [IngestTelemetryCommandHandler](file:///var/home/Chronic_Panda/RiderProjects/omnipulse/src/Modules/IoTModule/Features/Telemetry/IngestTelemetry/IngestTelemetryCommandHandler.cs)

### ⚙️ Çalışma Mantığı
1. Cihazdan gelen istek `IngestTelemetryCommand` ile karşılanır.
2. Seri numarası doğrulanır ve aktifliği kontrol edilir.
3. Telemetri verisi PostgreSQL'e yazılır.
4. [KinesisTelemetryPublisher.PublishAsync](file:///var/home/Chronic_Panda/RiderProjects/omnipulse/src/Modules/IoTModule/Infrastructure/Streaming/KinesisTelemetryPublisher.cs#L33) metodu çağrılır.
   * **Partition Key** olarak cihazın `SerialNumber` değeri verilir. Bu sayede aynı cihaza ait tüm telemetri kayıtları Kinesis'te aynı Shard'a sıralı olarak yerleştirilir.

### 📝 Yayınlanan Telemetri Şeması (JSON Payload)
```json
{
  "TelemetryId": "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d",
  "DeviceSerialNumber": "DEV-TRUCK-452",
  "TenantId": "9f8e7d6c-5b4a-3f2e-1d0c-9b8a7f6e5d4c",
  "Temperature": 6.5,
  "Pressure": 1013.2,
  "Timestamp": "2026-06-21T20:12:00Z"
}
```

---

## 📥 2. Telemetri Tüketimi (Tüketici - Consumer)

Kinesis Data Streams'ten akan verileri arka planda asenkron olarak okuyan, hatalarda tekrar deneme uygulayan ve iş akışlarına yönlendiren arka plan servisidir.

* **İlgili Sınıf**: [KinesisTelemetryConsumer](file:///var/home/Chronic_Panda/RiderProjects/omnipulse/src/Modules/WorkflowModule/Infrastructure/Services/KinesisTelemetryConsumer.cs)
* **Şema Sınıfı**: [TelemetryEventPayload](file:///var/home/Chronic_Panda/RiderProjects/omnipulse/src/Modules/WorkflowModule/Infrastructure/Services/KinesisTelemetryConsumer.cs#L15)

### ⚙️ Çalışma Mantığı
1. `KinesisTelemetryConsumer` bir `BackgroundService` olarak ayağa kalkar.
2. `ExecuteAsync` içinde her 15 saniyede bir `DescribeStreamAsync` ile aktif shard listesi çekilir.
3. Her shard için asenkron bir `ProcessShardAsync` görevi başlatılır (eğer o shard için zaten aktif bir task yoksa).
4. `ProcessShardAsync` içerisinde `GetRecordsAsync` ile veriler 100'erli paketler halinde çekilir.
5. Her kayıt için [ProcessRecordWithRetryAsync](file:///var/home/Chronic_Panda/RiderProjects/omnipulse/src/Modules/WorkflowModule/Infrastructure/Services/KinesisTelemetryConsumer.cs#L182) çağrılır:
   * **Tekrar Deneme (Retry)**: İş akışı tetiklenirken hata oluşursa, üstel bekleme süresiyle (Backoff) maksimum 3 deneme yapılır.
   * **Hatalı İletiler (DLQ)**: Bozuk veriler veya 3 deneme sonunda da başarısız olan kayıtlar AWS SQS kuyruğuna (Dead Letter Queue) park edilir.

### 📝 Tüketici Payload Şeması (`TelemetryEventPayload`)
```json
{
  "TenantId": "9f8e7d6c-5b4a-3f2e-1d0c-9b8a7f6e5d4c",
  "DeviceId": "b0a9c8d7-e6f5-4d3c-2b1a-0f9e8d7c6b5a",
  "TelemetryKey": "temperature",
  "TelemetryValue": 6.5,
  "EventId": "evt_1a2b3c4d5e"
}
```

---

## ⚡ 3. İş Akışı Tetikleme (Workflow Engine)

Tüketilen telemetri olayları, tanımlanmış iş akışı kurallarına göre değerlendirilerek DynamoDB üzerinde otonom görevler oluşturur.

* **İlgili Sınıf**: [ProcessTelemetryEventCommandHandler](file:///var/home/Chronic_Panda/RiderProjects/omnipulse/src/Modules/WorkflowModule/Features/Workflows/ProcessTelemetryEvent/ProcessTelemetryEventCommandHandler.cs)

### ⚙️ İş Akışı Senaryosu: Sıcaklık Sınırı Aşımı
1. Cihaz soğutuculu tırın içindeki sıcaklığı **6.5°C** olarak gönderir.
2. Sistemde tanımlı olan iş akışı kuralı (`TriggerCondition`):
   ```json
   {
     "TelemetryKey": "temperature",
     "Operator": ">",
     "StaticThreshold": 5.0
   }
   ```
3. `ProcessTelemetryEventCommandHandler` bu kuralı değerlendirir. Sıcaklık 5.0°C sınırını aştığı için kural **tetiklenir**.
4. **Idempotency Kontrolü**: `EventId` üzerinden bu olayın daha önce işlenip işlenmediği sorgulanır.
5. **Spam Koruması**: Bu varlık üzerinde zaten aktif bir "Sıcaklık Kontrolü" görevi varsa yeni bir görev açılmaz.
6. **Zimmet/Atama Politikası**: Cihazın bağlı olduğu varlığın (`Asset`) sorumlu kullanıcısı veya şefi belirlenir.
7. **DynamoDB Görev Kaydı**: DynamoDB'ye `Status = "Pending"` durumunda yeni bir `WorkflowTask` eklenir.
8. **Bildirim**: Atanan sorumluya anlık push/mail bildirimi gönderilir.

---

## 🚀 4. Başka Nelerde Kullanabiliriz? (Gelecek Senaryoları)

AWS Kinesis'in yüksek throughput ve sıralı veri akışı avantajını kullanarak projemizi şu senaryolarla zenginleştirebiliriz:

### 1. Canlı İzleme ve Gerçek Zamanlı Dashboard'lar (Real-time Dashboards)
* **Senaryo**: Kullanıcı arayüzünde (omnipulse-ui) tırların veya fabrikadaki makinelerin durumlarının gecikmesiz canlı grafiklerle izlenmesi.
* **Nasıl Uygulanır**: Kinesis'ten okuma yapan hafif bir SignalR/Websocket Gateway servisi yazılır. Gelen veriler anında UI'a basılır, böylece DB'ye yük bindirmeden canlı takip sağlanır.

### 2. Anomali Tespiti ve Yapay Zeka (AI/ML Streaming Analytics)
* **Senaryo**: Sıcaklık, basınç ve titreşim verilerinin anlık kombinasyonunu analiz ederek makinenin bozulacağını 2 saat önceden tahmin etme (Kestirimci Bakım).
* **Nasıl Uygulanır**: Kinesis Stream, AWS SageMaker or AWS Lambda üzerinden bir Python ML modeline beslenir. Modelden dönen anomali skoru kritik seviyeye ulaştığında yine bir Kinesis event'iyle iş akışı motoru tetiklenebilir.

### 3. Veri Ambarı ve Büyük Veri Analizi (Data Lakehouse)
* **Senaryo**: Son 1 yıla ait tüm telemetri geçmişi üzerinde trend analizi ve raporlama yapılması.
* **Nasıl Uygulanır**: **AWS Kinesis Data Firehose** entegrasyonu kurularak akan veriler hiç kod yazmadan otomatik olarak sıkıştırılmış (Parquet) formatta Amazon S3'e veya Snowflake / Google BigQuery'ye yazılır.

### 4. Audit Log ve Cihaz Güvenliği Takibi
* **Senaryo**: Cihazların bağlantı sıklıkları, geçersiz veri gönderme sıklıkları ve yetkisiz erişim denemelerinin güvenlik analizi.
* **Nasıl Uygulanır**: Cihazların bağlantı/bağlantı kesme durumları da Kinesis'e bir event olarak basılır. Güvenlik analizörü (SIEM) bu akışı dinleyerek anormal cihaz hareketlerinde güvenlik alarmları tetikler.
