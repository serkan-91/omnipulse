using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.Modules.WorkflowModule.Infrastructure.Services;

namespace OmniPulse.Modules.WorkflowModule.Features.Tasks.UpdateTaskStatus;

public class UpdateTaskStatusCommandHandler(
    IWorkflowTaskStore taskStore,
    IUserTenantContext userTenantContext,
    ILogger<UpdateTaskStatusCommandHandler> logger)
    : IRequestHandler<UpdateTaskStatusCommand, bool>
{
    private static readonly string[] ValidStatuses = ["InProgress", "Completed", "Cancelled"];

    public async Task<bool> Handle(UpdateTaskStatusCommand request, CancellationToken cancellationToken)
    {
        if (!userTenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Aktif bir kiracı bağlamı bulunamadı! 🔐");

        if (!ValidStatuses.Contains(request.NewStatus))
            throw new ArgumentException(
                $"Geçersiz durum: '{request.NewStatus}'. Geçerli değerler: {string.Join(", ", ValidStatuses)}");

        var tenantId = userTenantContext.TenantId.Value;
        var task = await taskStore.GetTaskAsync(tenantId, request.TaskId, cancellationToken);

        if (task is null)
        {
            logger.LogWarning("⚠️ [TaskLifecycle] Görev bulunamadı: {TaskId}", request.TaskId);
            return false;
        }

        var isValidTransition = (task.Status, request.NewStatus) switch
        {
            ("Pending",    "InProgress")  => true,
            ("Pending",    "Cancelled")   => true,
            ("InProgress", "Completed")   => true,
            ("InProgress", "Cancelled")   => true,
            _ => false
        };

        if (!isValidTransition)
        {
            logger.LogWarning(
                "⚠️ [TaskLifecycle] Geçersiz geçiş: {From} → {To} (TaskId: {TaskId})",
                task.Status, request.NewStatus, request.TaskId);
            throw new InvalidOperationException(
                $"'{task.Status}' durumundan '{request.NewStatus}' durumuna geçiş yapılamaz.");
        }

        await taskStore.UpdateTaskStatusAsync(task, request.NewStatus, cancellationToken);

        logger.LogInformation(
            "✅ [TaskLifecycle] {TaskId} | {Old} → {New}",
            request.TaskId, task.Status, request.NewStatus);

        return true;
    }
}
