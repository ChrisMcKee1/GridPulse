using System.Collections.Generic;
using System.Linq;
using GridPulse.Application.Abstractions;
using GridPulse.Application.Exceptions;
using GridPulse.Application.Models;
using GridPulse.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;

namespace GridPulse.WebApi.Endpoints;

internal static class TicketsEndpointGroup
{
    public static RouteGroupBuilder MapTicketsEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/tickets")
            .WithTags("Tickets");

        group.MapPost(string.Empty, CreateTicketAsync)
            .WithName("CreateTicket");

        group.MapGet(string.Empty, GetTicketsAsync)
            .WithName("GetTickets");

        group.MapPatch("/{ticketId:guid}/status", UpdateTicketStatusAsync)
            .WithName("UpdateTicketStatus");

        return group;
    }

    private static async Task<IResult> CreateTicketAsync(
        TicketCreateRequest request,
        ITicketAutomationService ticketAutomationService,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return ValidationProblem("Ticket payload is required.");
        }

        try
        {
            var ticket = await ticketAutomationService
                .CreateManualTicketAsync(request, cancellationToken)
                .ConfigureAwait(false);

            return Results.Created($"/api/tickets/{ticket.Id}", ticket);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return ConflictProblem(ex.Message);
        }
    }

    private static async Task<IResult> GetTicketsAsync(
        HttpRequest request,
        ITicketQueryService ticketQueryService,
        CancellationToken cancellationToken)
    {
        if (!TryBuildFilter(request, out var filter, out var errorResult))
        {
            return errorResult!;
        }

        var tickets = await ticketQueryService
            .GetTicketsAsync(filter, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(tickets);
    }

    private static async Task<IResult> UpdateTicketStatusAsync(
        Guid ticketId,
        TicketStatusUpdateRequest request,
        ITicketWriteService ticketWriteService,
        ITicketQueryService ticketQueryService,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return ValidationProblem("Ticket status payload is required.");
        }

        try
        {
            await ticketWriteService
                .UpdateStatusAsync(ticketId, request, cancellationToken)
                .ConfigureAwait(false);

            var dto = await ticketQueryService
                .GetByIdAsync(ticketId, cancellationToken)
                .ConfigureAwait(false);

            return dto is null
                ? NotFoundProblem($"Ticket {ticketId} was not found.")
                : Results.Ok(dto);
        }
        catch (TicketWorkflowException ex)
        {
            return ConflictProblem(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return NotFoundProblem(ex.Message);
        }
    }

    private static bool TryBuildFilter(
        HttpRequest request,
        out TicketFilter filter,
        out IResult? errorResult)
    {
        errorResult = null;
        filter = new TicketFilter();

        var statusesValue = request.Query["statuses"];
        if (!TryParseStatuses(statusesValue, out var statuses, out var parseError))
        {
            errorResult = ValidationProblem(parseError!);
            return false;
        }

        var minPriority = default(TicketPriority?);
        var minPriorityValue = request.Query["minPriority"];
        if (!StringValues.IsNullOrEmpty(minPriorityValue))
        {
            var raw = minPriorityValue[^1];
            if (!Enum.TryParse(raw, true, out TicketPriority parsedPriority))
            {
                errorResult = ValidationProblem($"Unknown ticket priority '{raw}'.");
                return false;
            }

            minPriority = parsedPriority;
        }

        var includeRecommendations = ParseBoolean(request.Query["includeRecommendations"]);
        var includeTimeline = ParseBoolean(request.Query["includeTimeline"]);
        var normalizedStatuses = statuses?.ToArray();

        filter = new TicketFilter
        {
            Statuses = normalizedStatuses,
            MinPriority = minPriority,
            IncludeRecommendations = includeRecommendations,
            IncludeTimeline = includeTimeline
        };

        return true;
    }

    private static bool TryParseStatuses(
        StringValues rawStatuses,
        out IReadOnlyCollection<TicketStatus>? statuses,
        out string? error)
    {
        error = null;
        statuses = null;

        if (StringValues.IsNullOrEmpty(rawStatuses))
        {
            return true;
        }

        var parsed = new List<TicketStatus>();

        foreach (var rawValue in rawStatuses)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                continue;
            }

            var parts = rawValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                if (!Enum.TryParse(part, true, out TicketStatus status))
                {
                    error = $"Unknown ticket status '{part}'.";
                    return false;
                }

                parsed.Add(status);
            }
        }

        statuses = parsed.Count == 0 ? null : parsed;
        return true;
    }

    private static bool ParseBoolean(StringValues values)
    {
        if (StringValues.IsNullOrEmpty(values))
        {
            return false;
        }

        var raw = values[^1];
        return bool.TryParse(raw, out var parsed) && parsed;
    }

    private static IResult ValidationProblem(string detail) =>
        Results.Problem(new ProblemDetails
        {
            Title = "Invalid ticket request",
            Detail = detail,
            Status = StatusCodes.Status400BadRequest
        });

    private static IResult ConflictProblem(string detail) =>
        Results.Problem(new ProblemDetails
        {
            Title = "Ticket conflict",
            Detail = detail,
            Status = StatusCodes.Status409Conflict
        });

    private static IResult NotFoundProblem(string detail) =>
        Results.Problem(new ProblemDetails
        {
            Title = "Ticket not found",
            Detail = detail,
            Status = StatusCodes.Status404NotFound
        });

}
