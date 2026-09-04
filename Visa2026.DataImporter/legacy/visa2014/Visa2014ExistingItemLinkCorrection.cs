using DevExpress.ExpressApp;
using Visa2026.Blazor.Server.Services.Migration;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.MigrationImport;
using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Links existing (input) WorkPermitItem / InvitationItem onto ApplicationProfileInstance
/// via ResolvedLink + skip-nav M2M (ApplicationProfileInstance.WorkPermitItems / InvitationItems).
/// Sources: PersonInApplication.WorkPermit and InvitationToBeCancelled → PersonInInvitation.
/// Does not set WorkPermit/Invitation header ApplicationProfileInstance FK (newly issued path).
/// </summary>
internal sealed class Visa2014ExistingItemLinkCorrectionResult
{
    public int LegacyRowsInScope { get; init; }
    public int WorkPermitItemLinked { get; init; }
    public int WorkPermitItemAlreadyLinked { get; init; }
    public int WorkPermitItemMissingIdMap { get; init; }
    public int InvitationItemLinked { get; init; }
    public int InvitationItemAlreadyLinked { get; init; }
    public int InvitationItemMissingIdMap { get; init; }
    public int SkippedMissingParentIdMap { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

internal static class Visa2014ExistingItemLinkCorrection
{
    internal const string ExtractSql = """
        SELECT
            CAST(pia.Oid AS varchar(36)) AS PiaOid,
            CAST(pia.Application AS varchar(36)) AS ApplicationOid,
            CAST(COALESCE(pia.Employee, pia.FamilyMember) AS varchar(36)) AS PersonOid,
            CAST(pia.WorkPermit AS varchar(36)) AS ExistingWorkPermitOid,
            CAST(pii.Oid AS varchar(36)) AS ExistingInvitationItemOid
        FROM dbo.PersonInApplication pia
        LEFT JOIN dbo.PersonInInvitation pii
            ON pii.Invitation = pia.InvitationToBeCancelled
           AND pii.GCRecord IS NULL
           AND COALESCE(pii.Employee, pii.FamilyMember) = COALESCE(pia.Employee, pia.FamilyMember)
        WHERE pia.GCRecord IS NULL
          AND (
                pia.WorkPermit IS NOT NULL
             OR pia.InvitationToBeCancelled IS NOT NULL
          )
        """;

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

        Console.WriteLine("=== VISA2014 existing WorkPermitItem/InvitationItem → ApplicationProfileInstance M2M");
        Console.WriteLine($"INF Legacy source: {source.Id}");
        Console.WriteLine($"INF Target SQL: {MaskConnectionString(targetConnection)}");
        if (dryRun) Console.WriteLine("INF Mode: dry-run (no writes)");

        HeadlessMigrationHost? host = null;
        IDisposable? importScope = null;
        try
        {
            host = HeadlessMigrationHost.Start(targetConnection);
            importScope = MigrationImportContext.BeginDataImportScope();

            var applicationIdMapPath = GetOptionValue(args, "--application-id-map")
                ?? source.IdMapPath(dataImporterRoot, "ApplicationProfileInstance");
            var personIdMapPath = GetOptionValue(args, "--person-id-map")
                ?? source.IdMapPath(dataImporterRoot, "Person");
            var workPermitItemIdMapPath = GetOptionValue(args, "--workpermititem-id-map")
                ?? source.IdMapPath(dataImporterRoot, "WorkPermitItem");
            var invitationItemIdMapPath = GetOptionValue(args, "--invitationitem-id-map")
                ?? source.IdMapPath(dataImporterRoot, "InvitationItem");

            var applicationIdMap = LoadMap(applicationIdMapPath);
            var personIdMap = LoadMap(personIdMapPath);
            var workPermitItemIdMap = LoadMap(workPermitItemIdMapPath);
            var invitationItemIdMap = LoadMap(invitationItemIdMapPath);

            Console.WriteLine($"INF ApplicationProfileInstance id-map: {applicationIdMap.Count}");
            Console.WriteLine($"INF Person id-map: {personIdMap.Count}");
            Console.WriteLine($"INF WorkPermitItem id-map: {workPermitItemIdMap.Count}");
            Console.WriteLine($"INF InvitationItem id-map: {invitationItemIdMap.Count}");

            var result = Run(
                host.ObjectSpaceFactory,
                source.ConnectionString,
                applicationIdMap,
                personIdMap,
                workPermitItemIdMap,
                invitationItemIdMap,
                dryRun,
                verbose);

            Console.WriteLine($"INF Legacy PIA rows in scope: {result.LegacyRowsInScope}");
            Console.WriteLine($"INF WorkPermitItem linked: {result.WorkPermitItemLinked}  already: {result.WorkPermitItemAlreadyLinked}  missing id-map: {result.WorkPermitItemMissingIdMap}");
            Console.WriteLine($"INF InvitationItem linked: {result.InvitationItemLinked}  already: {result.InvitationItemAlreadyLinked}  missing id-map: {result.InvitationItemMissingIdMap}");
            Console.WriteLine($"INF Skipped missing Application/Person id-map: {result.SkippedMissingParentIdMap}");
            foreach (var error in result.Errors.Take(20))
                Console.Error.WriteLine($"ERR {error}");
            return result.Errors.Count > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR Correction failed: {ex.Message}");
            if (verbose) Console.Error.WriteLine(ex);
            return 1;
        }
        finally { importScope?.Dispose(); host?.Dispose(); }
    }

    internal static Visa2014ExistingItemLinkCorrectionResult Run(
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        string legacyConnectionString,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap,
        IReadOnlyDictionary<Guid, Guid> personIdMap,
        IReadOnlyDictionary<Guid, Guid> workPermitItemIdMap,
        IReadOnlyDictionary<Guid, Guid> invitationItemIdMap,
        bool dryRun,
        bool verbose)
    {
        var rows = Visa2014SqlCmdReader.Query(legacyConnectionString, ExtractSql, verbose);
        var errors = new List<string>();
        int skippedParent = 0;
        int wpLinked = 0, wpAlready = 0, wpMissing = 0;
        int invLinked = 0, invAlready = 0, invMissing = 0;
        int pending = 0;
        const int batchSize = 50;

        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.ApplicationProfileInstance));
        MigrationImportContext.ApplyImportObjectSpaceHooks(objectSpace);

