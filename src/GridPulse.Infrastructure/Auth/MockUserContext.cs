using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GridPulse.Application.Abstractions;
using GridPulse.Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GridPulse.Infrastructure.Auth;

internal sealed class MockUserContext : IUserContext
{
    private readonly ILogger<MockUserContext> _logger;
    private readonly IReadOnlySet<string> _roleSet;

    public MockUserContext(IOptions<MockUserContextOptions> options, ILogger<MockUserContext> logger)
    {
        _logger = logger;
        ArgumentNullException.ThrowIfNull(options);
        var value = options.Value ?? throw new InvalidOperationException("Mock user context options were not configured.");

        var claims = value.Claims is not null
            ? new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(value.Claims, StringComparer.OrdinalIgnoreCase))
            : new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
        var roles = value.Roles is not null && value.Roles.Count > 0
            ? value.Roles.Select(role => role.Trim()).Where(role => !string.IsNullOrWhiteSpace(role)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            : new[] { "operator" };

        Current = new UserIdentity(
            value.IsAuthenticated,
            string.IsNullOrWhiteSpace(value.DisplayName) ? "Auto Operator" : value.DisplayName,
            claims,
            roles);

        _roleSet = roles.ToHashSet(StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation("Mock user context initialized for persona {DisplayName} with roles: {Roles}", Current.DisplayName, string.Join(", ", roles));
    }

    public UserIdentity Current { get; }

    public bool IsInRole(string role)
    {
        return !string.IsNullOrWhiteSpace(role) && _roleSet.Contains(role);
    }

    public bool HasClaim(string claimType, string? expectedValue = null)
    {
        if (string.IsNullOrWhiteSpace(claimType))
        {
            return false;
        }

        if (!Current.Claims.TryGetValue(claimType, out var value))
        {
            return false;
        }

        return expectedValue is null || string.Equals(value, expectedValue, StringComparison.OrdinalIgnoreCase);
    }
}
