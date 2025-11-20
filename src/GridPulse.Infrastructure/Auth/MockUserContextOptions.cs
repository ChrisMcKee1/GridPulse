using System.Collections.Generic;

namespace GridPulse.Infrastructure.Auth;

public sealed class MockUserContextOptions
{
    public bool IsAuthenticated { get; set; } = true;

    public string DisplayName { get; set; } = "Auto Operator";

    public IList<string> Roles { get; set; } = new List<string> { "operator", "dispatcher" };

    public IDictionary<string, string> Claims { get; set; } = new Dictionary<string, string>
    {
        ["tenant"] = "default",
        ["scope"] = "ticketing"
    };
}
