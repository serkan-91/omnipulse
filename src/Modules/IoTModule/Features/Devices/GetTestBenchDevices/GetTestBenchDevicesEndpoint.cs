using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.Modules.IoTModule.Features.Devices.GetTestBenchDevices;

public static class GetTestBenchDevicesEndpoint
{
    public static IEndpointRouteBuilder MapGetTestBenchDevicesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("api/iot/devices/test-bench", async (ISender mediator) =>
            {
                var response = await mediator.Send(new GetTestBenchDevicesQuery());
                return Results.Ok(response);
            })
            .WithName("GetTestBenchDevices")
            .WithTags("IoT Devices")
            .RequireAuthorization(); // JWT Authentication required 🔐

        return app;
    }
}
