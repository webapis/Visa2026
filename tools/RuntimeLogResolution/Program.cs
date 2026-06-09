using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Visa2026.Module.BusinessObjects.Operations;
using Visa2026.Module.Services.RuntimeLogging;

namespace Visa2026.Tools.RuntimeLogResolution;

internal static class RepoPaths
{
    public static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Visa2026.slnx")))
                return dir.FullName;

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repo root (Visa2026.slnx).");
    }

    public static string DefaultInboxDirectory() =>
        Path.Combine(FindRepoRoot(), ".cursor", "runtime-errors", "inbox");

    public static string DefaultArchiveDirectory() =>
        Path.Combine(FindRepoRoot(), ".cursor", "runtime-errors", "archive");
}

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
            return PrintUsage();

        string? connectionString = null;
        string? inboxPath = null;
        var commandArgs = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--connection" when i + 1 < args.Length:
                    connectionString = args[++i];
                    break;
                case "--inbox" when i + 1 < args.Length:
                    inboxPath = args[++i];
                    break;
                default:
                    commandArgs.Add(args[i]);
                    break;
            }
        }

        if (commandArgs.Count == 0)
            return PrintUsage();

        connectionString ??= ResolveDefaultConnectionString();
        inboxPath ??= RepoPaths.DefaultInboxDirectory();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString
            })
            .Build();

        var resolution = new EfApplicationRuntimeLogResolution(configuration);
        var command = commandArgs[0].ToLowerInvariant();

        try
        {
            return command switch
            {
                "list-open" => await ListOpenAsync(resolution, commandArgs),
                "get" => await GetAsync(resolution, commandArgs),
                "mark-in-progress" => await MarkAsync(resolution, commandArgs, ApplicationRuntimeLogResolutionStatus.InProgress, inboxPath),
                "mark-fixed" => await MarkAsync(resolution, commandArgs, ApplicationRuntimeLogResolutionStatus.Fixed, inboxPath),
                "mark-ignored" => await MarkAsync(resolution, commandArgs, ApplicationRuntimeLogResolutionStatus.Ignored, inboxPath),
                "archive-inbox" => ArchiveInbox(commandArgs, inboxPath),
                "pull-remote" => await PullRemoteAsync(configuration, commandArgs, inboxPath),
                _ => PrintUsage()
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static int PrintUsage()
    {
        Console.WriteLine("""
Visa2026 runtime log resolution CLI (Cursor agent + admin triage).

Usage:
  dotnet run --project tools/RuntimeLogResolution -- <command> [options]

Commands:
  list-open [--limit N]                 List open/ack/in-progress rows (default limit 10)
  get --id <guid>                       Print one row as JSON
  mark-in-progress --id <guid> [--by name] [--agent-run-id id]
  mark-fixed --id <guid> [--notes text] [--by name] [--commit hash] [--agent-run-id id]
  mark-ignored --id <guid> [--notes text] [--by name]
  archive-inbox --id <guid>             Move inbox JSON to archive after fixed
  pull-remote [--since 15m|1h|24h|ISO]  Query SQL and write Cursor inbox JSON locally

Options:
  --connection <cs>   SQL connection (default: LocalDB Visa2026 dev)
  --inbox <path>      Inbox directory (default: .cursor/runtime-errors/inbox)
  --limit N           For list-open
  --id <guid>         Runtime log row id
  --notes <text>      Resolution notes
  --by <name>         ResolvedBy (default: cursor-agent)
  --commit <hash>     Fix commit hash
  --agent-run-id <id> Cursor agent / automation id
  --since <duration|ISO> For pull-remote (default 24h)
  --min-level Error|Warning|Critical   For pull-remote (default Error)
  --source-slot <name>                 Label inbox JSON (e.g. Production)
  --source-database <name>             Label inbox JSON (e.g. Visa2026DbProd)
  --include-existing                   Write even when inbox file already exists

Examples:
  dotnet run --project tools/RuntimeLogResolution -- list-open --limit 5
  dotnet run --project tools/RuntimeLogResolution -- mark-in-progress --id 11111111-1111-1111-1111-111111111111
  dotnet run --project tools/RuntimeLogResolution -- mark-fixed --id 11111111-1111-1111-1111-111111111111 --notes "Fixed schema SQL"
  dotnet run --project tools/RuntimeLogResolution -- pull-remote --connection "Server=..." --since 1h --source-slot Production --source-database Visa2026DbProd
""");
        return 1;
    }

    private static async Task<int> PullRemoteAsync(
        IConfiguration configuration,
        IReadOnlyList<string> args,
        string inboxPath)
    {
        int limit = 50;
        var sinceUtc = ResolveSinceUtc(ReadOption(args, "--since"));
        var minSeverity = ParseMinSeverity(ReadOption(args, "--min-level"));
        var sourceSlot = ReadOption(args, "--source-slot");
        var sourceDatabase = ReadOption(args, "--source-database");
        var skipExisting = !args.Any(a => string.Equals(a, "--include-existing", StringComparison.OrdinalIgnoreCase));

        for (int i = 1; i < args.Count - 1; i++)
        {
            if (args[i] == "--limit" && int.TryParse(args[i + 1], out var parsed))
                limit = parsed;
        }

        var pull = new EfApplicationRuntimeLogRemotePull(configuration);
        var result = await pull.PullAsync(
            sinceUtc,
            limit,
            inboxPath,
            minSeverity,
            skipExisting,
            sourceSlot,
            sourceDatabase).ConfigureAwait(false);

        Console.WriteLine(JsonSerializer.Serialize(new
        {
            sinceUtc,
            inboxPath,
            sourceSlot,
            sourceDatabase,
            result.QueriedCount,
            result.WrittenCount,
            result.SkippedCount,
            result.NewestOccurredAtUtc,
            writtenIds = result.WrittenIds
        }, JsonOptions));

        return 0;
    }

    private static DateTime ResolveSinceUtc(string? since)
    {
        if (string.IsNullOrWhiteSpace(since))
            return DateTime.UtcNow.AddHours(-24);

        if (TryParseDuration(since, out var duration))
            return DateTime.UtcNow.Subtract(duration);

        if (DateTime.TryParse(since, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var parsed))
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);

        throw new InvalidOperationException($"Invalid --since value: {since}");
    }

    private static bool TryParseDuration(string value, out TimeSpan duration)
    {
        duration = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        value = value.Trim().ToLowerInvariant();
        if (value.Length < 2)
            return false;

        var unit = value[^1];
        if (!int.TryParse(value[..^1], out var amount) || amount <= 0)
            return false;

        duration = unit switch
        {
            'm' => TimeSpan.FromMinutes(amount),
            'h' => TimeSpan.FromHours(amount),
            'd' => TimeSpan.FromDays(amount),
            _ => default
        };

        return duration > TimeSpan.Zero;
    }

    private static ApplicationRuntimeLogSeverity ParseMinSeverity(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ApplicationRuntimeLogSeverity.Error;

        return value.Trim().ToLowerInvariant() switch
        {
            "warning" => ApplicationRuntimeLogSeverity.Warning,
            "critical" => ApplicationRuntimeLogSeverity.Critical,
            "error" => ApplicationRuntimeLogSeverity.Error,
            _ => throw new InvalidOperationException($"Invalid --min-level: {value}")
        };
    }

    private static string ResolveDefaultConnectionString() =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? "Server=(localdb)\\mssqllocaldb;Database=Visa2026;Trusted_Connection=True;MultipleActiveResultSets=true";

    private static async Task<int> ListOpenAsync(EfApplicationRuntimeLogResolution resolution, IReadOnlyList<string> args)
    {
        int limit = 10;
        for (int i = 1; i < args.Count - 1; i++)
        {
            if (args[i] == "--limit" && int.TryParse(args[i + 1], out var parsed))
                limit = parsed;
        }

        var rows = await resolution.ListOpenAsync(limit).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(rows, JsonOptions));
        return 0;
    }

    private static async Task<int> GetAsync(EfApplicationRuntimeLogResolution resolution, IReadOnlyList<string> args)
    {
        var id = ParseRequiredGuid(args, "--id");
        var row = await resolution.GetAsync(id).ConfigureAwait(false);
        if (row == null)
        {
            Console.Error.WriteLine($"Runtime log row not found: {id:D}");
            return 1;
        }

        Console.WriteLine(JsonSerializer.Serialize(row, JsonOptions));
        return 0;
    }

    private static async Task<int> MarkAsync(
        EfApplicationRuntimeLogResolution resolution,
        IReadOnlyList<string> args,
        ApplicationRuntimeLogResolutionStatus status,
        string inboxPath)
    {
        var id = ParseRequiredGuid(args, "--id");
        var update = new RuntimeLogResolutionUpdate
        {
            Id = id,
            Status = status,
            ResolvedBy = ReadOption(args, "--by") ?? "cursor-agent",
            ResolutionNotes = ReadOption(args, "--notes"),
            FixCommitHash = ReadOption(args, "--commit"),
            AgentRunId = ReadOption(args, "--agent-run-id")
        };

        var applied = await resolution.TryApplyAsync(update).ConfigureAwait(false);
        if (!applied)
        {
            Console.Error.WriteLine($"Runtime log row not found: {id:D}");
            return 1;
        }

        if (status is ApplicationRuntimeLogResolutionStatus.Fixed or ApplicationRuntimeLogResolutionStatus.Ignored)
            ArchiveInboxFile(id, inboxPath);

        Console.WriteLine($"Updated {id:D} -> {status}");
        return 0;
    }

    private static int ArchiveInbox(IReadOnlyList<string> args, string inboxPath)
    {
        var id = ParseRequiredGuid(args, "--id");
        ArchiveInboxFile(id, inboxPath);
        Console.WriteLine($"Archived inbox file for {id:D}");
        return 0;
    }

    private static void ArchiveInboxFile(Guid id, string inboxPath)
    {
        var source = Path.Combine(inboxPath, $"{id:D}.json");
        if (!File.Exists(source))
            return;

        var archiveDirectory = RepoPaths.DefaultArchiveDirectory();
        Directory.CreateDirectory(archiveDirectory);
        var destination = Path.Combine(archiveDirectory, $"{id:D}.json");
        File.Move(source, destination, overwrite: true);
    }

    private static Guid ParseRequiredGuid(IReadOnlyList<string> args, string optionName)
    {
        var value = ReadOption(args, optionName);
        if (string.IsNullOrWhiteSpace(value) || !Guid.TryParse(value, out var id))
            throw new InvalidOperationException($"Missing or invalid {optionName} <guid>.");

        return id;
    }

    private static string? ReadOption(IReadOnlyList<string> args, string optionName)
    {
        for (int i = 0; i < args.Count - 1; i++)
        {
            if (string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return null;
    }
}
