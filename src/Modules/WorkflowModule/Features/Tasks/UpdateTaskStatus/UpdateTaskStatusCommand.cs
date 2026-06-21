using System;
using MediatR;

namespace OmniPulse.Modules.WorkflowModule.Features.Tasks.UpdateTaskStatus;

/// <summary>
/// Bir iş kartının durumunu güncellemek için komut.
/// Geçerli geçişler: Pending → InProgress → Completed, herhangi biri → Cancelled.
/// </summary>
public record UpdateTaskStatusCommand(
    Guid TaskId,
    string NewStatus
) : IRequest<bool>;
