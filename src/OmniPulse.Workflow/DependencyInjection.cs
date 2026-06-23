using System.Reflection;
using Amazon.DynamoDBv2;
using Amazon.Kinesis;
using Amazon.SQS;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OmniPulse.BuildingBlocks.Configuration;
using OmniPulse.Workflow.Infrastructure.Persistence;
using OmniPulse.Workflow.Infrastructure.Services;

namespace OmniPulse.Workflow;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkflow(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. PostgreSQL DbContext Kaydı (Ortak DatabaseOptions kullanır)
        services.AddDbContext<WorkflowDbContext>((sp, options) =>
        {
            var dbOpts = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;

            options.UseNpgsql(dbOpts.DefaultConnection,
                b => b.MigrationsAssembly("OmniPulse.Identity.API"));
        });

        // 2. AWS Servisleri Kaydı (LocalStack veya Canlı AWS)
        var kinesisSection = configuration.GetSection("AWS:Kinesis");
        var useLocalStack = kinesisSection.GetValue<bool>("UseLocalStack");
        var regionName = kinesisSection.GetValue<string>("Region") ?? "us-east-1";
        var regionEndpoint = Amazon.RegionEndpoint.GetBySystemName(regionName);

        services.AddSingleton<IAmazonDynamoDB>(sp =>
        {
            try
            {
                if (useLocalStack)
                {
                    return new AmazonDynamoDBClient(new AmazonDynamoDBConfig
                    {
                        ServiceURL = "http://localhost:4566",
                        AuthenticationRegion = regionEndpoint.SystemName
                    });
                }
                return new AmazonDynamoDBClient(regionEndpoint);
            }
            catch
            {
                return null!;
            }
        });

        services.AddSingleton<IAmazonKinesis>(sp =>
        {
            try
            {
                if (useLocalStack)
                {
                    return new AmazonKinesisClient(new AmazonKinesisConfig
                    {
                        ServiceURL = "http://localhost:4566",
                        AuthenticationRegion = regionEndpoint.SystemName
                    });
                }
                return new AmazonKinesisClient(regionEndpoint);
            }
            catch
            {
                return null!;
            }
        });

        services.AddSingleton<IAmazonSQS>(sp =>
        {
            try
            {
                if (useLocalStack)
                {
                    return new AmazonSQSClient(new AmazonSQSConfig
                    {
                        ServiceURL = "http://localhost:4566",
                        AuthenticationRegion = regionEndpoint.SystemName
                    });
                }
                return new AmazonSQSClient(regionEndpoint);
            }
            catch
            {
                return null!;
            }
        });

        // 3. Modül Servislerinin Kaydı
        services.AddSingleton<IWorkflowTaskStore, DynamoDbTaskStore>();
        services.AddScoped<IAssignmentPolicyLoader, AssignmentPolicyLoader>();
        services.AddScoped<INotificationService, InMemoryNotificationService>();
        services.AddHostedService<KinesisTelemetryConsumer>();

        // 4. MediatR Kaydı
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        return services;
    }
}
