using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.Modules.IoTModule.Infrastructure.Persistence;

namespace OmniPulse.Modules.IoTModule.Features.Telemetry.GetTelemetryReport;

/// <summary>
/// Müşteri Hizmetleri (Canan Hanım) için soğuk zincir kırılma ve geçmiş analiz
/// raporunu yüksek performanslı Dapper (CQRS Read Optimization) ile hazırlayan işleyici! 📊⚡
/// </summary>
public class GetTelemetryReportQueryHandler(
    IoTDbContext dbContext,
    IUserTenantContext userTenantContext)
    : IRequestHandler<GetTelemetryReportQuery, TelemetryReportDto>
{
    public async Task<TelemetryReportDto> Handle(GetTelemetryReportQuery request, CancellationToken cancellationToken)
    {
        // 1. Kiracı bağlamını alalım
        if (!userTenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Aktif bir kiracı bağlamı bulunamadı şefim! 🔐");
        }

        var tenantId = userTenantContext.TenantId.Value;
        var isTemp = request.MetricKey.Equals("Temperature", StringComparison.OrdinalIgnoreCase);
        var metricCol = isTemp ? "Temperature" : "Pressure";
        var threshold = request.ColdChainThreshold;

        // 2. Dinamik Dapper Parametreleri ve WHERE Şartları
        var parameters = new DynamicParameters();
        parameters.Add("TenantId", tenantId);
        parameters.Add("StartDate", request.StartDate);
        parameters.Add("EndDate", request.EndDate);
        parameters.Add("Threshold", threshold);

        var conditions = new List<string>();

        if (request.DeviceId.HasValue)
        {
            conditions.Add("t.\"DeviceId\" = @DeviceId");
            parameters.Add("DeviceId", request.DeviceId.Value);
        }

        if (request.AssetId.HasValue)
        {
            conditions.Add("d.\"AssetId\" = @AssetId");
            parameters.Add("AssetId", request.AssetId.Value);
        }

        // 3. Saha Kullanıcısı (Driver/FieldWorker/Operator) Satır Bazlı Filtreleme (ABAC) 🏭
        var isFieldUser = 
            userTenantContext.Roles.Contains("Driver", StringComparer.OrdinalIgnoreCase) || 
            userTenantContext.Roles.Contains("Sürücü", StringComparer.OrdinalIgnoreCase) ||
            userTenantContext.Roles.Contains("FieldWorker", StringComparer.OrdinalIgnoreCase) ||
            userTenantContext.Roles.Contains("Operator", StringComparer.OrdinalIgnoreCase);

        if (isFieldUser)
        {
            if (Guid.TryParse(userTenantContext.UserId, out var responsibleUserId))
            {
                conditions.Add(@"a.""Id"" IN (
                    WITH RECURSIVE AllowedAssets AS (
                        SELECT ap.""AssetId"" AS ""Id""
                        FROM ""AssetPermissions"" ap
                        WHERE ap.""UserId"" = @ResponsibleUserId AND ap.""IsDeleted"" = false
                        
                        UNION
                        
                        SELECT ass.""Id""
                        FROM ""Assets"" ass
                        WHERE ass.""ResponsibleUserId"" = @ResponsibleUserId AND ass.""IsDeleted"" = false
                        
                        UNION
                        
                        SELECT child.""Id""
                        FROM ""Assets"" child
                        INNER JOIN AllowedAssets parent ON child.""ParentAssetId"" = parent.""Id""
                        WHERE child.""IsDeleted"" = false
                    )
                    SELECT ""Id"" FROM AllowedAssets
                )");
                parameters.Add("ResponsibleUserId", responsibleUserId);
            }
            else
            {
                // Kimlik çözülemediyse güvenli olarak veri döndürme
                return new TelemetryReportDto(
                    MinValue: 0, MaxValue: 0, AverageValue: 0, TotalCount: 0, BreachCount: 0, BreachPercentage: 0,
                    BreachedPoints: new List<BreachPointDto>(),
                    Message: "Kullanıcı kimliği doğrulanamadı!"
                );
            }
        }

        var whereClause = string.Join(" AND ", conditions.Select(c => $"({c})"));
        var finalWhere = string.IsNullOrEmpty(whereClause) ? "" : $" AND {whereClause}";

        // 4. EF Core üzerinden ADO.NET Connection'ı alalım (Dapper ile paylaşımlı) 🔌
        using var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        // 5. Aggregate özet sorgusunu Dapper ile çalıştır
        var summarySql = $@"
            SELECT 
                COALESCE(MIN(t.""{metricCol}""), 0) as MinValue,
                COALESCE(MAX(t.""{metricCol}""), 0) as MaxValue,
                COALESCE(AVG(t.""{metricCol}""), 0) as AverageValue,
                COUNT(t.""Id"") as TotalCount
            FROM ""Telemetry"" t
            INNER JOIN ""Devices"" d ON t.""DeviceId"" = d.""Id"" AND d.""IsDeleted"" = false
            LEFT JOIN ""Assets"" a ON d.""AssetId"" = a.""Id"" AND a.""IsDeleted"" = false
            WHERE t.""TenantId"" = @TenantId
              AND t.""Timestamp"" >= @StartDate
              AND t.""Timestamp"" <= @EndDate
              {finalWhere}
        ";

        var summary = await connection.QuerySingleAsync<TelemetrySummaryDto>(summarySql, parameters);

        if (summary.TotalCount == 0)
        {
            return new TelemetryReportDto(
                MinValue: 0, MaxValue: 0, AverageValue: 0, TotalCount: 0, BreachCount: 0, BreachPercentage: 0,
                BreachedPoints: new List<BreachPointDto>(),
                Message: $"Belirtilen zaman aralığında ({request.StartDate:dd/MM/yyyy HH:mm} - {request.EndDate:dd/MM/yyyy HH:mm}) herhangi bir telemetri verisi bulunamadı."
            );
        }

        // 6. Eşik ihlallerini (Breached Points) Dapper ile çek
        var breachedSql = $@"
            SELECT 
                t.""Id"",
                t.""DeviceId"",
                d.""Name"" as DeviceName,
                a.""Name"" as AssetName,
                t.""{metricCol}"" as Value,
                @Threshold as Threshold,
                t.""Timestamp""
            FROM ""Telemetry"" t
            INNER JOIN ""Devices"" d ON t.""DeviceId"" = d.""Id"" AND d.""IsDeleted"" = false
            LEFT JOIN ""Assets"" a ON d.""AssetId"" = a.""Id"" AND a.""IsDeleted"" = false
            WHERE t.""TenantId"" = @TenantId
              AND t.""Timestamp"" >= @StartDate
              AND t.""Timestamp"" <= @EndDate
              AND t.""{metricCol}"" > @Threshold
              {finalWhere}
            ORDER BY t.""Timestamp"" DESC
        ";

        var breachedPoints = (await connection.QueryAsync<BreachPointDto>(breachedSql, parameters)).ToList();

        var breachPercentage = (double)breachedPoints.Count / summary.TotalCount * 100.0;
        
        var msg = breachedPoints.Count > 0
            ? $"ANALİZ TAMAMLANDI: Belirtilen zaman aralığında toplam {summary.TotalCount} okumadan {breachedPoints.Count} adedinde soğuk zincir kırılması (eşik ihlali) tespit edilmiştir! ⚠️"
            : "ANALİZ TAMAMLANDI: Harika! Soğuk zincirde herhangi bir kırılma tespit edilmemiştir. ✅";

        return new TelemetryReportDto(
            MinValue: Math.Round(summary.MinValue, 2),
            MaxValue: Math.Round(summary.MaxValue, 2),
            AverageValue: Math.Round(summary.AverageValue, 2),
            TotalCount: summary.TotalCount,
            BreachCount: breachedPoints.Count,
            BreachPercentage: Math.Round(breachPercentage, 2),
            BreachedPoints: breachedPoints,
            Message: msg
        );
    }

    private class TelemetrySummaryDto
    {
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public double AverageValue { get; set; }
        public int TotalCount { get; set; }
    }
}
