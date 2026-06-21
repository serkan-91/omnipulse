using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.Modules.WorkflowModule.Features.Tasks.GetTasks;

public static class GetTasksEndpoint
{
    public static IEndpointRouteBuilder MapGetTasksEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("api/workflows/tasks", async (ISender mediator) =>
            {
                var query = new GetTasksQuery();
                var response = await mediator.Send(query);
                return Results.Ok(response);
            })
            .WithName("GetWorkflowTasks")
            .WithTags("Workflow Tasks")
            .RequireAuthorization();

        return app;
    }
}
