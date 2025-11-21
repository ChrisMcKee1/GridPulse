using GridPulse.Application.Models;

namespace GridPulse.Application.Abstractions;

/// <summary>
/// Coordinates dispatcher decisions and downstream crew acknowledgements so the Minimal APIs
/// only depend on a single abstraction.
/// </summary>
public interface IDispatchAssignmentService
{
    Task<AssignmentReceiptDto> PublishAssignmentAsync(
        DispatchAssignmentRequest request,
        CancellationToken cancellationToken = default);

    Task<CrewStatusUpdateResponse> ProcessCrewStatusAsync(
        Guid crewId,
        CrewStatusUpdateRequest request,
        CancellationToken cancellationToken = default);
}