using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using GridPulse.Application.Abstractions;
using GridPulse.Application.Models;
using GridPulse.Domain.Enums;
using GridPulse.Tests.Unit.TestInfrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GridPulse.Tests.Unit.WebApi;

[Collection(TestCollections.Api)]
public sealed class DispatchEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly GridPulseApiFactory _factory;

    public DispatchEndpointTests(GridPulseApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetRecommendations_ReturnsEnvelopeFromService()
    {
        var ticketId = Guid.NewGuid();
        var envelope = CreateEnvelope(ticketId);

        var optimizationService = Substitute.For<IDispatchOptimizationService>();
        optimizationService
            .GetRecommendationsAsync(ticketId, Arg.Any<CancellationToken>())
            .Returns(envelope);

        using var client = CreateClient(services =>
        {
            services.RemoveAll(typeof(IDispatchOptimizationService));
            services.AddSingleton(optimizationService);
        });

        var response = await client.GetAsync($"/api/dispatch/recommendations?ticketId={ticketId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DispatchRecommendationsEnvelope>(JsonOptions);
        body.Should().BeEquivalentTo(envelope);
        await optimizationService.Received(1).GetRecommendationsAsync(ticketId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRecommendations_MissingTicketIdReturnsBadRequest()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/dispatch/recommendations");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PublishAssignment_ReturnsReceipt()
    {
        var ticketId = Guid.NewGuid();
        var crewId = Guid.NewGuid();
        var receipt = new AssignmentReceiptDto(ticketId, crewId, "queued", Guid.NewGuid().ToString());

        var assignmentService = Substitute.For<IDispatchAssignmentService>();
        assignmentService
            .PublishAssignmentAsync(Arg.Any<DispatchAssignmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(receipt);

        using var client = CreateClient(services =>
        {
            services.RemoveAll(typeof(IDispatchAssignmentService));
            services.AddSingleton(assignmentService);
        });

        var request = new DispatchAssignmentRequest
        {
            TicketId = ticketId,
            CrewId = crewId,
            NotifyCrewChannels = new[] { "push" }
        };

        var response = await client.PostAsJsonAsync("/api/dispatch/assignments", request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var body = await response.Content.ReadFromJsonAsync<AssignmentReceiptDto>(JsonOptions);
        body.Should().BeEquivalentTo(receipt);
        await assignmentService.Received(1).PublishAssignmentAsync(Arg.Any<DispatchAssignmentRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAssignment_WhenValidationFails_ReturnsProblemDetails()
    {
        var assignmentService = Substitute.For<IDispatchAssignmentService>();
        assignmentService
            .PublishAssignmentAsync(Arg.Any<DispatchAssignmentRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ArgumentException("Override justification required."));

        using var client = CreateClient(services =>
        {
            services.RemoveAll(typeof(IDispatchAssignmentService));
            services.AddSingleton(assignmentService);
        });

        var request = new DispatchAssignmentRequest
        {
            TicketId = Guid.NewGuid(),
            CrewId = Guid.NewGuid()
        };

        var response = await client.PostAsJsonAsync("/api/dispatch/assignments", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Detail.Should().Contain("Override justification required");
    }

    private HttpClient CreateClient(Action<IServiceCollection>? configureServices = null)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                configureServices?.Invoke(services);
            });
        });

        return factory.CreateClient();
    }

    private static DispatchRecommendationsEnvelope CreateEnvelope(Guid ticketId)
    {
        var now = DateTimeOffset.UtcNow;
        var crewId = Guid.NewGuid();
        var crewDto = new CrewStatusDto(
            crewId,
            "Crew Alpha",
            CrewStatus.Available,
            1,
            new ReadOnlyCollection<CrewSkill>(new[] { CrewSkill.HighVoltage }),
            now.AddMinutes(-5),
            new CrewLocationSnapshotDto(32.779167m, -96.808891m, now.AddMinutes(-1), 34),
            false);

        var recommendation = new DispatchRecommendationDto(
            Guid.NewGuid(),
            ticketId,
            crewDto,
            0.92,
            new ReadOnlyDictionary<string, double>(new Dictionary<string, double>
            {
                ["distance"] = 0.5,
                ["skill"] = 0.3,
                ["workload"] = 0.2
            }),
            25,
            true,
            false,
            null,
            now,
            now.AddMinutes(5));

        var ticketSummary = new TicketSummaryDto(
            ticketId,
            "Downed line",
            TicketPriority.High,
            TicketStatus.InProgress,
            now.AddMinutes(-30),
            320,
            null,
            null);

        return new DispatchRecommendationsEnvelope(ticketSummary, new[] { recommendation });
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
