using System;
using System.Collections.Generic;

namespace GridPulse.Application.Models;

/// <summary>
/// Snapshot of the dispatcher decision passed to crew delivery transports.
/// </summary>
public sealed record AssignmentDeliveryContext(
    Guid TicketId,
    Guid CrewId,
    string TicketTitle,
    TicketPriority Priority,
    string CrewName,
    int EtaMinutes,
    IReadOnlyCollection<string> AffectedAssets,
    CrewLocationSnapshotDto? CrewLocation,
    bool RequiresOverrideJustification,
    string? OverrideReason,
    string RequestedBy)
{
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
}