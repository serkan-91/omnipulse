using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.Modules.WorkflowModule.Features.Workflows.RunDemo;

public static class RunDemoEndpoint
{
    public static IEndpointRouteBuilder MapRunDemoEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/workflows/demo", async (ISender mediator) =>
            {
                var command = new RunDemoCommand();
                var response = await mediator.Send(command);
                return Results.Ok(response);
            })
            .WithName("RunWorkflowDemo")
            .WithTags("Workflow Demo")
            .AllowAnonymous(); // Anonim erişime izin verelim kolay test için! 🔓

        return app;
    }
}
