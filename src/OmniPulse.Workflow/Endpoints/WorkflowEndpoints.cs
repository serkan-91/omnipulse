using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using OmniPulse.Workflow.Features.Definitions.CreateDefinition;
using OmniPulse.Workflow.Features.Policies.CreatePolicy;
using OmniPulse.Workflow.Features.Tasks.GetTasks;
using OmniPulse.Workflow.Features.Tasks.UpdateTaskStatus;
using OmniPulse.Workflow.Features.Workflows.RunDemo;

namespace OmniPulse.Workflow;

public static class WorkflowEndpoints
{
    public static IEndpointRouteBuilder MapWorkflowEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapCreateDefinitionEndpoint();
        app.MapCreatePolicyEndpoint();
        app.MapGetTasksEndpoint();
        app.MapRunDemoEndpoint();

        // Task Lifecycle: Accept / Complete / Cancel
        app.MapUpdateTaskStatusEndpoints();

        return app;
    }
}
