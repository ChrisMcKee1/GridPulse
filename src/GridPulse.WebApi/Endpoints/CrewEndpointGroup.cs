using GridPulse.Application.Abstractions;
using GridPulse.Application.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GridPulse.WebApi.Endpoints;

internal static class CrewEndpointGroup
{
    public static RouteGroupBuilder MapCrewEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/crews")
            .WithTags("Crew");

        group.MapPost("/{crewId:guid}/status", PostCrewStatusAsync)
            .WithName("PostCrewStatus")
            .Produces<CrewStatusUpdateResponse>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> PostCrewStatusAsync(
        Guid crewId,
        CrewStatusUpdateRequest request,
        IDispatchAssignmentService dispatchAssignmentService,
        CancellationToken cancellationToken)
    {
        if (crewId == Guid.Empty)
        {
            return ValidationProblem("Crew identifier is required.");
        }

        if (request is null)
        {
            return ValidationProblem("Crew status payload is required.");
        }

        try
        {
            var response = await dispatchAssignmentService
                .ProcessCrewStatusAsync(crewId, request, cancellationToken)
                .ConfigureAwait(false);

            return Results.Accepted($"/api/tickets/{response.TicketId}", response);
        }
        catch (InvalidOperationException ex)
        {
            return NotFoundProblem(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    private static IResult ValidationProblem(string detail) =>
        Results.Problem(new ProblemDetails
        {
            Title = "Invalid crew status request",
            Detail = detail,
            Status = StatusCodes.Status400BadRequest
        });

    private static IResult NotFoundProblem(string detail) =>
        Results.Problem(new ProblemDetails
        {
            Title = "Crew assignment not found",
            Detail = detail,
            Status = StatusCodes.Status404NotFound
        });
}
