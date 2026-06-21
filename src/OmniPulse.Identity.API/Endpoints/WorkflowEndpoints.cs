using OmniPulse.Modules.WorkflowModule.Features.Definitions.CreateDefinition;
using OmniPulse.Modules.WorkflowModule.Features.Policies.CreatePolicy;
using OmniPulse.Modules.WorkflowModule.Features.Tasks.GetTasks;
using OmniPulse.Modules.WorkflowModule.Features.Tasks.UpdateTaskStatus;
using OmniPulse.Modules.WorkflowModule.Features.Workflows.RunDemo;

namespace OmniPulse.Identity.API.Endpoints;

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

