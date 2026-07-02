namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014ImportCommand
{
    public static async Task<int> RunAsync(IReadOnlyList<string> args, bool verbose)
    {
        var entity = GetOptionValue(args, "--entity");
        if (string.IsNullOrWhiteSpace(entity))
        {
            Console.Error.WriteLine("ERR --import-visa2014 requires --entity <Name> (e.g. Person, Passport).");
            return 1;
        }

        var supported = string.Equals(entity, "Person", StringComparison.OrdinalIgnoreCase)
            || string.Equals(entity, "Passport", StringComparison.OrdinalIgnoreCase)
            || string.Equals(entity, "Visa", StringComparison.OrdinalIgnoreCase)
            || string.Equals(entity, "Education", StringComparison.OrdinalIgnoreCase)
            || string.Equals(entity, "Application", StringComparison.OrdinalIgnoreCase)
            || string.Equals(entity, "ApplicationProgress", StringComparison.OrdinalIgnoreCase)
            || string.Equals(entity, "EmployeePositionHistory", StringComparison.OrdinalIgnoreCase)
            || string.Equals(entity, "EmployeeSalary", StringComparison.OrdinalIgnoreCase)
            || string.Equals(entity, "AddressOfResidence", StringComparison.OrdinalIgnoreCase)
            || string.Equals(entity, "ApplicationItem", StringComparison.OrdinalIgnoreCase);
        if (!supported)
        {
            Console.Error.WriteLine($"ERR Entity '{entity}' is not supported yet. Supported: Person, Passport, Visa, Education, Application, ApplicationProgress, ApplicationItem, EmployeePositionHistory, EmployeeSalary, AddressOfResidence.");
            return 1;
        }

        var dataImporterRoot = Visa2014ContentRoot.FindDataImporterRoot();
        if (dataImporterRoot == null)
        {
            Console.Error.WriteLine("ERR Could not locate Visa2026.DataImporter content root.");
            return 1;
        }

        var importSessionStartedUtc = DateTime.UtcNow;

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

        var apiBaseUrl = GetOptionValue(args, "--api-base-url")
            ?? Environment.GetEnvironmentVariable("ApiOptions__BaseUrl")
            ?? Environment.GetEnvironmentVariable("API_BASE_URL")
            ?? "https://localhost:5001";
        var userName = GetOptionValue(args, "--user") ?? "Admin";
        var password = GetOptionValue(args, "--password") ?? "";

        var idMapPath = GetOptionValue(args, "--id-map-output")
            ?? source.IdMapPath(dataImporterRoot, entity);

        int? maxRows = null;
        var maxRowsText = GetOptionValue(args, "--max-rows");
        if (int.TryParse(maxRowsText, out var parsedMax) && parsedMax > 0)
            maxRows = parsedMax;

        bool dryRun = HasArg(args, "--dry-run");
        bool noWait = HasArg(args, "--no-wait");
        bool inProcess = HasArg(args, "--inprocess");

        if (!dryRun
            && string.Equals(entity, "Application", StringComparison.OrdinalIgnoreCase)
            && !HasArg(args, "--skip-tenant-catalog-generation"))
        {
            var catalogExit = Visa2014TenantCatalogGenerationCommand.Run(dataImporterRoot, args, verbose);
            if (catalogExit != 0)
                return catalogExit;
        }

        Console.WriteLine($"=== VISA2014 import — {entity}" + (inProcess ? " (headless XAF)" : " (OData)"));
        Console.WriteLine($"INF Legacy source: {source.Id} ({source.Label})");
        Console.WriteLine($"INF Legacy (read-only): {Visa2014LegacySqlGuard.DescribeLegacyConnection(source.ConnectionString, source.LegacyDatabase)}");
        if (inProcess)
        {
            var targetConnection = GetTargetConnection(args);
            if (string.IsNullOrWhiteSpace(targetConnection))
            {
                Console.Error.WriteLine("ERR --inprocess requires --target-connection or ConnectionStrings__DefaultConnection.");
                return 1;
            }

            Console.WriteLine($"INF Target (write): Visa2026 in-process ObjectSpace");
            Console.WriteLine($"INF Target SQL: {MaskConnectionForLog(targetConnection)}");
        }
        else
        {
            Console.WriteLine($"INF Target (write): Visa2026 via OData at {apiBaseUrl}");
        }
        Console.WriteLine($"INF Lookup translations:");
        foreach (var path in source.LookupTranslationPaths)
            Console.WriteLine($"INF   - {path}");
        if (maxRows.HasValue)
            Console.WriteLine($"INF Max rows: {maxRows.Value}");
        if (dryRun)
            Console.WriteLine("INF Mode: dry-run (no POST)");

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

        // Resolve a write target + lookup resolver for either the headless XAF host (in-process)
        // or the OData API. All entities run through the same IVisa2014ImportTarget abstraction.
        IVisa2014ImportTarget target;
        Visa2014ODataLookupResolver resolver;
        Visa2026.DataImporter.ApiClient? api = null;
        Visa2014HeadlessImportSession? session = null;

        var tenantCatalogDir = ResolveTenantCatalogDir(solutionRoot);

        try
        {
            if (inProcess)
            {
                if (dryRun)
                {
                    target = new Visa2014DryRunImportTarget();
                    resolver = new Visa2014ODataLookupResolver();
                }
                else
                {
                    var targetConnection = GetTargetConnection(args);
                    var batchSize = ResolveBatchSize(args);
                    session = await Visa2014HeadlessImportSession.OpenAsync(targetConnection, batchSize);
                    target = session.Target;
                    resolver = session.Resolver;
                }
            }
            else
            {
                api = new Visa2026.DataImporter.ApiClient(apiBaseUrl, userName, password) { Verbose = verbose };
                if (!dryRun)
                {
                    if (!noWait)
                        await api.WaitForServerAsync();
                    await api.LoginAsync();
                }

                target = new Visa2014ODataImportTarget(api);
                resolver = new Visa2014ODataLookupResolver();
                if (!dryRun)
                    await resolver.LoadAsync(api, tenantCatalogDir);
            }

            // Seed cleanup for ApplicationProgress is only available over OData; the headless
            // path imports Applications with SuppressInitialProgress, so there are no seeds.
            var seedCleanupApi = inProcess ? null : api;

            int exitCode;
            if (string.Equals(entity, "Person", StringComparison.OrdinalIgnoreCase))
                exitCode = await RunPersonImportAsync(target, resolver, source, args, idMapPath, maxRows, dryRun, verbose);
            else if (string.Equals(entity, "Passport", StringComparison.OrdinalIgnoreCase))
                exitCode = await RunPassportImportAsync(target, resolver, source, dataImporterRoot, args, idMapPath, maxRows, dryRun, verbose);
            else if (string.Equals(entity, "Visa", StringComparison.OrdinalIgnoreCase))
                exitCode = await RunVisaImportAsync(target, resolver, source, dataImporterRoot, args, idMapPath, maxRows, dryRun, verbose);
            else if (string.Equals(entity, "Education", StringComparison.OrdinalIgnoreCase))
                exitCode = await RunEducationImportAsync(target, resolver, source, dataImporterRoot, args, idMapPath, maxRows, dryRun, verbose);
            else if (string.Equals(entity, "Application", StringComparison.OrdinalIgnoreCase))
                exitCode = await RunApplicationImportAsync(target, resolver, source, args, idMapPath, maxRows, dryRun, verbose);
            else if (string.Equals(entity, "ApplicationProgress", StringComparison.OrdinalIgnoreCase))
                exitCode = await RunApplicationProgressImportAsync(
                    target, resolver, seedCleanupApi, source, dataImporterRoot, args, idMapPath, maxRows, dryRun, verbose, session);
            else if (string.Equals(entity, "AddressOfResidence", StringComparison.OrdinalIgnoreCase))
                exitCode = await RunAddressOfResidenceImportAsync(target, resolver, source, dataImporterRoot, args, idMapPath, maxRows, dryRun, verbose);
            else if (string.Equals(entity, "EmployeeSalary", StringComparison.OrdinalIgnoreCase))
                exitCode = await RunEmployeeSalaryImportAsync(target, source, dataImporterRoot, args, idMapPath, maxRows, dryRun, verbose);
            else if (string.Equals(entity, "ApplicationItem", StringComparison.OrdinalIgnoreCase))
                exitCode = await RunApplicationItemImportAsync(target, resolver, source, dataImporterRoot, args, idMapPath, maxRows, dryRun, verbose);
            else
                exitCode = await RunEmployeePositionHistoryImportAsync(target, resolver, source, dataImporterRoot, args, idMapPath, maxRows, dryRun, verbose);

            return await CompleteSuccessfulImportAsync(
                exitCode, dryRun, dataImporterRoot, args, importSessionStartedUtc, verbose);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR Import failed: {ex.Message}");
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

    private static string ResolveTenantCatalogDir(string? solutionRoot) =>
        Path.Combine(
            solutionRoot ?? Directory.GetCurrentDirectory(),
            "Visa2026.Module",
            "DatabaseUpdate",
            "LookupCatalogs",
            "tenant");

    private static async Task<int> RunPersonImportAsync(
        IVisa2014ImportTarget target,
        Visa2014ODataLookupResolver resolver,
        Visa2014LegacySourceProfile source,
        IReadOnlyList<string> args,
        string idMapPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var result = await Visa2014PersonODataImporter.RunAsync(
            target,
            resolver,
            source.ConnectionString,
            source.LookupTranslationPaths,
            dryRun ? null : idMapPath,
            maxRows,
            dryRun,
            verbose);

        Console.WriteLine($"INF Legacy SQL rows: {result.LegacyRowCount}");
        Console.WriteLine($"INF Prepared: {result.PreparedCount}  Skipped: {result.SkippedCount}  Dedupe merged: {result.DedupeMergedCount}");
        if (!dryRun)
        {
            Console.WriteLine($"INF Posted: {result.PostedCount}  Failed: {result.FailedCount}");
            if (result.IdMapPath != null)
            {
                Console.WriteLine($"INF Id-map: {result.IdMapPath}");
                var targetCs = GetTargetConnection(args);
                var expandCode = await Visa2014PersonIdMapExpander.ExpandAsync(
                    source.ConnectionString,
                    source.LookupTranslationPaths,
                    result.IdMapPath,
                    targetCs,
                    verbose);
                if (expandCode != 0)
                    return expandCode;
            }
        }

        return result.FailedCount > 0 ? 1 : 0;
    }

    private static async Task<int> RunPassportImportAsync(
        IVisa2014ImportTarget target,
        Visa2014ODataLookupResolver resolver,
        Visa2014LegacySourceProfile source,
        string dataImporterRoot,
        IReadOnlyList<string> args,
        string passportIdMapPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var personIdMapPath = GetOptionValue(args, "--person-id-map")
            ?? source.IdMapPath(dataImporterRoot, "Person");

        Console.WriteLine($"INF Person id-map: {personIdMapPath}");

        var result = await Visa2014PassportODataImporter.RunAsync(
            target,
            resolver,
            source.ConnectionString,
            source.LookupTranslationPaths,
            personIdMapPath,
            dryRun ? null : passportIdMapPath,
            maxRows,
            dryRun,
            verbose);

        Console.WriteLine($"INF Legacy SQL rows: {result.LegacyRowCount}");
        Console.WriteLine($"INF Prepared: {result.PreparedCount}  Skipped: {result.SkippedCount}  Dedupe merged: {result.DedupeMergedCount}");
        if (!dryRun)
        {
            Console.WriteLine(
                $"INF Posted: {result.PostedCount}  Failed: {result.FailedCount}  Skipped (no Person map): {result.SkippedNoPersonMap}");
            if (result.IdMapPath != null)
                Console.WriteLine($"INF Id-map: {result.IdMapPath}");
        }
        else if (result.SkippedNoPersonMap > 0)
        {
            Console.WriteLine($"INF Would skip (no Person map): {result.SkippedNoPersonMap}");
        }

        return result.FailedCount > 0 ? 1 : 0;
    }

    private static async Task<int> RunVisaImportAsync(
        IVisa2014ImportTarget target,
        Visa2014ODataLookupResolver resolver,
        Visa2014LegacySourceProfile source,
        string dataImporterRoot,
        IReadOnlyList<string> args,
        string visaIdMapPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var passportIdMapPath = GetOptionValue(args, "--passport-id-map")
            ?? source.IdMapPath(dataImporterRoot, "Passport");

        Console.WriteLine($"INF Passport id-map: {passportIdMapPath}");

        var result = await Visa2014VisaODataImporter.RunAsync(
            target,
            resolver,
            source.ConnectionString,
            source.LookupTranslationPaths,
            passportIdMapPath,
            dryRun ? null : visaIdMapPath,
            maxRows,
            dryRun,
            verbose);

        Console.WriteLine($"INF Legacy SQL rows: {result.LegacyRowCount}");
        Console.WriteLine($"INF Prepared: {result.PreparedCount}  Skipped: {result.SkippedCount}  Dedupe merged: {result.DedupeMergedCount}");
        if (!dryRun)
        {
            Console.WriteLine(
                $"INF Posted: {result.PostedCount}  Failed: {result.FailedCount}  Skipped (no Passport map): {result.SkippedNoPassportMap}  Skipped (already imported): {result.SkippedAlreadyImported}");
            if (result.IdMapPath != null)
                Console.WriteLine($"INF Id-map: {result.IdMapPath}");
        }
        else if (result.SkippedNoPassportMap > 0)
        {
            Console.WriteLine($"INF Would skip (no Passport map): {result.SkippedNoPassportMap}");
        }

        return result.FailedCount > 0 ? 1 : 0;
    }

    private static async Task<int> RunEducationImportAsync(
        IVisa2014ImportTarget target,
        Visa2014ODataLookupResolver resolver,
        Visa2014LegacySourceProfile source,
        string dataImporterRoot,
        IReadOnlyList<string> args,
        string educationIdMapPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var personIdMapPath = GetOptionValue(args, "--person-id-map")
            ?? source.IdMapPath(dataImporterRoot, "Person");

        Console.WriteLine($"INF Person id-map: {personIdMapPath}");

        var result = await Visa2014EducationODataImporter.RunAsync(
            target,
            resolver,
            source.ConnectionString,
            source.LookupTranslationPaths,
            personIdMapPath,
            dryRun ? null : educationIdMapPath,
            maxRows,
            dryRun,
            verbose);

        Console.WriteLine($"INF Legacy SQL rows: {result.LegacyRowCount}");
        Console.WriteLine($"INF Prepared: {result.PreparedCount}  Skipped: {result.SkippedCount}  Dedupe merged: {result.DedupeMergedCount}");
        if (!dryRun)
        {
            Console.WriteLine(
                $"INF Posted: {result.PostedCount}  Failed: {result.FailedCount}  Skipped (no Person map): {result.SkippedNoPersonMap}  Skipped (already imported): {result.SkippedAlreadyImported}");
            if (result.IdMapPath != null)
                Console.WriteLine($"INF Id-map: {result.IdMapPath}");
        }
        else if (result.SkippedNoPersonMap > 0)
        {
            Console.WriteLine($"INF Would skip (no Person map): {result.SkippedNoPersonMap}");
        }

        if (result.FailedCount > 0)
        {
            foreach (var error in result.Errors.Take(10))
                Console.Error.WriteLine($"ERR {error}");
            if (result.Errors.Count > 10)
                Console.Error.WriteLine($"ERR ... and {result.Errors.Count - 10} more");
        }

        return result.FailedCount > 0 ? 1 : 0;
    }

    private static async Task<int> RunApplicationImportAsync(
        IVisa2014ImportTarget target,
        Visa2014ODataLookupResolver resolver,
        Visa2014LegacySourceProfile source,
        IReadOnlyList<string> args,
        string applicationIdMapPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var result = await Visa2014ApplicationODataImporter.RunAsync(
            target,
            resolver,
            source.ConnectionString,
            source.LookupTranslationPaths,
            dryRun ? null : applicationIdMapPath,
            maxRows,
            dryRun,
            verbose);

        Console.WriteLine($"INF Legacy SQL rows: {result.LegacyRowCount}");
        Console.WriteLine($"INF Prepared: {result.PreparedCount}  Skipped: {result.SkippedCount}  Dedupe merged: {result.DedupeMergedCount}");
        if (!dryRun)
        {
            Console.WriteLine(
                $"INF Posted: {result.PostedCount}  Failed: {result.FailedCount}  Skipped (already imported): {result.SkippedAlreadyImported}");
            if (result.IdMapPath != null)
                Console.WriteLine($"INF Id-map: {result.IdMapPath}");
        }

        if (result.FailedCount > 0)
        {
            foreach (var error in result.Errors.Take(10))
                Console.Error.WriteLine($"ERR {error}");
            if (result.Errors.Count > 10)
                Console.Error.WriteLine($"ERR ... and {result.Errors.Count - 10} more");
        }

        return result.FailedCount > 0 ? 1 : 0;
    }

    private static async Task<int> RunApplicationProgressImportAsync(
        IVisa2014ImportTarget target,
        Visa2014ODataLookupResolver resolver,
        Visa2026.DataImporter.ApiClient? seedCleanupApi,
        Visa2014LegacySourceProfile source,
        string dataImporterRoot,
        IReadOnlyList<string> args,
        string progressIdMapPath,
        int? maxRows,
        bool dryRun,
        bool verbose,
        Visa2014HeadlessImportSession? headlessSession = null)
    {
        var applicationIdMapPath = GetOptionValue(args, "--application-id-map")
            ?? source.IdMapPath(dataImporterRoot, "Application");

        Console.WriteLine($"INF Application id-map: {applicationIdMapPath}");

        var result = await Visa2014ApplicationProgressODataImporter.RunAsync(
            target,
            resolver,
            seedCleanupApi,
            source.ConnectionString,
            source.LookupTranslationPaths,
            applicationIdMapPath,
            dryRun ? null : progressIdMapPath,
            maxRows,
            dryRun,
            verbose,
            headlessSession?.ObjectSpaceFactory,
            GetTargetConnection(args));

        Console.WriteLine($"INF Legacy applications: {result.LegacyRowCount}");
        Console.WriteLine($"INF Prepared: {result.PreparedCount}  Parent-skipped: {result.SkippedCount}");
        if (!dryRun)
        {
            Console.WriteLine(
                $"INF Seeds removed: {result.SeedsRemovedBeforeImport}  Posted: {result.PostedCount}  Failed: {result.FailedCount}  Skipped (no Application map): {result.SkippedNoApplicationMap}  Skipped (already imported): {result.SkippedAlreadyImported}");
            if (result.IdMapPath != null)
                Console.WriteLine($"INF Id-map: {result.IdMapPath}");
        }
        else if (result.SkippedNoApplicationMap > 0)
        {
            Console.WriteLine($"INF Would skip (no Application map): {result.SkippedNoApplicationMap}");
        }

        if (result.FailedCount > 0)
        {
            foreach (var error in result.Errors.Take(10))
                Console.Error.WriteLine($"ERR {error}");
            if (result.Errors.Count > 10)
                Console.Error.WriteLine($"ERR ... and {result.Errors.Count - 10} more");
        }

        return result.FailedCount > 0 ? 1 : 0;
    }

    private static async Task<int> RunAddressOfResidenceImportAsync(
        IVisa2014ImportTarget target,
        Visa2014ODataLookupResolver resolver,
        Visa2014LegacySourceProfile source,
        string dataImporterRoot,
        IReadOnlyList<string> args,
        string addressIdMapPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var personIdMapPath = GetOptionValue(args, "--person-id-map")
            ?? source.IdMapPath(dataImporterRoot, "Person");

        Console.WriteLine($"INF Person id-map: {personIdMapPath}");

        var result = await Visa2014AddressOfResidenceODataImporter.RunAsync(
            target,
            resolver,
            source.ConnectionString,
            source.LookupTranslationPaths,
            personIdMapPath,
            dryRun ? null : addressIdMapPath,
            maxRows,
            dryRun,
            verbose);

        Console.WriteLine($"INF Legacy SQL rows: {result.LegacyRowCount}");
        Console.WriteLine($"INF Prepared: {result.PreparedCount}  Skipped: {result.SkippedCount}  Dedupe merged: {result.DedupeMergedCount}");
        if (!dryRun)
        {
            Console.WriteLine(
                $"INF Posted: {result.PostedCount}  Failed: {result.FailedCount}  Skipped (no Person map): {result.SkippedNoPersonMap}  Skipped (already imported): {result.SkippedAlreadyImported}");
            if (result.IdMapPath != null)
                Console.WriteLine($"INF Id-map: {result.IdMapPath}");
        }
        else if (result.SkippedNoPersonMap > 0)
        {
            Console.WriteLine($"INF Would skip (no Person map): {result.SkippedNoPersonMap}");
        }

        if (result.FailedCount > 0)
        {
            foreach (var error in result.Errors.Take(10))
                Console.Error.WriteLine($"ERR {error}");
            if (result.Errors.Count > 10)
                Console.Error.WriteLine($"ERR ... and {result.Errors.Count - 10} more");
        }

        return result.FailedCount > 0 ? 1 : 0;
    }

    private static async Task<int> RunApplicationItemImportAsync(
        IVisa2014ImportTarget target,
        Visa2014ODataLookupResolver resolver,
        Visa2014LegacySourceProfile source,
        string dataImporterRoot,
        IReadOnlyList<string> args,
        string applicationItemIdMapPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var applicationIdMapPath = GetOptionValue(args, "--application-id-map")
            ?? source.IdMapPath(dataImporterRoot, "Application");
        var personIdMapPath = GetOptionValue(args, "--person-id-map")
            ?? source.IdMapPath(dataImporterRoot, "Person");
        var passportIdMapPath = GetOptionValue(args, "--passport-id-map")
            ?? source.IdMapPath(dataImporterRoot, "Passport");
        var visaIdMapPath = GetOptionValue(args, "--visa-id-map")
            ?? source.IdMapPath(dataImporterRoot, "Visa");
        var positionHistoryIdMapPath = GetOptionValue(args, "--position-history-id-map")
            ?? source.IdMapPath(dataImporterRoot, "EmployeePositionHistory");
        var addressIdMapPath = GetOptionValue(args, "--address-id-map")
            ?? source.IdMapPath(dataImporterRoot, "AddressOfResidence");
        var workPermitItemIdMapPath = GetOptionValue(args, "--work-permit-item-id-map")
            ?? source.IdMapPath(dataImporterRoot, "WorkPermitItem");

        Console.WriteLine($"INF Application id-map: {applicationIdMapPath}");
        Console.WriteLine($"INF Person id-map: {personIdMapPath}");
        Console.WriteLine($"INF Passport id-map: {passportIdMapPath}");
        Console.WriteLine($"INF Visa id-map: {visaIdMapPath}");
        Console.WriteLine($"INF EmployeePositionHistory id-map: {positionHistoryIdMapPath}");
        Console.WriteLine($"INF AddressOfResidence id-map: {addressIdMapPath}");
        Console.WriteLine($"INF WorkPermitItem id-map: {workPermitItemIdMapPath}");

        var result = await Visa2014ApplicationItemODataImporter.RunAsync(
            target,
            resolver,
            source.ConnectionString,
            source.LookupTranslationPaths,
            applicationIdMapPath,
            personIdMapPath,
            passportIdMapPath,
            visaIdMapPath,
            positionHistoryIdMapPath,
            addressIdMapPath,
            workPermitItemIdMapPath,
            applicationItemIdMapPath,
            maxRows,
            dryRun,
            verbose);

        Console.WriteLine($"INF Legacy SQL rows: {result.LegacyRowCount}");
        Console.WriteLine($"INF Prepared: {result.PreparedCount}  Skipped: {result.SkippedCount}  Dedupe merged: {result.DedupeMergedCount}");
        if (!dryRun)
        {
            Console.WriteLine(
                $"INF Posted: {result.PostedCount}  Failed: {result.FailedCount}  Skipped (missing required id-map): {result.SkippedMissingRequiredIdMap}  Skipped (already imported): {result.SkippedAlreadyImported}");
            if (result.IdMapPath != null)
                Console.WriteLine($"INF Id-map: {result.IdMapPath}");
        }

        if (result.FailedCount > 0)
        {
            foreach (var error in result.Errors.Take(10))
                Console.Error.WriteLine($"ERR {error}");
            if (result.Errors.Count > 10)
                Console.Error.WriteLine($"ERR ... and {result.Errors.Count - 10} more");
        }

        return result.FailedCount > 0 ? 1 : 0;
    }

    private static async Task<int> RunEmployeePositionHistoryImportAsync(
        IVisa2014ImportTarget target,
        Visa2014ODataLookupResolver resolver,
        Visa2014LegacySourceProfile source,
        string dataImporterRoot,
        IReadOnlyList<string> args,
        string historyIdMapPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var personIdMapPath = GetOptionValue(args, "--person-id-map")
            ?? source.IdMapPath(dataImporterRoot, "Person");

        Console.WriteLine($"INF Person id-map: {personIdMapPath}");

        var result = await Visa2014EmployeePositionHistoryODataImporter.RunAsync(
            target,
            resolver,
            source.ConnectionString,
            source.LookupTranslationPaths,
            personIdMapPath,
            dryRun ? null : historyIdMapPath,
            maxRows,
            dryRun,
            verbose);

        Console.WriteLine($"INF Legacy SQL rows: {result.LegacyRowCount}");
        Console.WriteLine($"INF Prepared: {result.PreparedCount}  Skipped: {result.SkippedCount}  Dedupe merged: {result.DedupeMergedCount}");
        if (!dryRun)
        {
            Console.WriteLine(
                $"INF Posted: {result.PostedCount}  Failed: {result.FailedCount}  Skipped (no Person map): {result.SkippedNoPersonMap}  Skipped (already imported): {result.SkippedAlreadyImported}  ActualPositions created: {result.ActualPositionsCreated}");
            if (result.IdMapPath != null)
                Console.WriteLine($"INF Id-map: {result.IdMapPath}");
        }
        else if (result.SkippedNoPersonMap > 0)
        {
            Console.WriteLine($"INF Would skip (no Person map): {result.SkippedNoPersonMap}");
        }

        if (result.FailedCount > 0)
        {
            foreach (var error in result.Errors.Take(10))
                Console.Error.WriteLine($"ERR {error}");
            if (result.Errors.Count > 10)
                Console.Error.WriteLine($"ERR ... and {result.Errors.Count - 10} more");
        }

        return result.FailedCount > 0 ? 1 : 0;
    }

    private static async Task<int> RunEmployeeSalaryImportAsync(
        IVisa2014ImportTarget target,
        Visa2014LegacySourceProfile source,
        string dataImporterRoot,
        IReadOnlyList<string> args,
        string salaryIdMapPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var personIdMapPath = GetOptionValue(args, "--person-id-map")
            ?? source.IdMapPath(dataImporterRoot, "Person");

        Console.WriteLine($"INF Person id-map: {personIdMapPath}");

        var result = await Visa2014EmployeeSalaryODataImporter.RunAsync(
            target,
            source.ConnectionString,
            source.LookupTranslationPaths,
            personIdMapPath,
            dryRun ? null : salaryIdMapPath,
            maxRows,
            dryRun,
            verbose);

        Console.WriteLine($"INF Legacy SQL rows: {result.LegacyRowCount}");
        Console.WriteLine($"INF Prepared: {result.PreparedCount}  Skipped: {result.SkippedCount}  Dedupe merged: {result.DedupeMergedCount}");
        if (!dryRun)
        {
            Console.WriteLine(
                $"INF Posted: {result.PostedCount}  Failed: {result.FailedCount}  Skipped (no Person map): {result.SkippedNoPersonMap}  Skipped (already imported): {result.SkippedAlreadyImported}");
            if (result.IdMapPath != null)
                Console.WriteLine($"INF Id-map: {result.IdMapPath}");
        }
        else if (result.SkippedNoPersonMap > 0)
        {
            Console.WriteLine($"INF Would skip (no Person map): {result.SkippedNoPersonMap}");
        }

        if (result.FailedCount > 0)
        {
            foreach (var error in result.Errors.Take(10))
                Console.Error.WriteLine($"ERR {error}");
            if (result.Errors.Count > 10)
                Console.Error.WriteLine($"ERR ... and {result.Errors.Count - 10} more");
        }

        return result.FailedCount > 0 ? 1 : 0;
    }

    private static async Task<int> CompleteSuccessfulImportAsync(
        int exitCode,
        bool dryRun,
        string dataImporterRoot,
        IReadOnlyList<string> args,
        DateTime sessionStartedUtc,
        bool verbose)
    {
        if (exitCode != 0 || dryRun || HasArg(args, "--skip-import-log-cleanup"))
            return exitCode;

        await Visa2014ImportTrackingLogCleanup.ClearAfterSuccessfulImportAsync(
            dataImporterRoot,
            ResolveTargetConnectionForCleanup(args),
            sessionStartedUtc,
            verbose);
        return exitCode;
    }

    /// <summary>Target SQL for post-import log cleanup — explicit only (no LocalDB default).</summary>
    private static string? ResolveTargetConnectionForCleanup(IReadOnlyList<string> args) =>
        GetOptionValue(args, "--target-connection")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? Environment.GetEnvironmentVariable("VISA2026_SQL_CONNECTION");

    private static string GetTargetConnection(IReadOnlyList<string> args) =>
        ResolveTargetConnectionForCleanup(args)
        ?? "Server=(localdb)\\mssqllocaldb;Database=Visa2026;Trusted_Connection=True;TrustServerCertificate=True";

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

    public static async Task<int> RunExpandIdMapAsync(IReadOnlyList<string> args, bool verbose)
    {
        var entity = GetOptionValue(args, "--entity");
        if (!string.Equals(entity, "Person", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("ERR --expand-visa2014-id-map requires --entity Person.");
            return 1;
        }

        var dataImporterRoot = Visa2014ContentRoot.FindDataImporterRoot();
        if (dataImporterRoot == null)
        {
            Console.Error.WriteLine("ERR Could not locate Visa2026.DataImporter content root.");
            return 1;
        }
        var solutionRoot = Visa2014ContentRoot.FindSolutionRoot();
        var source = Visa2014LegacySource.Resolve(dataImporterRoot, solutionRoot, args);
        var idMapPath = GetOptionValue(args, "--id-map-output")
            ?? source.IdMapPath(dataImporterRoot, entity);
        var targetCs = GetTargetConnection(args);

        return await Visa2014PersonIdMapExpander.ExpandAsync(
            source.ConnectionString,
            source.LookupTranslationPaths,
            idMapPath,
            targetCs,
            verbose);
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