        foreach (var dict in rows)
        {
            if (!Guid.TryParse(dict.GetValueOrDefault("ApplicationOid")?.Trim(), out var legacyAppOid)
                || !Guid.TryParse(dict.GetValueOrDefault("PersonOid")?.Trim(), out var legacyPersonOid))
            {
                skippedParent++;
                continue;
            }

            if (!applicationIdMap.TryGetValue(legacyAppOid, out var applicationId)
                || !personIdMap.TryGetValue(legacyPersonOid, out var personId))
            {
                skippedParent++;
                continue;
            }

            var application = objectSpace.GetObjectByKey<Bo.ApplicationProfileInstance>(applicationId);
            var person = objectSpace.GetObjectByKey<Bo.Person>(personId);
            if (application == null || person == null)
            {
                skippedParent++;
                continue;
            }

            if (Guid.TryParse(dict.GetValueOrDefault("ExistingWorkPermitOid")?.Trim(), out var legacyWpOid))
            {
                if (!workPermitItemIdMap.TryGetValue(legacyWpOid, out var workPermitItemId))
                {
                    wpMissing++;
                }
                else
                {
                    var outcome = TryEnsureLink(
                        objectSpace,
                        application,
                        person,
                        Bo.ApplicationProfileInstancePersonLinkKind.WorkPermitItem,
                        workPermitItemId,
                        dryRun,
                        verbose);
                    if (outcome == LinkOutcome.Linked) wpLinked++;
                    else if (outcome == LinkOutcome.Already) wpAlready++;
                }
            }

            if (Guid.TryParse(dict.GetValueOrDefault("ExistingInvitationItemOid")?.Trim(), out var legacyInvItemOid))
            {
                if (!invitationItemIdMap.TryGetValue(legacyInvItemOid, out var invitationItemId))
                {
                    invMissing++;
                }
                else
                {
                    var outcome = TryEnsureLink(
                        objectSpace,
                        application,
                        person,
                        Bo.ApplicationProfileInstancePersonLinkKind.InvitationItem,
                        invitationItemId,
                        dryRun,
                        verbose);
                    if (outcome == LinkOutcome.Linked) invLinked++;
                    else if (outcome == LinkOutcome.Already) invAlready++;
                }
            }

            if (!dryRun)
            {
                pending++;
                if (pending >= batchSize)
                {
                    objectSpace.CommitChanges();
                    pending = 0;
                }
            }
        }

        if (!dryRun && pending > 0)
            objectSpace.CommitChanges();

        return new Visa2014ExistingItemLinkCorrectionResult
        {
            LegacyRowsInScope = rows.Count,
            WorkPermitItemLinked = wpLinked,
            WorkPermitItemAlreadyLinked = wpAlready,
            WorkPermitItemMissingIdMap = wpMissing,
            InvitationItemLinked = invLinked,
            InvitationItemAlreadyLinked = invAlready,
            InvitationItemMissingIdMap = invMissing,
            SkippedMissingParentIdMap = skippedParent,
            Errors = errors,
        };
    }

    private enum LinkOutcome { Linked, Already, Skipped }

    private static LinkOutcome TryEnsureLink(
        IObjectSpace objectSpace,
        Bo.ApplicationProfileInstance application,
        Bo.Person person,
        Bo.ApplicationProfileInstancePersonLinkKind kind,
        Guid linkedObjectId,
        bool dryRun,
        bool verbose)
    {
        var existing = ApplicationProfileInstancePersonResolver.LoadLinks(objectSpace, application.ID, person.ID);
        foreach (var link in existing)
        {
            if (link.LinkKind == kind && link.LinkedObjectId == linkedObjectId)
                return LinkOutcome.Already;
            if (link.LinkKind == kind && link.LinkedObjectId is Guid id && id != Guid.Empty && id != linkedObjectId)
                return LinkOutcome.Already;
        }

        if (dryRun)
            return LinkOutcome.Linked;

        ApplicationProfileInstancePersonResolver.EnsureResolvedLink(
            objectSpace, application, person, kind, linkedObjectId);
        if (verbose)
            Console.WriteLine($"  LINK {kind} {linkedObjectId} -> app {application.ID} person {person.ID}");
        return LinkOutcome.Linked;
    }

    private static Dictionary<Guid, Guid> LoadMap(string path) =>
        File.Exists(path) ? Visa2014IdMapHelper.Load(path) : new Dictionary<Guid, Guid>();

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

    private static string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return "(empty)";
        return System.Text.RegularExpressions.Regex.Replace(
            connectionString,
            @"(Password|Pwd)=[^;]+",
            "$1=***",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}