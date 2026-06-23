using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OmniPulse.IoT.Infrastructure.Persistence;
using OmniPulse.IoT.Infrastructure.Streaming;

namespace OmniPulse.IoT.Infrastructure.Services;

/// <summary>
/// Transactional Outbox tablosunu tarayarak biriken Kinesis olaylarını 
/// güvenli ve sıralı şekilde Kinesis Stream'ine pompalayan arka plan işçisi. 📦⚡
/// </summary>
public class OutboxEventProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxEventProcessor> _logger;
    private const int BatchSize = 100;
    private const int MaxAttempts = 5;

    public OutboxEventProcessor(
        IServiceProvider serviceProvider,
        ILogger<OutboxEventProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Event Processor Background Service is starting.");

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessOutboxEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing outbox events.");
            }
        }

        _logger.LogInformation("Outbox Event Processor Background Service is stopping.");
    }

    private async Task ProcessOutboxEventsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IoTDbContext>();
        var kinesisPublisher = scope.ServiceProvider.GetRequiredService<IKinesisTelemetryPublisher>();

        // Ignore query filters to process events from all tenants seamlessly
        var events = await dbContext.OutboxEvents
            .IgnoreQueryFilters()
            .OrderBy(o => o.CreatedAtUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (events.Count == 0)
        {
            return;
        }

        _logger.LogDebug("Found {Count} outbox events to process.", events.Count);

        foreach (var outboxEvent in events)
        {
            try
            {
                // Publish directly to Kinesis using raw JSON payload
                await kinesisPublisher.PublishRawAsync(
                    outboxEvent.PartitionKey,
                    outboxEvent.Payload,
                    cancellationToken
                );

                // Delete successfully sent event to prevent table growth
                dbContext.OutboxEvents.Remove(outboxEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox event {Id} to Kinesis. Attempt: {Attempt}", 
                    outboxEvent.Id, outboxEvent.AttemptCount + 1);

                outboxEvent.IncrementAttempt(ex.Message);

                if (outboxEvent.AttemptCount >= MaxAttempts)
                {
                    _logger.LogCritical("Outbox event {Id} has failed {Attempts} times. Moving or keeping for manual intervention.", 
                        outboxEvent.Id, outboxEvent.AttemptCount);
                    
                    // We can either delete it to unblock pipeline and move to log, or keep it in DB.
                    // Let's delete it so it doesn't block other events indefinitely, but log critical details.
                    dbContext.OutboxEvents.Remove(outboxEvent);
                }
                else
                {
                    dbContext.OutboxEvents.Update(outboxEvent);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
