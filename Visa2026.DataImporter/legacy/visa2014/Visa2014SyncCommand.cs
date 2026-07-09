using DevExpress.ExpressApp;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014SyncCommand
{
    private static readonly string[] DefaultEntityOrder =
    [
        "Person",
        "Passport",
        "Visa",
        "Education",
        "EmployeePositionHistory",
        "EmployeeSalary",
        "AddressOfResidence",
        "Application",
        "WorkPermit",
        "WorkPermitItem",
        "Invitation",
        "InvitationItem",
        "ApplicationItem",
        "ApplicationProgress",
    ];

    public static async Task<int> RunAsync(IReadOnlyList<string> args, bool verbose)
    {
        if (!HasArg(args, "--inprocess"))
        {
            Console.Error.WriteLine("ERR --sync-visa2014 requires --inprocess (headless XAF ObjectSpace).");
            return 1;
        }

        var entities = ResolveEntities(args);
        if (entities.Count == 0)
        {
            Console.Error.WriteLine("ERR No supported entities to sync.");
            return 1;
        }

        var dataImporterRoot = Visa2014ContentRoot.FindDataImporterRoot();
        if (dataImporterRoot == null)
        {
            Console.Error.WriteLine("ERR Could not locate Visa2026.DataImporter content root.");
            return 1;
        }

        var solutionRoot = Visa2014ContentRoot.FindSolutionRoot();
        Visa2014LegacySourceProfile source;
        try
        {
            source = Visa2014LegacySource.Resolve(dataImporterRoot, solutionRoot, args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR {ex.Message}");
            return 1;
        }

        var targetConnection = GetTargetConnection(args);
        if (string.IsNullOrWhiteSpace(targetConnection))
        {
            Console.Error.WriteLine("ERR --sync-visa2014 requires --target-connection or ConnectionStrings__DefaultConnection.");
            return 1;
        }

        bool dryRun = HasArg(args, "--dry-run");
        bool syncFull = HasArg(args, "--sync-full");
        bool noSoftDelete = HasArg(args, "--no-soft-delete-sync");
        int? maxRows = ParseMaxRows(args);

        var syncStateDir = GetOptionValue(args, "--sync-state-dir");
        var statePath = Visa2014SyncStateStore.ResolveStatePath(dataImporterRoot, source.Id, syncStateDir);
        var state = Visa2014SyncStateStore.LoadOrCreate(statePath, source.Id);

        DateTime? explicitSince = ParseSyncSince(args);
        var sinceUtc = Visa2014SyncStateStore.ResolveSyncSinceUtc(state, explicitSince, syncFull);

        Console.WriteLine("=== VISA2014 sync — delta upsert (headless XAF)");
        Console.WriteLine($"INF Legacy source: {source.Id} ({source.Label})");
        Console.WriteLine($"INF Legacy (read-only): {Visa2014LegacySqlGuard.DescribeLegacyConnection(source.ConnectionString, source.LegacyDatabase)}");
        Console.WriteLine($"INF Target SQL: {MaskConnectionForLog(targetConnection)}");
        Console.WriteLine($"INF Sync state: {statePath}");
        Console.WriteLine($"INF Mode: {(syncFull ? "full mapped row scan" : $"incremental since {sinceUtc:u}")}");
        if (dryRun)
            Console.WriteLine("INF Dry-run: no writes");

        if (!dryRun)
        {
            try
            {
                Visa2014LegacySqlGuard.EnsureLegacyReadCredentials(source.ConnectionString);
                await Visa2014LegacySqlGuard.EnsureLegacyConnectionAsync(source.ConnectionString);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ERR {ex.Message}");
                return 1;
            }
        }

        HashSet<Guid>? changedLegacyOids = null;
        if (!syncFull && !dryRun)
        {
            changedLegacyOids = await Visa2014LegacyAuditChangeQuery.LoadChangedLegacyOidsAsync(
                source.ConnectionString,
                sinceUtc,
                restrictToLegacyOids: null);
            Console.WriteLine($"INF Legacy audit changes since watermark: {changedLegacyOids.Count}");
        }

        var rowFilter = new Visa2014SyncRowFilter
        {
            ProcessAllMappedRows = syncFull,
            ChangedLegacyOids = changedLegacyOids,
        };

        IVisa2014ImportTarget target;
        Visa2014ODataLookupResolver resolver;
        Visa2014HeadlessImportSession? session = null;

        try
        {
            if (dryRun)
            {
                target = new Visa2014DryRunImportTarget();
                resolver = new Visa2014ODataLookupResolver();
            }
            else
            {
                session = await Visa2014HeadlessImportSession.OpenAsync(targetConnection, ResolveBatchSize(args));
                target = session.Target;
                resolver = session.Resolver;
            }

            int exitCode = 0;
            var runStartedUtc = DateTime.UtcNow;

            foreach (var entity in entities)
            {
                var entityExit = await RunEntitySyncAsync(
                    entity,
                    target,
                    resolver,
                    session,
                    source,
                    dataImporterRoot,
                    args,
                    rowFilter,
                    maxRows,
                    dryRun,
                    !noSoftDelete,
                    verbose);

                if (entityExit != 0)
                    exitCode = entityExit;
            }

            if (!dryRun && exitCode == 0)
            {
                state.LastSuccessfulRunUtc = runStartedUtc;
                await Visa2014SyncStateStore.SaveAsync(statePath, state);
                Console.WriteLine($"INF Sync state updated: {statePath}");
            }

            return exitCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR Sync failed: {ex.Message}");
            if (verbose)
                Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            if (session != null)
                await session.DisposeAsync();
        }
    }

    private static async Task<int> RunEntitySyncAsync(
        string entity,
        IVisa2014ImportTarget target,
        Visa2014ODataLookupResolver resolver,
        Visa2014HeadlessImportSession? session,
        Visa2014LegacySourceProfile source,
        string dataImporterRoot,
        IReadOnlyList<string> args,
        Visa2014SyncRowFilter rowFilter,
        int? maxRows,
        bool dryRun,
        bool propagateSoftDeletes,
        bool verbose)
    {
        Console.WriteLine();
        Console.WriteLine($"=== Sync entity: {entity}");

        var idMapPath = GetOptionValue(args, "--id-map-output")
            ?? source.IdMapPath(dataImporterRoot, entity);

        var sync = Visa2014SyncIdMapLoader.CreateContext(idMapPath, rowFilter, propagateSoftDeletes);

        Visa2014SyncEntityResult result;
        try
        {
        if (string.Equals(entity, "Person", StringComparison.OrdinalIgnoreCase))
        {
            result = await Visa2014PersonODataImporter.RunSyncAsync(
                target, resolver, source.ConnectionString, source.LookupTranslationPaths,
                sync, maxRows, verbose);
        }
        else if (string.Equals(entity, "Passport", StringComparison.OrdinalIgnoreCase))
        {
            var personIdMapPath = GetOptionValue(args, "--person-id-map")
                ?? source.IdMapPath(dataImporterRoot, "Person");
            result = await Visa2014PassportODataImporter.RunSyncAsync(
                target, resolver, source.ConnectionString, source.LookupTranslationPaths,
                personIdMapPath, sync, GetTargetConnection(args), maxRows, verbose);
        }
        else if (string.Equals(entity, "Visa", StringComparison.OrdinalIgnoreCase))
        {
            var passportIdMapPath = GetOptionValue(args, "--passport-id-map")
                ?? source.IdMapPath(dataImporterRoot, "Passport");
            result = await Visa2014VisaODataImporter.RunSyncAsync(
                target, resolver, source.ConnectionString, source.LookupTranslationPaths,
                passportIdMapPath, sync, maxRows, verbose);
        }
        else if (string.Equals(entity, "Education", StringComparison.OrdinalIgnoreCase))
        {
            var personIdMapPath = GetOptionValue(args, "--person-id-map")
                ?? source.IdMapPath(dataImporterRoot, "Person");
            result = await Visa2014EducationODataImporter.RunSyncAsync(
                target, resolver, source.ConnectionString, source.LookupTranslationPaths,
                personIdMapPath, sync, maxRows, verbose);
        }
        else if (string.Equals(entity, "EmployeePositionHistory", StringComparison.OrdinalIgnoreCase))
        {
            var personIdMapPath = GetOptionValue(args, "--person-id-map")
                ?? source.IdMapPath(dataImporterRoot, "Person");
            result = await Visa2014EmployeePositionHistoryODataImporter.RunSyncAsync(
                target, resolver, source.ConnectionString, source.LookupTranslationPaths,
                personIdMapPath, sync, maxRows, verbose,
                HasArg(args, "--supplement-permit-positions"));
        }
        else if (string.Equals(entity, "EmployeeSalary", StringComparison.OrdinalIgnoreCase))
        {
            var personIdMapPath = GetOptionValue(args, "--person-id-map")
                ?? source.IdMapPath(dataImporterRoot, "Person");
            result = await Visa2014EmployeeSalaryODataImporter.RunSyncAsync(
                target, source.ConnectionString, source.LookupTranslationPaths,
                personIdMapPath, sync, maxRows, verbose);
        }
        else if (string.Equals(entity, "AddressOfResidence", StringComparison.OrdinalIgnoreCase))
        {
            var personIdMapPath = GetOptionValue(args, "--person-id-map")
                ?? source.IdMapPath(dataImporterRoot, "Person");
            result = await Visa2014AddressOfResidenceODataImporter.RunSyncAsync(
                target, resolver, source.ConnectionString, source.LookupTranslationPaths,
                personIdMapPath, sync, GetTargetConnection(args), maxRows, verbose);
        }
        else if (string.Equals(entity, "Application", StringComparison.OrdinalIgnoreCase))
        {
            if (!dryRun && !HasArg(args, "--skip-tenant-catalog-generation"))
            {
                var catalogExit = Visa2014TenantCatalogGenerationCommand.Run(dataImporterRoot, args, verbose);
                if (catalogExit != 0)
                    return catalogExit;
            }

            result = await Visa2014ApplicationODataImporter.RunSyncAsync(
                target, resolver, source.ConnectionString, source.LookupTranslationPaths,
                sync, maxRows, verbose);
        }
        else if (string.Equals(entity, "WorkPermit", StringComparison.OrdinalIgnoreCase))
        {
            result = await Visa2014WorkPermitODataImporter.RunSyncAsync(
                target, source.ConnectionString, source.LookupTranslationPaths,
                sync, maxRows, verbose);
        }
        else if (string.Equals(entity, "WorkPermitItem", StringComparison.OrdinalIgnoreCase))
        {
            result = await Visa2014WorkPermitItemODataImporter.RunSyncAsync(
                target, source.ConnectionString, source.LookupTranslationPaths,
                GetOptionValue(args, "--person-id-map") ?? source.IdMapPath(dataImporterRoot, "Person"),
                GetOptionValue(args, "--passport-id-map") ?? source.IdMapPath(dataImporterRoot, "Passport"),
                GetOptionValue(args, "--position-history-id-map") ?? source.IdMapPath(dataImporterRoot, "EmployeePositionHistory"),
                GetOptionValue(args, "--work-permit-id-map") ?? source.IdMapPath(dataImporterRoot, "WorkPermit"),
                sync, GetTargetConnection(args), maxRows, verbose);
        }
        else if (string.Equals(entity, "Invitation", StringComparison.OrdinalIgnoreCase))
        {
            var applicationIdMapPath = GetOptionValue(args, "--application-id-map")
                ?? source.IdMapPath(dataImporterRoot, "Application");
            var applicationIdMap = Visa2014IdMapHelper.Load(applicationIdMapPath);
            result = await Visa2014InvitationODataImporter.RunSyncAsync(
                target, source.ConnectionString, source.LookupTranslationPaths,
                applicationIdMap, session?.ObjectSpaceFactory, sync, maxRows, verbose);
        }
        else if (string.Equals(entity, "InvitationItem", StringComparison.OrdinalIgnoreCase))
        {
            result = await Visa2014InvitationItemODataImporter.RunSyncAsync(
                target, source.ConnectionString, source.LookupTranslationPaths,
                GetOptionValue(args, "--person-id-map") ?? source.IdMapPath(dataImporterRoot, "Person"),
                GetOptionValue(args, "--passport-id-map") ?? source.IdMapPath(dataImporterRoot, "Passport"),
                GetOptionValue(args, "--invitation-id-map") ?? source.IdMapPath(dataImporterRoot, "Invitation"),
                sync, maxRows, verbose);
        }
        else if (string.Equals(entity, "ApplicationItem", StringComparison.OrdinalIgnoreCase))
        {
            result = await Visa2014ApplicationItemODataImporter.RunSyncAsync(
                target, resolver, source.ConnectionString, source.LookupTranslationPaths,
                GetOptionValue(args, "--application-id-map") ?? source.IdMapPath(dataImporterRoot, "Application"),
                GetOptionValue(args, "--person-id-map") ?? source.IdMapPath(dataImporterRoot, "Person"),
                GetOptionValue(args, "--passport-id-map") ?? source.IdMapPath(dataImporterRoot, "Passport"),
                GetOptionValue(args, "--visa-id-map") ?? source.IdMapPath(dataImporterRoot, "Visa"),
                GetOptionValue(args, "--position-history-id-map") ?? source.IdMapPath(dataImporterRoot, "EmployeePositionHistory"),
                GetOptionValue(args, "--address-id-map") ?? source.IdMapPath(dataImporterRoot, "AddressOfResidence"),
                GetOptionValue(args, "--education-id-map") ?? source.IdMapPath(dataImporterRoot, "Education"),
                GetOptionValue(args, "--employee-salary-id-map") ?? source.IdMapPath(dataImporterRoot, "EmployeeSalary"),
                GetOptionValue(args, "--work-permit-item-id-map") ?? source.IdMapPath(dataImporterRoot, "WorkPermitItem"),
                GetOptionValue(args, "--invitation-item-id-map") ?? source.IdMapPath(dataImporterRoot, "InvitationItem"),
                sync, GetTargetConnection(args), maxRows, verbose);
        }
        else if (string.Equals(entity, "ApplicationProgress", StringComparison.OrdinalIgnoreCase))
        {
            result = await Visa2014ApplicationProgressODataImporter.RunSyncAsync(
                target, resolver, source.ConnectionString, source.LookupTranslationPaths,
                GetOptionValue(args, "--application-id-map") ?? source.IdMapPath(dataImporterRoot, "Application"),
                sync, maxRows, verbose,
                session?.ObjectSpaceFactory,
                GetTargetConnection(args));
        }
        else
        {
            Console.Error.WriteLine($"ERR Entity '{entity}' is not supported for sync.");
            return 1;
        }

        if (!dryRun && propagateSoftDeletes)
        {
            var entityType = ResolveEntityType(entity);
            var softErrors = result.Errors is List<string> list ? list : result.Errors.ToList();
            var softDeleted = await Visa2014SyncUpsertHelper.ApplySoftDeletesForEntityAsync(
                target,
                entityType,
                entity,
                source.ConnectionString,
                sync,
                verbose,
                softErrors);
            if (softDeleted > 0)
                result = Visa2014SyncUpsertHelper.WithSoftDeletedCount(result, softDeleted);
        }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR {entity} sync failed: {ex.Message}");
            if (verbose)
                Console.Error.WriteLine(ex);
            return 1;
        }

        PrintEntityResult(entity, result);
        return result.FailedCount > 0 ? 1 : 0;
    }

    private static void PrintEntityResult(string entity, Visa2014SyncEntityResult result)
    {
        Console.WriteLine($"INF {entity} legacy rows: {result.LegacyRowCount}");
        Console.WriteLine(
            $"INF Inserted: {result.InsertedCount}  Updated: {result.UpdatedCount}  " +
            $"Relinked: {result.RelinkedCount}  " +
            $"Skipped unchanged: {result.SkippedUnchangedCount}  Soft-deleted: {result.SoftDeletedCount}  " +
            $"Failed: {result.FailedCount}");
        if (result.IdMapPath != null)
            Console.WriteLine($"INF Id-map: {result.IdMapPath}");
        if (result.FailedCount > 0)
        {
            foreach (var error in result.Errors.Take(10))
                Console.Error.WriteLine($"ERR {error}");
            if (result.Errors.Count > 10)
                Console.Error.WriteLine($"ERR ... and {result.Errors.Count - 10} more");
        }
    }

    private static Type ResolveEntityType(string entity) =>
        entity switch
        {
            "Person" => typeof(Visa2026.Module.BusinessObjects.Person),
            "Passport" => typeof(Visa2026.Module.BusinessObjects.Passport),
            "Visa" => typeof(Visa2026.Module.BusinessObjects.Visa),
            "Education" => typeof(Visa2026.Module.BusinessObjects.Education),
            "EmployeePositionHistory" => typeof(Visa2026.Module.BusinessObjects.EmployeePositionHistory),
            "EmployeeSalary" => typeof(Visa2026.Module.BusinessObjects.EmployeeSalary),
            "AddressOfResidence" => typeof(Visa2026.Module.BusinessObjects.AddressOfResidence),
            "Application" => typeof(Visa2026.Module.BusinessObjects.Application),
            "ApplicationItem" => typeof(Visa2026.Module.BusinessObjects.ApplicationItem),
            "ApplicationProgress" => typeof(Visa2026.Module.BusinessObjects.ApplicationProgress),
            "WorkPermit" => typeof(Visa2026.Module.BusinessObjects.WorkPermit),
            "WorkPermitItem" => typeof(Visa2026.Module.BusinessObjects.WorkPermitItem),
            "Invitation" => typeof(Visa2026.Module.BusinessObjects.Invitation),
            "InvitationItem" => typeof(Visa2026.Module.BusinessObjects.InvitationItem),
            _ => throw new NotSupportedException($"Unknown entity type: {entity}"),
        };

    private static IReadOnlyList<string> ResolveEntities(IReadOnlyList<string> args)
    {
        var entityArg = GetOptionValue(args, "--entity");
        if (!string.IsNullOrWhiteSpace(entityArg))
        {
            return entityArg
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(e => DefaultEntityOrder.Contains(e, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        return DefaultEntityOrder;
    }

    private static int? ParseMaxRows(IReadOnlyList<string> args)
    {
        if (int.TryParse(GetOptionValue(args, "--max-rows"), out var parsed) && parsed > 0)
            return parsed;
        return null;
    }

    private static DateTime? ParseSyncSince(IReadOnlyList<string> args)
    {
        var text = GetOptionValue(args, "--sync-since");
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (DateTime.TryParse(text, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var parsed))
            return parsed;

        Console.Error.WriteLine($"WRN Could not parse --sync-since '{text}' — using sync state watermark.");
        return null;
    }

    private static string GetTargetConnection(IReadOnlyList<string> args) =>
        GetOptionValue(args, "--target-connection")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? Environment.GetEnvironmentVariable("VISA2026_SQL_CONNECTION")
        ?? "";

    private static int ResolveBatchSize(IReadOnlyList<string> args)
    {
        if (int.TryParse(GetOptionValue(args, "--batch-size"), out var size) && size > 0)
            return size;
        return 50;
    }

    private static bool HasArg(IReadOnlyList<string> args, string flag) =>
        args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

    private static string? GetOptionValue(IReadOnlyList<string> args, string optionName)
    {
        for (int i = 0; i < args.Count; i++)
        {
            if (!string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (i + 1 < args.Count && !args[i + 1].StartsWith('-'))
                return args[i + 1];
            return null;
        }

        return null;
    }

    private static string MaskConnectionForLog(string connectionString)
    {
        string? server = null;
        string? database = null;
        bool trusted = connectionString.Contains("Trusted_Connection=True", StringComparison.OrdinalIgnoreCase);

        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.StartsWith("Server=", StringComparison.OrdinalIgnoreCase))
                server = part["Server=".Length..].Trim();
            else if (part.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
                server = part["Data Source=".Length..].Trim();
            else if (part.StartsWith("Database=", StringComparison.OrdinalIgnoreCase))
                database = part["Database=".Length..].Trim();
            else if (part.StartsWith("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
                database = part["Initial Catalog=".Length..].Trim();
        }

        return $"Server={server ?? "?"};Database={database ?? "?"};Auth={(trusted ? "Windows" : "SQL")}";
    }
}
