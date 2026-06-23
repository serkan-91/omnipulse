using System;
using System.Collections.Generic;
using MediatR;
using OmniPulse.Workflow.Domain.Entities;

namespace OmniPulse.Workflow.Features.Tasks.GetTasks;

public record GetTasksQuery(string? Status = null) : IRequest<IEnumerable<WorkflowTask>>;
