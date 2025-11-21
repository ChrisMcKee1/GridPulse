using System;
using System.Collections.Generic;
using System.Globalization;
using GridPulse.Application.Abstractions;
using GridPulse.Application.Models;
using GridPulse.Domain.Entities;
using GridPulse.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GridPulse.Application.Services;

internal sealed class CrewAssignmentDeliveryService : ICrewAssignmentDeliveryService
{
    public CrewAssignmentDeliveryService(
        IAssignmentDeliveryRepository repository,
        ILogger<CrewAssignmentDeliveryService> logger,
        TimeProvider? timeProvider = null)
    {
        Repository = repository;
        Logger = logger;
        TimeProvider = timeProvider ?? TimeProvider.System;
    }

    private IAssignmentDeliveryRepository Repository { get; }
    private ILogger<CrewAssignmentDeliveryService> Logger { get; }
    private TimeProvider TimeProvider { get; }

    public async Task<AssignmentReceiptDto> QueueAssignmentAsync(AssignmentDeliveryContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var existing = await Repository.GetLatestAsync(context.TicketId, context.CrewId, cancellationToken).ConfigureAwait(false);
        var now = TimeProvider.GetUtcNow();

        AssignmentDelivery delivery;
        if (existing is null || IsTerminal(existing.Status))
        {
            delivery = BuildNewDelivery(context, now);
            await Repository.AddAsync(delivery, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            delivery = existing;
            delivery.AttemptCount += 1;
            delivery.Status = AssignmentDeliveryStatus.Queued;
            delivery.Payload = CreatePayload(context);
            delivery.UpdatedAt = now;
            delivery.LastError = null;
            await Repository.UpdateAsync(delivery, cancellationToken).ConfigureAwait(false);
        }

        await Repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        Logger.LogInformation(
            "Queued assignment {TrackingId} for ticket {TicketId} targeting crew {CrewId}.",
            delivery.TrackingId,
            delivery.TicketId,
            delivery.CrewId);

        return new AssignmentReceiptDto(
            delivery.TicketId,
            delivery.CrewId,
            delivery.Status.ToString().ToLowerInvariant(),
            delivery.TrackingId);
    }

    public async Task<CrewStatusUpdateResponse> RecordStatusAsync(Guid crewId, Guid ticketId, CrewStatusUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var delivery = await Repository.GetLatestAsync(ticketId, crewId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"No assignment delivery found for ticket {ticketId} and crew {crewId}.");

        var now = TimeProvider.GetUtcNow();
        var newStatus = MapStatus(request.Status);
        delivery.Status = newStatus;
        delivery.UpdatedAt = now;
        delivery.DeliveredAt = ShouldMarkDelivered(newStatus) ? now : delivery.DeliveredAt;
        ApplyStatusMetadata(delivery, request);

        await Repository.UpdateAsync(delivery, cancellationToken).ConfigureAwait(false);
        await Repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        Logger.LogInformation(
            "Crew {CrewId} reported status {Status} for ticket {TicketId} (tracking {TrackingId}).",
            crewId,
            request.Status,
            ticketId,
            delivery.TrackingId);

        return new CrewStatusUpdateResponse(ticketId, now);
    }

    private AssignmentDelivery BuildNewDelivery(AssignmentDeliveryContext context, DateTimeOffset now)
    {
        return new AssignmentDelivery
        {
            Id = Guid.NewGuid(),
            TicketId = context.TicketId,
            CrewId = context.CrewId,
            TrackingId = $"crew-{Guid.NewGuid():N}",
            Status = AssignmentDeliveryStatus.Queued,
            AttemptCount = 1,
            CreatedAt = now,
            UpdatedAt = now,
            Payload = CreatePayload(context)
        };
    }

    private static Dictionary<string, string> CreatePayload(AssignmentDeliveryContext context)
    {
        var payload = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ticketTitle"] = context.TicketTitle,
            ["priority"] = context.Priority.ToString(),
            ["crewName"] = context.CrewName,
            ["etaMinutes"] = context.EtaMinutes.ToString(CultureInfo.InvariantCulture),
            ["requiresOverride"] = context.RequiresOverrideJustification.ToString(CultureInfo.InvariantCulture),
            ["requestedBy"] = context.RequestedBy,
            ["createdAt"] = context.CreatedAt.ToString("O", CultureInfo.InvariantCulture)
        };

        if (context.AffectedAssets?.Count > 0)
        {
            payload["affectedAssets"] = string.Join(',', context.AffectedAssets);
        }

        if (!string.IsNullOrWhiteSpace(context.OverrideReason))
        {
            payload["overrideReason"] = context.OverrideReason!;
        }

        if (context.CrewLocation is { } location)
        {
            payload["crewLatitude"] = location.Latitude.ToString(CultureInfo.InvariantCulture);
            payload["crewLongitude"] = location.Longitude.ToString(CultureInfo.InvariantCulture);
            payload["crewLocationCapturedAt"] = location.CapturedAt.ToString("O", CultureInfo.InvariantCulture);
            if (location.SpeedMph is not null)
            {
                payload["crewSpeedMph"] = location.SpeedMph.Value.ToString(CultureInfo.InvariantCulture);
            }
        }

        return payload;
    }

    private static bool IsTerminal(AssignmentDeliveryStatus status) =>
        status is AssignmentDeliveryStatus.Completed or AssignmentDeliveryStatus.Failed;

    private static AssignmentDeliveryStatus MapStatus(CrewAssignmentStatus status)
    {
        return status switch
        {
            CrewAssignmentStatus.Acknowledged => AssignmentDeliveryStatus.Acknowledged,
            CrewAssignmentStatus.EnRoute => AssignmentDeliveryStatus.EnRoute,
            CrewAssignmentStatus.OnScene => AssignmentDeliveryStatus.OnScene,
            CrewAssignmentStatus.Paused => AssignmentDeliveryStatus.Paused,
            CrewAssignmentStatus.Completed => AssignmentDeliveryStatus.Completed,
            _ => AssignmentDeliveryStatus.Queued
        };
    }

    private static bool ShouldMarkDelivered(AssignmentDeliveryStatus status)
    {
        return status is AssignmentDeliveryStatus.Acknowledged
            or AssignmentDeliveryStatus.Completed;
    }

    private static void ApplyStatusMetadata(AssignmentDelivery delivery, CrewStatusUpdateRequest request)
    {
        delivery.Payload ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        delivery.Payload["lastStatus"] = request.Status.ToString();

        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            delivery.Payload["statusNote"] = request.Note!;
        }
        else
        {
            delivery.Payload.Remove("statusNote");
        }

        if (request.Location is { } location)
        {
            delivery.Payload["statusLatitude"] = location.Latitude.ToString(CultureInfo.InvariantCulture);
            delivery.Payload["statusLongitude"] = location.Longitude.ToString(CultureInfo.InvariantCulture);
            delivery.Payload["statusLocationCapturedAt"] = location.CapturedAt.ToString("O", CultureInfo.InvariantCulture);
            if (location.SpeedMph is not null)
            {
                delivery.Payload["statusSpeedMph"] = location.SpeedMph.Value.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                delivery.Payload.Remove("statusSpeedMph");
            }
        }
        else
        {
            delivery.Payload.Remove("statusLatitude");
            delivery.Payload.Remove("statusLongitude");
            delivery.Payload.Remove("statusLocationCapturedAt");
            delivery.Payload.Remove("statusSpeedMph");
        }
    }
}