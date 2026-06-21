using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.Modules.WorkflowModule.Domain.Entities;
using OmniPulse.Modules.WorkflowModule.Infrastructure.Services;

namespace OmniPulse.Modules.WorkflowModule.Features.Tasks.GetTasks;

public class GetTasksQueryHandler(
    IWorkflowTaskStore taskStore,
    IUserTenantContext userTenantContext)
    : IRequestHandler<GetTasksQuery, IEnumerable<WorkflowTask>>
{
    public async Task<IEnumerable<WorkflowTask>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
    {
        if (!userTenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Aktif bir kiracı bağlamı bulunamadı şefim! 🔐");
        }

        var tenantId = userTenantContext.TenantId.Value;
        return await taskStore.GetTasksByTenantAsync(tenantId, cancellationToken);
    }
}
