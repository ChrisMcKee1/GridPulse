using System;
using System.Collections.Generic;

namespace GridPulse.Application.Models;

public sealed record UserIdentity(
    bool IsAuthenticated,
    string DisplayName,
    IReadOnlyDictionary<string, string> Claims,
    IReadOnlyCollection<string> Roles)
{
    public static UserIdentity Anonymous { get; } = new(
        false,
        "Anonymous",
        new Dictionary<string, string>(),
        Array.Empty<string>());
}
