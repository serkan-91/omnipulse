using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OmniPulse.IoT.Infrastructure.Persistence;

namespace OmniPulse.IoT.Infrastructure.Services;

/// <summary>
/// PostgreSQL Telemetri tablosu için aylık partition'ları yöneten servis. 🗑️📅
/// Gelecek ayın partition'ını otomatik hazırlar ve 6 aydan eski partition'ları otomatik temizler (DROP PARTITION).
/// </summary>
public class PostgresPartitionManager : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PostgresPartitionManager> _logger;

    public PostgresPartitionManager(
        IServiceProvider serviceProvider,
        ILogger<PostgresPartitionManager> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PostgreSQL Partition Manager Background Service is starting.");

        // Günde bir kere çalışması yeterlidir
        using var timer = new PeriodicTimer(TimeSpan.FromDays(1));

        // İlk tetiklemeyi hemen yapalım
        await ManagePartitionsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ManagePartitionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while managing PostgreSQL telemetry partitions.");
            }
        }

        _logger.LogInformation("PostgreSQL Partition Manager Background Service is stopping.");
    }

    private async Task ManagePartitionsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IoTDbContext>();

        var now = DateTime.UtcNow;

        // 1. Gelecek ayın partition'ını hazırla (Örn: Bu ay Haziran ise Temmuz'u oluştur)
        var nextMonthDate = now.AddMonths(1);
        await CreatePartitionAsync(dbContext, nextMonthDate.Year, nextMonthDate.Month, cancellationToken);

        // 2. Bu ayın partition'ının da her ihtimale karşı var olduğundan emin ol
        await CreatePartitionAsync(dbContext, now.Year, now.Month, cancellationToken);

        // 3. 6 aydan eski partition'ları drop et (Aylık temizlik)
        // Örn: Son 6-12 ay arasındaki tabloları kontrol edip drop edelim
        for (int i = 6; i <= 12; i++)
        {
            var oldDate = now.AddMonths(-i);
            var partitionName = $"Telemetry_y{oldDate.Year}m{oldDate.Month:D2}";

            try
            {
                // PostgreSQL'de partition'ı güvenle DROP etmek için
                // DROP TABLE IF EXISTS "partition_name" çalıştırırız.
#pragma warning disable EF1002
                await dbContext.Database.ExecuteSqlRawAsync(
                    $"DROP TABLE IF EXISTS \"{partitionName}\";",
                    cancellationToken
                );
#pragma warning restore EF1002
                _logger.LogInformation("🧹 Eski telemetri partition'ı başarıyla temizlendi: {PartitionName}", partitionName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to drop old partition: {PartitionName}", partitionName);
            }
        }
    }

    private async Task CreatePartitionAsync(IoTDbContext dbContext, int year, int month, CancellationToken cancellationToken)
    {
        var partitionName = $"Telemetry_y{year}m{month:D2}";
        var startDate = $"{year}-{month:D2}-01 00:00:00Z";
        
        var nextYear = month == 12 ? year + 1 : year;
        var nextMonth = month == 12 ? 1 : month + 1;
        var endDate = $"{nextYear}-{nextMonth:D2}-01 00:00:00Z";

        try
        {
            var sql = $@"
                CREATE TABLE IF NOT EXISTS ""{partitionName}"" PARTITION OF ""Telemetry""
                FOR VALUES FROM ('{startDate}') TO ('{endDate}');
            ";

#pragma warning disable EF1002
            await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
#pragma warning restore EF1002
            _logger.LogInformation("📅 Telemetri partition'ı hazırlandı/doğrulandı: {PartitionName}", partitionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create/ensure partition: {PartitionName}", partitionName);
        }
    }
}
