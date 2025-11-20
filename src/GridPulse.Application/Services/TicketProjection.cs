using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GridPulse.Application.Services;

internal static class TicketProjection
{
    public static TicketDto ToDto(Ticket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        var events = (ticket.Events ?? Array.Empty<AssignmentEvent>())
            .OrderBy(evt => evt.OccurredAt)
            .Select(MapEvent)
            .ToArray();

        var recommendations = (ticket.Recommendations ?? Array.Empty<DispatchRecommendation>())
            .OrderByDescending(rec => rec.CompositeScore)
            .ThenByDescending(rec => rec.CreatedAt)
            .Select(MapRecommendation)
            .ToArray();

        return new TicketDto(
            ticket.Id,
            ticket.Title,
            ticket.OutageReferenceId,
            ticket.Priority,
            ticket.Status,
            ticket.CustomerImpact,
            ticket.EtaMinutes,
            ticket.AssignedCrewId,
            ResolveAssignedCrewName(ticket, recommendations),
            (ticket.AffectedAssets ?? Array.Empty<string>()).ToArray(),
            ticket.OpenedAt,
            ticket.UpdatedAt,
            ticket.ClosedAt,
            events,
            recommendations);
    }

    private static string? ResolveAssignedCrewName(Ticket ticket, IReadOnlyCollection<DispatchRecommendationDto> mappedRecommendations)
    {
        if (ticket.AssignedCrewId is not Guid crewId)
        {
            return null;
        }

        var match = mappedRecommendations.FirstOrDefault(dto => dto.Crew.CrewId == crewId);
        return match?.Crew.DisplayName;
    }

    private static AssignmentEventDto MapEvent(AssignmentEvent assignmentEvent)
    {
        var details = assignmentEvent.Details is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(assignmentEvent.Details, StringComparer.OrdinalIgnoreCase);

        return new AssignmentEventDto(
            assignmentEvent.Id,
            assignmentEvent.EventType,
            assignmentEvent.Actor,
            assignmentEvent.OccurredAt,
            assignmentEvent.CrewId,
            new ReadOnlyDictionary<string, string>(details));
    }

    private static DispatchRecommendationDto MapRecommendation(DispatchRecommendation recommendation)
    {
        var crewDto = recommendation.Crew is null
            ? CreatePlaceholderCrew(recommendation.CrewId)
            : MapCrew(recommendation.Crew);

        var scoreComponents = recommendation.ScoreComponents is null
            ? new Dictionary<string, double>()
            : new Dictionary<string, double>(recommendation.ScoreComponents, StringComparer.OrdinalIgnoreCase);

        return new DispatchRecommendationDto(
            recommendation.Id,
            recommendation.TicketId,
            crewDto,
            recommendation.CompositeScore,
            new ReadOnlyDictionary<string, double>(scoreComponents),
            recommendation.RecommendedRouteEtaMinutes,
            recommendation.IsAutoSelected,
            recommendation.IsOverride,
            recommendation.OverrideReason,
            recommendation.CreatedAt,
            recommendation.ExpiresAt);
    }

    private static CrewStatusDto CreatePlaceholderCrew(Guid crewId)
    {
        return new CrewStatusDto(
            crewId,
            $"Crew {crewId.ToString()[..8]}",
            CrewStatus.Available,
            0,
            Array.Empty<CrewSkill>(),
            DateTimeOffset.MinValue,
            null,
            true);
    }

    private static CrewStatusDto MapCrew(Crew crew)
    {
        var location = crew.LocationHistory?
            .OrderByDescending(snapshot => snapshot.CapturedAt)
            .FirstOrDefault();

        var locationDto = location is null
            ? null
            : new CrewLocationSnapshotDto(
                location.Latitude,
                location.Longitude,
                location.CapturedAt,
                location.SpeedMph);

        return new CrewStatusDto(
            crew.Id,
            crew.DisplayName,
            crew.Status,
            crew.CurrentTicketCount,
            (crew.Skills ?? Array.Empty<CrewSkill>()).ToArray(),
            crew.LastStatusUpdate,
            locationDto,
            location?.IsStale ?? true);
    }
}
