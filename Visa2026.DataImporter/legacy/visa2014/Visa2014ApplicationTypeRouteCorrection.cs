using System.Text.Json;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using Visa2026.Blazor.Server.Services.Migration;
using Bo = Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014ApplicationTypeRouteCorrectionResult
{
    public int EmployeeRetyped { get; init; }
    public int EmployeeItemsBackfilled { get; init; }
    public int FamilyApplicationsCorrected { get; init; }
    public int FamilyProgressDeleted { get; init; }
    public int FamilyProgressPosted { get; init; }
    public int FamilyProgressFailed { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

internal static class Visa2014ApplicationTypeRouteCorrection
{
    private const string EmployeeVisaExtName = "App_Visa_Ext";
    private const string EmployeeVisaAndWpExtName = "App_Visa_and_WP_Ext";
    private const string FamilyVisaExtName = "App_Visa_Ext_FM";
    private const string FamilyNewbornName = "App_Visa_For_New_Born_FM";

    public static async Task<int> RunCommandAsync(IReadOnlyList<string> args, bool verbose)
    {
        var dataImporterRoot = Visa2014ContentRoot.FindDataImporterRoot();
        if (dataImporterRoot == null)
        {
            Console.Error.WriteLine("ERR Could not locate Visa2026.DataImporter content root.");
            return 1;
        }

        var solutionRoot = Visa2014ContentRoot.FindSolutionRoot();
        Visa2014LegacySourceProfile source;
        try { source = Visa2014LegacySource.Resolve(dataImporterRoot, solutionRoot, args); }
        catch (Exception ex) { Console.Error.WriteLine($"ERR {ex.Message}"); return 1; }

        var dryRun = HasArg(args, "--dry-run");
        var targetConnection = GetOptionValue(args, "--target-connection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=visa2026;Username=postgres;Password=Visa2026Local;Persist Security Info=True;EFCoreProvider=Postgres";

        Console.WriteLine("=== VISA2014 Application type route correction");
        Console.WriteLine($"INF Legacy source: {source.Id}");
        Console.WriteLine($"INF Target SQL: {MaskConnectionString(targetConnection)}");
        if (dryRun) Console.WriteLine("INF Mode: dry-run (no writes)");

        HeadlessMigrationHost? host = null;
        IDisposable? importScope = null;
        try
        {
            host = HeadlessMigrationHost.Start(targetConnection);
            importScope = MigrationImportContext.BeginDataImportScope();

            var resolver = new Visa2014ODataLookupResolver();
            using (var lookupSpace = host.ObjectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.Person)))
            {
                MigrationImportContext.ApplyImportObjectSpaceHooks(lookupSpace);
                resolver.LoadFromObjectSpace(lookupSpace, Visa2014HeadlessImportSession.ResolveTenantCatalogDirStatic());
            }

            var target = new Visa2014ObjectSpaceImportTarget(host.ObjectSpaceFactory, batchSize: 50);
            var applicationIdMap = File.Exists(source.IdMapPath(dataImporterRoot, "Application"))
                ? Visa2014IdMapHelper.Load(source.IdMapPath(dataImporterRoot, "Application"))
                : new Dictionary<Guid, Guid>();

            var result = await RunAsync(host.ObjectSpaceFactory, target, resolver, source.ConnectionString,
                source.LookupTranslationPaths, applicationIdMap,
                source.IdMapPath(dataImporterRoot, "ApplicationProgress"), dryRun, verbose);

            Console.WriteLine($"INF Employee retyped: {result.EmployeeRetyped}");
            Console.WriteLine($"INF Employee items backfilled: {result.EmployeeItemsBackfilled}");
            Console.WriteLine($"INF Family apps corrected: {result.FamilyApplicationsCorrected}");
            Console.WriteLine($"INF Family progress deleted: {result.FamilyProgressDeleted}");
            Console.WriteLine($"INF Family progress posted: {result.FamilyProgressPosted}");
            Console.WriteLine($"INF Family progress failed: {result.FamilyProgressFailed}");
            foreach (var error in result.Errors.Take(20))
            {
                var prefix = error.Contains("no sponsor ProjectContract", StringComparison.OrdinalIgnoreCase) ? "WRN" : "ERR";
                if (prefix == "ERR") Console.Error.WriteLine($"ERR {error}");
                else if (verbose) Console.Error.WriteLine($"WRN {error}");
            }

            var fatalErrors = result.Errors.Count(e =>
                !e.Contains("no sponsor ProjectContract", StringComparison.OrdinalIgnoreCase));
            return fatalErrors > 0 || result.FamilyProgressFailed > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR Correction failed: {ex.Message}");
            if (verbose) Console.Error.WriteLine(ex);
            return 1;
        }
        finally { importScope?.Dispose(); host?.Dispose(); }
    }

    internal static async Task<Visa2014ApplicationTypeRouteCorrectionResult> RunAsync(
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        IVisa2014ImportTarget target,
        Visa2014ODataLookupResolver resolver,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap,
        string? progressIdMapPath,
        bool dryRun,
        bool verbose)
    {
        var errors = new List<string>();
        int employeeRetyped = 0, itemsBackfilled = 0, familyCorrected = 0;
        int progressDeleted = 0, progressPosted = 0, progressFailed = 0;

        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.Application));
        MigrationImportContext.ApplyImportObjectSpaceHooks(objectSpace);

        var visaExtType = objectSpace.GetObjectsQuery<Bo.ApplicationType>().FirstOrDefault(t => t.Name == EmployeeVisaExtName);
        var visaAndWpType = objectSpace.GetObjectsQuery<Bo.ApplicationType>().FirstOrDefault(t => t.Name == EmployeeVisaAndWpExtName);
        var familyVisaExtType = objectSpace.GetObjectsQuery<Bo.ApplicationType>().FirstOrDefault(t => t.Name == FamilyVisaExtName);
        var familyNewbornType = objectSpace.GetObjectsQuery<Bo.ApplicationType>().FirstOrDefault(t => t.Name == FamilyNewbornName);

        if (visaExtType == null || visaAndWpType == null)
            throw new InvalidOperationException("ApplicationType seed missing -- run Update-LocalDatabase.ps1 -ForceUpdate first.");

        if (familyVisaExtType?.ApplicationProgressRoute != Bo.ApplicationProgressRouteKind.ViaMinistries
            || familyNewbornType?.ApplicationProgressRoute != Bo.ApplicationProgressRouteKind.ViaMinistries)
            throw new InvalidOperationException("FM types not ViaMinistries -- sync ApplicationTypeConfigurationCatalog first.");

        var employeeApps = objectSpace.GetObjectsQuery<Bo.Application>()
            .Where(a => a.ApplicationType != null && a.ApplicationType.ID == visaExtType.ID).ToList();
        if (verbose) Console.WriteLine($"INF Phase 1: {employeeApps.Count} App_Visa_Ext application(s)");

        foreach (var application in employeeApps)
        {
            if (!dryRun) application.ApplicationType = visaAndWpType;
            foreach (var item in application.ApplicationItems ?? [])
            {
                if (item.Person == null || !item.Person.IsEmployee) continue;
                if (!dryRun) BackfillEmployeeVisaAndWpItem(item);
                itemsBackfilled++;
            }
            employeeRetyped++;
        }
        if (!dryRun && employeeApps.Count > 0) objectSpace.CommitChanges();

        var familyTypeIds = new HashSet<Guid>();
        if (familyVisaExtType != null) familyTypeIds.Add(familyVisaExtType.ID);
        if (familyNewbornType != null) familyTypeIds.Add(familyNewbornType.ID);

        var familyApps = objectSpace.GetObjectsQuery<Bo.Application>()
            .Where(a => a.ApplicationType != null && familyTypeIds.Contains(a.ApplicationType.ID)).ToList();
        if (verbose) Console.WriteLine($"INF Phase 2: {familyApps.Count} family visa application(s)");

        var familyAppIds = familyApps.Select(a => a.ID).ToHashSet();
        if (familyAppIds.Count > 0)
        {
            using var progressSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.ApplicationProgress));
            MigrationImportContext.ApplyImportObjectSpaceHooks(progressSpace);
            var progresses = progressSpace.GetObjectsQuery<Bo.ApplicationProgress>()
                .Where(p => p.Application != null && familyAppIds.Contains(p.Application.ID)).ToList();
            progressDeleted = progresses.Count;
            if (!dryRun)
            {
                foreach (var progress in progresses)
                    progressSpace.Delete(progress);
                progressSpace.CommitChanges();
            }
        }

        foreach (var applicationId in familyAppIds)
        {
            using var familySpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.Application));
            MigrationImportContext.ApplyImportObjectSpaceHooks(familySpace);
            var application = familySpace.GetObjectByKey<Bo.Application>(applicationId);
            if (application == null)
            {
                errors.Add($"Application {applicationId}: not found");
                continue;
            }

            var sponsor = ResolveSponsoringEmployee(application);
            var contract = sponsor?.ProjectContract;
            if (contract == null) { errors.Add($"Application {application.ID}: no sponsor ProjectContract"); continue; }
            var profile = ResolveApprovalLegProfileFromSponsorContract(familySpace, contract);
            if (profile == null) { errors.Add($"Application {application.ID}: no ApprovalLegProfile for contract {contract.Code}"); continue; }
            if (!dryRun)
            {
                application.ProjectContract = familySpace.GetObject(contract);
                application.ApprovalLegProfile = familySpace.GetObject(profile);
                familySpace.CommitChanges();
            }
            familyCorrected++;
        }

        if (familyAppIds.Count > 0)
        {
            var legacyToTarget = applicationIdMap.Where(kv => familyAppIds.Contains(kv.Value))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
            var targetLegCounts = Visa2014ApplicationMinistryLegCountResolver.LoadFromObjectSpace(objectSpaceFactory);
            var familyLegCounts = Visa2014ApplicationMinistryLegCountResolver.MapLegacyLegCounts(legacyToTarget, targetLegCounts);
            var regen = await Visa2014ApplicationProgressODataImporter.RegenerateForLegacyApplicationsAsync(
                target, resolver, legacyConnectionString, lookupTranslationPaths, legacyToTarget, familyLegCounts, dryRun, verbose);
            progressPosted = regen.PostedCount;
            progressFailed = regen.FailedCount;
            errors.AddRange(regen.Errors);
            if (!dryRun && regen.ProgressIdMapUpdates.Count > 0 && !string.IsNullOrWhiteSpace(progressIdMapPath))
                MergeProgressIdMap(progressIdMapPath, regen.ProgressIdMapUpdates);
        }

        return new Visa2014ApplicationTypeRouteCorrectionResult
        {
            EmployeeRetyped = employeeRetyped, EmployeeItemsBackfilled = itemsBackfilled,
            FamilyApplicationsCorrected = familyCorrected, FamilyProgressDeleted = progressDeleted,
            FamilyProgressPosted = progressPosted, FamilyProgressFailed = progressFailed, Errors = errors,
        };
    }

    private static void BackfillEmployeeVisaAndWpItem(Bo.ApplicationItem item)
    {
        var person = item.Person!;
        item.CurrentPositionHistory = Bo.PersonCurrentItems.GetCurrentPositionHistory(person);
        item.CurrentSalary = Bo.PersonCurrentItems.GetCurrentSalary(person);
        item.CurrentWorkDuty = Bo.PersonCurrentItems.GetCurrentWorkDuty(person);
        var workPermitItem = Bo.PersonCurrentItems.GetCurrentWorkPermitItem(person);
        item.CurrentWorkPermitItem = workPermitItem;
        item.WorkPermittedLocations = workPermitItem?.WorkPermittedLocations ?? string.Empty;
        if (item.VisaIssued) { item.VisaIsChanged = true; if (workPermitItem != null) item.WorkPermitItemIsIssued = true; }
    }

    private static Bo.Person? ResolveSponsoringEmployee(Bo.Application application)
    {
        foreach (var item in application.ApplicationItems ?? [])
        {
            var person = item.Person;
            if (person == null) continue;
            if (!person.IsEmployee && person.SponsoringEmployee != null) return person.SponsoringEmployee;
            if (person.IsEmployee) return person;
        }
        return null;
    }

    private static Bo.ApprovalLegProfile? ResolveApprovalLegProfileFromSponsorContract(IObjectSpace objectSpace, Bo.ProjectContract contract)
    {
        var allowed = objectSpace.GetObjectsQuery<Bo.ProjectContractApprovalLegProfile>()
            .Where(x => x.ProjectContract != null && x.ProjectContract.ID == contract.ID)
            .Select(x => x.ApprovalLegProfile).Where(p => p != null && p.IsActive).ToList();
        if (allowed.Count == 1) return allowed[0];
        var dominantProfileId = objectSpace.GetObjectsQuery<Bo.Application>()
            .Where(a => a.ProjectContract != null && a.ProjectContract.ID == contract.ID
                && a.ApprovalLegProfile != null && a.ApplicationType != null
                && a.ApplicationType.Category == Bo.ApplicationTypeCategory.Employee)
            .GroupBy(a => a.ApprovalLegProfile!.ID).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault();
        if (dominantProfileId != Guid.Empty)
            return objectSpace.GetObjectByKey<Bo.ApprovalLegProfile>(dominantProfileId);
        return allowed.FirstOrDefault();
    }

    private static void MergeProgressIdMap(string path, IReadOnlyDictionary<string, Guid> updates)
    {
        var existing = File.Exists(path)
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in updates) existing[key] = value.ToString();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(existing, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return "(empty)";
        return string.Join("; ", connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => !p.StartsWith("Password=", StringComparison.OrdinalIgnoreCase) && !p.StartsWith("Pwd=", StringComparison.OrdinalIgnoreCase)));
    }

    private static bool HasArg(IReadOnlyList<string> args, string flag) =>
        args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

    private static string? GetOptionValue(IReadOnlyList<string> args, string optionName)
    {
        for (var i = 0; i < args.Count - 1; i++)
            if (string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
        return null;
    }
}