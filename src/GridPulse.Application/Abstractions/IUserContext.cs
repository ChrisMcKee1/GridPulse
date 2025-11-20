using GridPulse.Application.Models;

namespace GridPulse.Application.Abstractions;

/// <summary>
/// Provides the current authenticated user identity so endpoints and UI components can be authored
/// without coupling to a concrete authentication provider. The placeholder implementation can be
/// swapped for real Entra backing without touching feature code.
/// </summary>
public interface IUserContext
{
    UserIdentity Current { get; }

    bool IsInRole(string role);

    bool HasClaim(string claimType, string? expectedValue = null);
}
