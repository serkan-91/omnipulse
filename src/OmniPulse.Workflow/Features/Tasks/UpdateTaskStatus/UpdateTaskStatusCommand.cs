using System;
using MediatR;

namespace OmniPulse.Workflow.Features.Tasks.UpdateTaskStatus;

/// <summary>
/// Bir iş kartının durumunu güncellemek için komut.
/// Geçerli geçişler: Pending → InProgress → Completed, herhangi biri → Cancelled.
/// </summary>
public record UpdateTaskStatusCommand(
    Guid TaskId,
    string NewStatus
) : IRequest<bool>;
