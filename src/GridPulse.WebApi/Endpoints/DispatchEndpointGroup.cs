using GridPulse.Application.Abstractions;
using GridPulse.Application.Exceptions;
using GridPulse.Application.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GridPulse.WebApi.Endpoints;

internal static class DispatchEndpointGroup
{
    public static RouteGroupBuilder MapDispatchEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/dispatch")
            .WithTags("Dispatch");

        group.MapGet("/recommendations", GetRecommendationsAsync)
            .WithName("GetDispatchRecommendations");

        group.MapPost("/assignments", PublishAssignmentAsync)
            .WithName("PublishAssignment");

        group.MapDelete("/crews/{crewId:guid}", DeleteCrewAsync)
            .WithName("DeleteCrew");

        group.MapPost("/crews/batch-delete", DeleteCrewsBatchAsync)
            .WithName("DeleteCrewsBatch");

        return group;
    }

    private static async Task<IResult> GetRecommendationsAsync(
        Guid? ticketId,
        IDispatchOptimizationService dispatchOptimizationService,
        CancellationToken cancellationToken)
    {
        if (ticketId is null || ticketId == Guid.Empty)
        {
            return ValidationProblem("Query parameter 'ticketId' is required.");
        }

        try
        {
            var envelope = await dispatchOptimizationService
                .GetRecommendationsAsync(ticketId.Value, cancellationToken)
                .ConfigureAwait(false);

            return Results.Ok(envelope);
        }
        catch (InvalidOperationException ex)
        {
            return NotFoundProblem(ex.Message);
        }
    }

    private static async Task<IResult> PublishAssignmentAsync(
        DispatchAssignmentRequest request,
        IDispatchAssignmentService assignmentService,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return ValidationProblem("Assignment payload is required.");
        }

        try
        {
            var receipt = await assignmentService
                .PublishAssignmentAsync(request, cancellationToken)
                .ConfigureAwait(false);

            return Results.Accepted($"/api/tickets/{request.TicketId}", receipt);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
        catch (DispatchWorkflowException ex)
        {
            return ConflictProblem(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return NotFoundProblem(ex.Message);
        }
    }

    private static async Task<IResult> DeleteCrewAsync(
        Guid crewId,
        IDispatchRepository dispatchRepository,
        CancellationToken cancellationToken)
    {
        try
        {
            await dispatchRepository.DeleteCrewAsync(crewId, cancellationToken).ConfigureAwait(false);
            await dispatchRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Results.NoContent();
        }
        catch (Exception ex)
        {
            return Results.Problem(new ProblemDetails
            {
                Title = "Failed to delete crew",
                Detail = ex.Message,
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    private static async Task<IResult> DeleteCrewsBatchAsync(
        [FromBody] CrewBatchDeleteRequest request,
        IDispatchRepository dispatchRepository,
        CancellationToken cancellationToken)
    {
        if (request?.CrewIds is null || !request.CrewIds.Any())
        {
            return ValidationProblem("At least one crew ID is required.");
        }

        try
        {
            await dispatchRepository.DeleteCrewsAsync(request.CrewIds, cancellationToken).ConfigureAwait(false);
            await dispatchRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Results.Ok(new { DeletedCount = request.CrewIds.Count() });
        }
        catch (Exception ex)
        {
            return Results.Problem(new ProblemDetails
            {
                Title = "Failed to delete crews",
                Detail = ex.Message,
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    private static IResult ValidationProblem(string detail) =>
        Results.Problem(new ProblemDetails
        {
            Title = "Invalid dispatch request",
            Detail = detail,
            Status = StatusCodes.Status400BadRequest
        });

    private static IResult ConflictProblem(string detail) =>
        Results.Problem(new ProblemDetails
        {
            Title = "Dispatch workflow conflict",
            Detail = detail,
            Status = StatusCodes.Status409Conflict
        });

    private static IResult NotFoundProblem(string detail) =>
        Results.Problem(new ProblemDetails
        {
            Title = "Dispatch resource missing",
            Detail = detail,
            Status = StatusCodes.Status404NotFound
        });
}
