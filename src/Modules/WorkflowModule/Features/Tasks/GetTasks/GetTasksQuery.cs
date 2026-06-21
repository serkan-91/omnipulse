using System;
using System.Collections.Generic;
using MediatR;
using OmniPulse.Modules.WorkflowModule.Domain.Entities;

namespace OmniPulse.Modules.WorkflowModule.Features.Tasks.GetTasks;

public record GetTasksQuery(string? Status = null) : IRequest<IEnumerable<WorkflowTask>>;
