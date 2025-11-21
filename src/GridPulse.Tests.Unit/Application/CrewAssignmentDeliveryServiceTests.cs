using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using GridPulse.Application.Abstractions;
using GridPulse.Application.Models;
using GridPulse.Application.Services;
using GridPulse.Domain.Entities;
using GridPulse.Domain.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GridPulse.Tests.Unit.Application;

public sealed class CrewAssignmentDeliveryServiceTests
{
    private static readonly Guid TicketId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CrewId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly IAssignmentDeliveryRepository _repository = Substitute.For<IAssignmentDeliveryRepository>();
    private readonly ILogger<CrewAssignmentDeliveryService> _logger = Substitute.For<ILogger<CrewAssignmentDeliveryService>>();
    private readonly TestTimeProvider _timeProvider = new(DateTimeOffset.Parse("2025-11-20T13:00:00Z"));

    [Fact]
    public async Task QueueAssignmentAsync_PersistsNewDeliveryWithTrackingId()
    {
        _repository.GetLatestAsync(TicketId, CrewId, Arg.Any<CancellationToken>()).Returns((AssignmentDelivery?)null);
        var service = CreateService();
        var context = BuildContext();

        var receipt = await service.QueueAssignmentAsync(context);

        receipt.TicketId.Should().Be(TicketId);
        receipt.CrewId.Should().Be(CrewId);
        receipt.DeliveryStatus.Should().Be("queued");
        receipt.TrackingId.Should().NotBeNullOrWhiteSpace();

        await _repository.Received(1).AddAsync(
            Arg.Is<AssignmentDelivery>(delivery =>
                delivery.TicketId == TicketId &&
                delivery.CrewId == CrewId &&
                delivery.Status == AssignmentDeliveryStatus.Queued &&
                delivery.AttemptCount == 1 &&
                delivery.Payload["ticketTitle"] == context.TicketTitle &&
                delivery.CreatedAt == _timeProvider.GetUtcNow()),
            Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueueAssignmentAsync_WhenDeliveryExists_IncrementsAttemptCount()
    {
        var existing = new AssignmentDelivery
        {
            Id = Guid.NewGuid(),
            TicketId = TicketId,
            CrewId = CrewId,
            TrackingId = "trk-original",
            Status = AssignmentDeliveryStatus.Queued,
            AttemptCount = 2,
            Payload = new Dictionary<string, string>(),
            CreatedAt = _timeProvider.GetUtcNow().AddMinutes(-5),
            UpdatedAt = _timeProvider.GetUtcNow().AddMinutes(-5)
        };

        _repository.GetLatestAsync(TicketId, CrewId, Arg.Any<CancellationToken>()).Returns(existing);
        var service = CreateService();

        await service.QueueAssignmentAsync(BuildContext());

        await _repository.Received(1).UpdateAsync(
            Arg.Is<AssignmentDelivery>(delivery =>
                delivery.Id == existing.Id &&
                delivery.AttemptCount == 3 &&
                delivery.Status == AssignmentDeliveryStatus.Queued &&
                delivery.UpdatedAt == _timeProvider.GetUtcNow()),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordStatusAsync_UpdatesDeliveryStatus()
    {
        var existing = new AssignmentDelivery
        {
            Id = Guid.NewGuid(),
            TicketId = TicketId,
            CrewId = CrewId,
            TrackingId = "trk-status",
            Status = AssignmentDeliveryStatus.Queued,
            AttemptCount = 1,
            Payload = new Dictionary<string, string>(),
            CreatedAt = _timeProvider.GetUtcNow().AddMinutes(-15),
            UpdatedAt = _timeProvider.GetUtcNow().AddMinutes(-15)
        };

        _repository.GetLatestAsync(TicketId, CrewId, Arg.Any<CancellationToken>()).Returns(existing);
        var service = CreateService();

        var response = await service.RecordStatusAsync(CrewId, TicketId, new CrewStatusUpdateRequest
        {
            Status = CrewAssignmentStatus.Acknowledged,
            Note = "Departing yard"
        });

        response.TicketId.Should().Be(TicketId);
        response.AcceptedAt.Should().Be(_timeProvider.GetUtcNow());

        await _repository.Received(1).UpdateAsync(
            Arg.Is<AssignmentDelivery>(delivery =>
                delivery.Id == existing.Id &&
                delivery.Status == AssignmentDeliveryStatus.Acknowledged &&
                delivery.DeliveredAt == _timeProvider.GetUtcNow()),
            Arg.Any<CancellationToken>());
    }

    private CrewAssignmentDeliveryService CreateService() =>
        new(_repository, _logger, _timeProvider);

    private static AssignmentDeliveryContext BuildContext()
    {
        return new AssignmentDeliveryContext(
            TicketId,
            CrewId,
            "Pole fire near 5th Ave",
            TicketPriority.High,
            "Crew Alpha",
            22,
            new[] { "TX-100" },
            null,
            false,
            null,
            "Dispatcher One");
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public TestTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}