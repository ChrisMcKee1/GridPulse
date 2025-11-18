namespace GridPulse.Infrastructure.Options;

public sealed class OracleOptions
{
    public const string SectionName = "Oracle";

    public string DataSource { get; set; } = string.Empty;
    public string? TnsAdminPath { get; set; }
    public string? WalletPasswordSecretName { get; set; }
}
