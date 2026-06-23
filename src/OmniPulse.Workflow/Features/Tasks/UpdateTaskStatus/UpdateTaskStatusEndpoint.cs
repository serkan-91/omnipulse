using System;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.Workflow.Features.Tasks.UpdateTaskStatus;

public static class UpdateTaskStatusEndpoint
{
    public static IEndpointRouteBuilder MapUpdateTaskStatusEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPatch("api/workflows/tasks/{taskId:guid}/accept",
            async (Guid taskId, ISender mediator) =>
            {
                var command = new UpdateTaskStatusCommand(taskId, "InProgress");
                var result = await mediator.Send(command);
                return result
                    ? Results.Ok(new { Message = "Görev kabul edildi ve devam ediyor. ✅" })
                    : Results.NotFound();
            })
            .WithName("AcceptTask")
            .WithSummary("Görevi kabul et (Pending → InProgress)")
            .WithTags("Workflow Tasks")
            .RequireAuthorization();

        app.MapPatch("api/workflows/tasks/{taskId:guid}/complete",
            async (Guid taskId, ISender mediator) =>
            {
                var command = new UpdateTaskStatusCommand(taskId, "Completed");
                var result = await mediator.Send(command);
                return result
                    ? Results.Ok(new { Message = "Görev başarıyla tamamlandı. 🎉" })
                    : Results.NotFound();
            })
            .WithName("CompleteTask")
            .WithSummary("Görevi tamamla (InProgress → Completed)")
            .WithTags("Workflow Tasks")
            .RequireAuthorization();

        app.MapPatch("api/workflows/tasks/{taskId:guid}/cancel",
            async (Guid taskId, ISender mediator) =>
            {
                var command = new UpdateTaskStatusCommand(taskId, "Cancelled");
                var result = await mediator.Send(command);
                return result
                    ? Results.Ok(new { Message = "Görev iptal edildi." })
                    : Results.NotFound();
            })
            .WithName("CancelTask")
            .WithSummary("Görevi iptal et")
            .WithTags("Workflow Tasks")
            .RequireAuthorization();

        return app;
    }
}
