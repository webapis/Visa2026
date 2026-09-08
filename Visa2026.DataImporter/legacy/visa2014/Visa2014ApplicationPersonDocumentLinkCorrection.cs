using DevExpress.ExpressApp;
using Visa2026.Blazor.Server.Services.Migration;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.MigrationImport;
using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014ApplicationPersonDocumentLinkCorrectionResult
{
    public int LegacyRowsInScope { get; init; }
    public int PassportChanged { get; init; }
    public int VisaChanged { get; init; }
    public int WorkPermitItemChanged { get; init; }
    public int AlreadyCorrect { get; init; }
    public int SkippedMissingParentIdMap { get; init; }
    public int SkippedNoSnapshot { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// Re-pins Passport/Visa/WorkPermitItem ResolvedLinks from PersonInApplication
/// (id-map), replacing latest-N / "today" leftovers.
/// </summary>
internal static class Visa2014ApplicationPersonDocumentLinkCorrection
{
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

        Console.WriteLine("=== VISA2014 ApplicationProfileInstancePerson document-link correction");
        Console.WriteLine($"INF Legacy source: {source.Id}");
        Console.WriteLine($"INF Target SQL: {MaskConnectionString(targetConnection)}");
        if (dryRun) Console.WriteLine("INF Mode: dry-run (no writes)");

        try
        {
            Visa2014LegacySqlGuard.EnsureLegacyReadCredentials(source.ConnectionString);
            await Visa2014LegacySqlGuard.EnsureLegacyConnectionAsync(source.ConnectionString);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR Legacy SQL: {ex.Message}");
            return 1;
        }

        HeadlessMigrationHost? host = null;
        IDisposable? importScope = null;
        try
        {
            host = HeadlessMigrationHost.Start(targetConnection);
            importScope = MigrationImportContext.BeginDataImportScope();

            var applicationIdMap = LoadMap(
                GetOptionValue(args, "--application-id-map")
                ?? source.IdMapPath(dataImporterRoot, "ApplicationProfileInstance"));
            var personIdMap = LoadMap(
                GetOptionValue(args, "--person-id-map")
                ?? source.IdMapPath(dataImporterRoot, "Person"));
            var passportIdMap = LoadMap(
                GetOptionValue(args, "--passport-id-map")
                ?? source.IdMapPath(dataImporterRoot, "Passport"));
            var visaIdMap = LoadMap(
                GetOptionValue(args, "--visa-id-map")
                ?? source.IdMapPath(dataImporterRoot, "Visa"));
            var workPermitItemIdMap = LoadMap(
                GetOptionValue(args, "--workpermititem-id-map")
                ?? source.IdMapPath(dataImporterRoot, "WorkPermitItem"));

            Console.WriteLine($"INF ApplicationProfileInstance id-map: {applicationIdMap.Count}");
            Console.WriteLine($"INF Person id-map: {personIdMap.Count}");
            Console.WriteLine($"INF Passport id-map: {passportIdMap.Count}");
            Console.WriteLine($"INF Visa id-map: {visaIdMap.Count}");
            Console.WriteLine($"INF WorkPermitItem id-map: {workPermitItemIdMap.Count}");

            var result = Run(
                host.ObjectSpaceFactory,
                source.ConnectionString,
                applicationIdMap,
                personIdMap,
                passportIdMap,
                visaIdMap,
                workPermitItemIdMap,
                dryRun,
                verbose);

            Console.WriteLine($"INF Legacy PIA rows: {result.LegacyRowsInScope}");
            Console.WriteLine($"INF Passport links changed: {result.PassportChanged}");
            Console.WriteLine($"INF Visa links changed: {result.VisaChanged}");
            Console.WriteLine($"INF WorkPermitItem links changed: {result.WorkPermitItemChanged}");
            Console.WriteLine($"INF Already correct: {result.AlreadyCorrect}");
            Console.WriteLine($"INF Skipped missing parent id-map: {result.SkippedMissingParentIdMap}");
            Console.WriteLine($"INF Skipped (no mapped snapshot): {result.SkippedNoSnapshot}");
            foreach (var error in result.Errors.Take(40))
                Console.Error.WriteLine($"ERR {error}");
            if (result.Errors.Count > 40)
                Console.Error.WriteLine($"ERR ... and {result.Errors.Count - 40} more");

            return result.Errors.Count > 0 ? 1 : 0;
        }
        finally
        {
            importScope?.Dispose();
            host?.Dispose();
        }
    }

    internal static Visa2014ApplicationPersonDocumentLinkCorrectionResult Run(
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        string legacyConnectionString,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap,
        IReadOnlyDictionary<Guid, Guid> personIdMap,
        IReadOnlyDictionary<Guid, Guid> passportIdMap,
        IReadOnlyDictionary<Guid, Guid> visaIdMap,
        IReadOnlyDictionary<Guid, Guid> workPermitItemIdMap,
        bool dryRun,
        bool verbose)
    {
        var rawRows = Visa2014ApplicationProfileInstancePersonTransform.LoadRawRows(
            legacyConnectionString, maxRows: null, verbose);
        var errors = new List<string>();
        var passportChanged = 0;
        var visaChanged = 0;
        var workPermitItemChanged = 0;
        var alreadyCorrect = 0;
        var skippedMissingParent = 0;
        var skippedNoSnapshot = 0;

        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.ApplicationProfileInstance));
        MigrationImportContext.ApplyImportObjectSpaceHooks(objectSpace);

        var pending = 0;
        foreach (var raw in rawRows)
        {
            var personOid = Visa2014ApplicationProfileInstancePersonTransform.ResolvePersonOid(raw);
            if (personOid is not Guid legacyPerson || legacyPerson == Guid.Empty)
            {
                skippedNoSnapshot++;
                continue;
            }

            if (!applicationIdMap.TryGetValue(raw.LegacyApplicationProfileInstanceOid, out var applicationId)
                || !personIdMap.TryGetValue(legacyPerson, out var personId))
            {
                skippedMissingParent++;
                continue;
            }

            var passportIds = Visa2014ApplicationPersonDocumentLinks.MapLegacyOids(
                passportIdMap, raw.LegacyPreviousPassportOid, raw.LegacyPassportOid);
            var visaIds = Visa2014ApplicationPersonDocumentLinks.MapLegacyOids(
                visaIdMap, raw.LegacyVisaOid);
            var workPermitItemIds = Visa2014ApplicationPersonDocumentLinks.MapLegacyOids(
                workPermitItemIdMap, raw.LegacyWorkPermitOid);
            if (passportIds.Count == 0 && visaIds.Count == 0 && workPermitItemIds.Count == 0)
            {
                skippedNoSnapshot++;
                continue;
            }

            var application = objectSpace.GetObjectByKey<Bo.ApplicationProfileInstance>(applicationId);
            var person = objectSpace.GetObjectByKey<Bo.Person>(personId);
            if (application == null || person == null)
            {
                skippedMissingParent++;
                continue;
            }

            var existing = ApplicationProfileInstancePersonResolver.LoadLinks(objectSpace, application.ID, person.ID);
            var existingPassports = existing
                .Where(l => l.LinkKind == ApplicationProfileInstancePersonLinkKind.Passport)
                .Select(l => l.LinkedObjectId)
                .Where(id => id is Guid g && g != Guid.Empty)
                .Select(id => id!.Value)
                .ToList();
            var existingVisas = existing
                .Where(l => l.LinkKind == ApplicationProfileInstancePersonLinkKind.Visa)
                .Select(l => l.LinkedObjectId)
                .Where(id => id is Guid g && g != Guid.Empty)
                .Select(id => id!.Value)
                .ToList();
            var existingWorkPermits = existing
                .Where(l => l.LinkKind == ApplicationProfileInstancePersonLinkKind.WorkPermitItem)
                .Select(l => l.LinkedObjectId)
                .Where(id => id is Guid g && g != Guid.Empty)
                .Select(id => id!.Value)
                .ToList();

            Visa2014ApplicationPersonDocumentLinks.Diff(existingPassports, passportIds, out var removeP, out var addP);
            Visa2014ApplicationPersonDocumentLinks.Diff(existingVisas, visaIds, out var removeV, out var addV);
            Visa2014ApplicationPersonDocumentLinks.Diff(existingWorkPermits, workPermitItemIds, out var removeW, out var addW);
            if (removeP.Count == 0 && addP.Count == 0 && removeV.Count == 0 && addV.Count == 0
                && removeW.Count == 0 && addW.Count == 0)
            {
                alreadyCorrect++;
                continue;
            }

            if (verbose)
            {
                Console.WriteLine(
                    $"INF app {application.FullApplicationNumber} person {personId}: " +
                    $"passport -{removeP.Count}/+{addP.Count} visa -{removeV.Count}/+{addV.Count} " +
                    $"wp -{removeW.Count}/+{addW.Count}");
            }

            if (!dryRun)
            {
                if (passportIds.Count > 0)
                    passportChanged += Visa2014ApplicationPersonDocumentLinks.ReplaceKind(
                        objectSpace, application, person,
                        ApplicationProfileInstancePersonLinkKind.Passport, passportIds);
                if (visaIds.Count > 0)
                    visaChanged += Visa2014ApplicationPersonDocumentLinks.ReplaceKind(
                        objectSpace, application, person,
                        ApplicationProfileInstancePersonLinkKind.Visa, visaIds);
                if (workPermitItemIds.Count > 0)
                    workPermitItemChanged += Visa2014ApplicationPersonDocumentLinks.ReplaceKind(
                        objectSpace, application, person,
                        ApplicationProfileInstancePersonLinkKind.WorkPermitItem, workPermitItemIds);
                pending++;
                if (pending >= 50)
                {
                    objectSpace.CommitChanges();
                    pending = 0;
                }
            }
            else
            {
                if (passportIds.Count > 0 && (removeP.Count > 0 || addP.Count > 0))
                    passportChanged++;
                if (visaIds.Count > 0 && (removeV.Count > 0 || addV.Count > 0))
                    visaChanged++;
                if (workPermitItemIds.Count > 0 && (removeW.Count > 0 || addW.Count > 0))
                    workPermitItemChanged++;
            }
        }

        if (!dryRun && pending > 0)
            objectSpace.CommitChanges();

        return new Visa2014ApplicationPersonDocumentLinkCorrectionResult
        {
            LegacyRowsInScope = rawRows.Count,
            PassportChanged = passportChanged,
            VisaChanged = visaChanged,
            WorkPermitItemChanged = workPermitItemChanged,
            AlreadyCorrect = alreadyCorrect,
            SkippedMissingParentIdMap = skippedMissingParent,
            SkippedNoSnapshot = skippedNoSnapshot,
            Errors = errors,
        };
    }

    private static Dictionary<Guid, Guid> LoadMap(string path) =>
        File.Exists(path) ? Visa2014IdMapHelper.Load(path) : new Dictionary<Guid, Guid>();

    private static bool HasArg(IReadOnlyList<string> args, string flag) =>
        args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

    private static string? GetOptionValue(IReadOnlyList<string> args, string optionName)
    {
        for (var i = 0; i < args.Count - 1; i++)
        {
            if (string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return null;
    }

    private static string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return "(empty)";
        return string.Join("; ", connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => !p.StartsWith("Password=", StringComparison.OrdinalIgnoreCase)
                && !p.StartsWith("Pwd=", StringComparison.OrdinalIgnoreCase)));
    }
}
