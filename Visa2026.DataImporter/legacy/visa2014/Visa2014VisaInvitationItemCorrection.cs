using DevExpress.ExpressApp;
using Visa2026.Blazor.Server.Services.Migration;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.MigrationImport;
using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014VisaInvitationItemCorrectionResult
{
    public int VisasInScope { get; init; }
    public int Updated { get; init; }
    public int AlreadyCorrect { get; init; }
    public int SkippedNoIssuingItem { get; init; }
    public int SkippedNotInvitationIssuing { get; init; }
    public int SkippedNoPerson { get; init; }
    public int SkippedNoMatch { get; init; }
    public int SkippedMissingVisaMap { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// Path B post-pass: set Visa.InvitationItem (and IsUsed) after IssuingApplicationItem correction.
/// Target-side closest-match only; does not call Path A.
/// </summary>
internal static class Visa2014VisaInvitationItemCorrection
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
        var visaIdMapPath = GetOptionValue(args, "--visa-id-map")
            ?? source.IdMapPath(dataImporterRoot, "Visa");

        Console.WriteLine("=== VISA2014 Visa InvitationItem correction (Path B)");
        Console.WriteLine($"INF Legacy source: {source.Id}");
        Console.WriteLine($"INF Target SQL: {MaskConnectionString(targetConnection)}");
        Console.WriteLine($"INF Visa id-map: {visaIdMapPath}");
        if (dryRun) Console.WriteLine("INF Mode: dry-run (no writes)");

        HeadlessMigrationHost? host = null;
        IDisposable? importScope = null;
        try
        {
            host = HeadlessMigrationHost.Start(targetConnection);
            importScope = MigrationImportContext.BeginDataImportScope();

            var visaIdMap = Visa2014IdMapHelper.LoadOrEmpty(visaIdMapPath);
            if (visaIdMap.Count == 0)
            {
                Console.Error.WriteLine($"ERR Visa id-map empty or missing: {visaIdMapPath}");
                return 1;
            }

            var result = await RunAsync(host.ObjectSpaceFactory, visaIdMap, dryRun, verbose);

            Console.WriteLine($"INF Visas in id-map: {result.VisasInScope}");
            Console.WriteLine($"INF InvitationItem updated: {result.Updated}");
            Console.WriteLine($"INF Already correct: {result.AlreadyCorrect}");
            Console.WriteLine($"INF No IssuingApplicationItem: {result.SkippedNoIssuingItem}");
            Console.WriteLine($"INF Issuing type cannot issue invitation: {result.SkippedNotInvitationIssuing}");
            Console.WriteLine($"INF No passport person: {result.SkippedNoPerson}");
            Console.WriteLine($"INF No InvitationItem match: {result.SkippedNoMatch}");
            Console.WriteLine($"INF Missing Visa target: {result.SkippedMissingVisaMap}");

            foreach (var error in result.Errors.Take(40))
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

    internal static Task<Visa2014VisaInvitationItemCorrectionResult> RunAsync(
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        IReadOnlyDictionary<Guid, Guid> visaIdMap,
        bool dryRun,
        bool verbose)
    {
        var errors = new List<string>();
        int updated = 0, alreadyCorrect = 0, noIssuing = 0, notInvIssuing = 0, noPerson = 0, noMatch = 0, missingVisa = 0;

        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.Visa));
        MigrationImportContext.ApplyImportObjectSpaceHooks(objectSpace);

        var visasById = objectSpace.GetObjectsQuery<Bo.Visa>()
            .Where(v => v.GCRecord == 0)
            .ToDictionary(v => v.ID);

        var linkedInvitationIds = objectSpace.GetObjectsQuery<Bo.Visa>()
            .Where(v => v.GCRecord == 0 && v.InvitationItem != null)
            .Select(v => new { VisaId = v.ID, InvitationItemId = v.InvitationItem!.ID })
            .ToList()
            .GroupBy(x => x.InvitationItemId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.VisaId).ToHashSet());

        var invitationItems = objectSpace.GetObjectsQuery<Bo.InvitationItem>()
            .Where(ii => ii.GCRecord == 0)
            .ToList();

        foreach (var (_, targetVisaId) in visaIdMap)
        {
            if (!visasById.TryGetValue(targetVisaId, out var visa))
            {
                missingVisa++;
                continue;
            }

            var issuingApplication = visa.IssuingApplicationProfileInstance;
            if (issuingApplication == null)
            {
                noIssuing++;
                continue;
            }

            var applicationType = issuingApplication.ApplicationType;
            if (!ApplicationTypeCapabilities.CanIssueInvitation(applicationType))
            {
                notInvIssuing++;
                continue;
            }

            var person = visa.Passport?.Person;
            var application = issuingApplication;
            if (person == null || application == null)
            {
                noPerson++;
                continue;
            }

            if (visa.InvitationItem != null
                && visa.InvitationItem.Person?.ID == person.ID
                && visa.InvitationItem.Invitation?.ApplicationProfileInstance?.ID == application.ID
                && !visa.InvitationItem.IsCancelled
                && !visa.InvitationItem.IsChanged)
            {
                if (!dryRun && !visa.InvitationItem.IsUsed)
                    visa.InvitationItem.IsUsed = true;
                alreadyCorrect++;
                continue;
            }

            var linkedToOthers = linkedInvitationIds
                .Where(kv => !kv.Value.Contains(visa.ID))
                .Select(kv => kv.Key)
                .ToHashSet();

            var candidates = invitationItems
                .Where(ii => ii.Person != null && ii.Invitation?.ApplicationProfileInstance != null)
                .Select(ii => new Visa2014VisaInvitationItemLinkCandidate
                {
                    InvitationItemId = ii.ID,
                    InvitationId = ii.Invitation!.ID,
                    PersonId = ii.Person!.ID,
                    ApplicationProfileInstanceId = ii.Invitation.ApplicationProfileInstance!.ID,
                    IssuedDate = ii.Invitation.IssuedDate,
                    ApplicationDate = ii.Invitation.ApplicationProfileInstance.ApplicationDate,
                    IsCancelled = ii.IsCancelled,
                    IsChanged = ii.IsChanged,
                    IsUsed = ii.IsUsed,
                });

            var matchId = Visa2014VisaInvitationItemLinkMatcher.SelectClosest(
                person.ID,
                application.ID,
                visa.IssueDate,
                candidates,
                linkedToOthers);

            if (matchId == null)
            {
                noMatch++;
                continue;
            }

            var match = invitationItems.FirstOrDefault(ii => ii.ID == matchId.Value);
            if (match == null)
            {
                noMatch++;
                continue;
            }

            if (dryRun)
            {
                updated++;
                if (verbose && updated <= 20)
                    Console.WriteLine($"  DRY Visa {targetVisaId} <- InvitationItem {matchId}");
                continue;
            }

            visa.InvitationItem = match;
            if (!match.IsUsed)
                match.IsUsed = true;

            if (!linkedInvitationIds.TryGetValue(match.ID, out var visaSet))
            {
                visaSet = new HashSet<Guid>();
                linkedInvitationIds[match.ID] = visaSet;
            }
            visaSet.Add(visa.ID);

            updated++;
            if (verbose && updated % 500 == 0)
                Console.WriteLine($"INF Progress: {updated} updated...");
        }

        if (!dryRun && updated > 0)
            objectSpace.CommitChanges();

        return Task.FromResult(new Visa2014VisaInvitationItemCorrectionResult
        {
            VisasInScope = visaIdMap.Count,
            Updated = updated,
            AlreadyCorrect = alreadyCorrect,
            SkippedNoIssuingItem = noIssuing,
            SkippedNotInvitationIssuing = notInvIssuing,
            SkippedNoPerson = noPerson,
            SkippedNoMatch = noMatch,
            SkippedMissingVisaMap = missingVisa,
            Errors = errors,
        });
    }

    private static string MaskConnectionString(string connectionString) =>
        System.Text.RegularExpressions.Regex.Replace(
            connectionString,
            @"(Password|Pwd)\s*=\s*[^;]+",
            "$1=***",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private static bool HasArg(IReadOnlyList<string> args, string flag) =>
        args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

    private static string? GetOptionValue(IReadOnlyList<string> args, string optionName)
    {
        for (int i = 0; i < args.Count - 1; i++)
        {
            if (string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return null;
    }
}