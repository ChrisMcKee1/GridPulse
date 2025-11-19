using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using GridPulse.Domain.Entities;
using GridPulse.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GridPulse.Infrastructure.Persistence.SampleData;

internal sealed class OutageCsvSampleDataSeeder : IDataSeeder
{
    private readonly GridPulseDbContext _dbContext;
    private readonly SampleDataOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<OutageCsvSampleDataSeeder> _logger;

    public OutageCsvSampleDataSeeder(
        GridPulseDbContext dbContext,
        IOptions<SampleDataOptions> options,
        IHostEnvironment environment,
        ILogger<OutageCsvSampleDataSeeder> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.Outages.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInformation("Sample data seeding skipped because outages already exist.");
            return;
        }

        var rootPath = ResolveRootPath();
        var outagesPath = Path.Combine(rootPath, _options.OutagesFile);
        var outageEventsPath = Path.Combine(rootPath, _options.OutageEventsFile);

        if (!File.Exists(outagesPath))
        {
            _logger.LogWarning("Outage sample data file {FilePath} was not found; skipping seed.", outagesPath);
            return;
        }

        var outages = ReadOutages(outagesPath);
        var outageEvents = File.Exists(outageEventsPath)
            ? ReadOutageEvents(outageEventsPath)
            : new List<OutageEvent>();

        if (outages.Count == 0)
        {
            _logger.LogWarning("No outage rows were parsed from {FilePath}; skipping seed.", outagesPath);
            return;
        }

        await _dbContext.Outages.AddRangeAsync(outages, cancellationToken).ConfigureAwait(false);

        var eventsPersistedCount = 0;
        if (outageEvents.Count > 0)
        {
            var outageIds = new HashSet<Guid>(outages.Select(o => o.Id));
            var filteredEvents = outageEvents.Where(e => outageIds.Contains(e.OutageId)).ToList();
            if (filteredEvents.Count != outageEvents.Count)
            {
                _logger.LogWarning("Filtered {RemovedCount} outage events because their outages were missing in the CSV payload.", outageEvents.Count - filteredEvents.Count);
            }

            if (filteredEvents.Count > 0)
            {
                eventsPersistedCount = filteredEvents.Count;
                await _dbContext.OutageEvents.AddRangeAsync(filteredEvents, cancellationToken).ConfigureAwait(false);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Seeded {OutageCount} outages and {EventCount} outage events from CSV sample data.",
            outages.Count,
            eventsPersistedCount);
    }

    private string ResolveRootPath()
    {
        var configuredRoot = _options.RootPath;
        if (string.IsNullOrWhiteSpace(configuredRoot))
        {
            return _environment.ContentRootPath;
        }

        if (Path.IsPathRooted(configuredRoot))
        {
            return configuredRoot;
        }

        var contentRootCandidate = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, configuredRoot));
        if (Directory.Exists(contentRootCandidate))
        {
            return contentRootCandidate;
        }

        var baseDirectoryCandidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredRoot));
        if (Directory.Exists(baseDirectoryCandidate))
        {
            return baseDirectoryCandidate;
        }

        return contentRootCandidate;
    }

    private static List<Outage> ReadOutages(string path)
    {
        using var reader = new StreamReader(path);
        using var csv = CreateCsvReader(reader);
        csv.Context.RegisterClassMap<OutageCsvMap>();
        return csv.GetRecords<OutageCsvRow>()
                  .Select(ToOutage)
                  .ToList();
    }

    private static List<OutageEvent> ReadOutageEvents(string path)
    {
        using var reader = new StreamReader(path);
        using var csv = CreateCsvReader(reader);
        csv.Context.RegisterClassMap<OutageEventCsvMap>();
        return csv.GetRecords<OutageEventCsvRow>()
                  .Select(ToOutageEvent)
                  .ToList();
    }

    private static CsvReader CreateCsvReader(TextReader textReader)
    {
        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            MissingFieldFound = null,
            HeaderValidated = null,
            TrimOptions = TrimOptions.Trim,
            IgnoreBlankLines = true
        };

        var csv = new CsvReader(textReader, configuration);

        var offsetOptions = csv.Context.TypeConverterOptionsCache.GetOptions<DateTimeOffset?>();
        if (!offsetOptions.NullValues.Contains(string.Empty))
        {
            offsetOptions.NullValues.Add(string.Empty);
        }

        var statusOptions = csv.Context.TypeConverterOptionsCache.GetOptions<OutageStatus?>();
        if (!statusOptions.NullValues.Contains(string.Empty))
        {
            statusOptions.NullValues.Add(string.Empty);
        }

        return csv;
    }

    private static Outage ToOutage(OutageCsvRow row) => new()
    {
        Id = row.Id,
        ServiceLocationId = row.ServiceLocationId,
        ServiceAddress = row.ServiceAddress,
        Status = row.Status,
        ReportedAt = row.ReportedAt,
        LastUpdatedAt = row.LastUpdatedAt,
        EstimatedRestoration = row.EstimatedRestoration,
        Cause = string.IsNullOrWhiteSpace(row.Cause) ? null : row.Cause
    };

    private static OutageEvent ToOutageEvent(OutageEventCsvRow row) => new()
    {
        Id = row.Id,
        OutageId = row.OutageId,
        Timestamp = row.Timestamp,
        Type = row.Type,
        StatusFrom = row.StatusFrom,
        StatusTo = row.StatusTo,
        Message = string.IsNullOrWhiteSpace(row.Message) ? null : row.Message,
        CreatedBy = string.IsNullOrWhiteSpace(row.CreatedBy) ? "system" : row.CreatedBy
    };

    private sealed record OutageCsvRow
    {
        public required Guid Id { get; init; }
        public required Guid ServiceLocationId { get; init; }
        public required string ServiceAddress { get; init; }
        public required OutageStatus Status { get; init; }
        public required DateTimeOffset ReportedAt { get; init; }
        public required DateTimeOffset LastUpdatedAt { get; init; }
        public DateTimeOffset? EstimatedRestoration { get; init; }
        public string? Cause { get; init; }
    }

    private sealed record OutageEventCsvRow
    {
        public required Guid Id { get; init; }
        public required Guid OutageId { get; init; }
        public required DateTimeOffset Timestamp { get; init; }
        public required OutageEventType Type { get; init; }
        public OutageStatus? StatusFrom { get; init; }
        public OutageStatus? StatusTo { get; init; }
        public string? Message { get; init; }
        public string? CreatedBy { get; init; }
    }

    private sealed class OutageCsvMap : ClassMap<OutageCsvRow>
    {
        public OutageCsvMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
        }
    }

    private sealed class OutageEventCsvMap : ClassMap<OutageEventCsvRow>
    {
        public OutageEventCsvMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
        }
    }
}
