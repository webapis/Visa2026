using DevExpress.ExpressApp;
using Visa2026.Blazor.Server.Services.Migration;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.MigrationImport;
using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014FamilyMemberEducationCorrectionResult
{
    public int FamilyMembersInScope { get; init; }
    public int Updated { get; init; }
    public int Created { get; init; }
    public int Unchanged { get; init; }
    public int SkippedChild { get; init; }
    public int SkippedNoCountry { get; init; }
    public int UnresolvedLookups { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

internal static class Visa2014FamilyMemberEducationCorrection
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

        Console.WriteLine("=== VISA2014 family-member Education correction");
        Console.WriteLine($"INF Legacy source: {source.Id}");
        Console.WriteLine($"INF Target SQL: {MaskConnectionString(targetConnection)}");
        if (dryRun) Console.WriteLine("INF Mode: dry-run (no writes)");

        HeadlessMigrationHost? host = null;
        IDisposable? importScope = null;
        try
        {
            host = HeadlessMigrationHost.Start(targetConnection);
            importScope = MigrationImportContext.BeginDataImportScope();

            var personIdMapPath = GetOptionValue(args, "--person-id-map")
                ?? ResolvePersonIdMapPath(dataImporterRoot, source);
            Console.WriteLine($"INF Person id-map: {personIdMapPath}");
            var personIdMap = File.Exists(personIdMapPath)
                ? Visa2014IdMapHelper.Load(personIdMapPath)
                : new Dictionary<Guid, Guid>();

            var result = await RunAsync(
                host.ObjectSpaceFactory,
                source.ConnectionString,
                personIdMap,
                dryRun,
                verbose);

            Console.WriteLine($"INF Family members in scope: {result.FamilyMembersInScope}");
            Console.WriteLine($"INF Education lookups updated: {result.Updated}");
            Console.WriteLine($"INF Education rows created: {result.Created}");
            Console.WriteLine($"INF Unchanged: {result.Unchanged}");
            Console.WriteLine($"INF Skipped child: {result.SkippedChild}");
            Console.WriteLine($"INF Skipped no birth country: {result.SkippedNoCountry}");
            Console.WriteLine($"INF Unresolved lookups: {result.UnresolvedLookups}");
            foreach (var error in result.Errors.Take(20))
                Console.Error.WriteLine($"ERR {error}");
            return result.Errors.Count > 0 || result.UnresolvedLookups > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR Correction failed: {ex.Message}");
            if (verbose) Console.Error.WriteLine(ex);
            return 1;
        }
        finally { importScope?.Dispose(); host?.Dispose(); }
    }

    internal static Task<Visa2014FamilyMemberEducationCorrectionResult> RunAsync(
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        string legacyConnectionString,
        IReadOnlyDictionary<Guid, Guid> personIdMap,
        bool dryRun,
        bool verbose)
    {
        var errors = new List<string>();
        if (personIdMap.Count == 0)
        {
            return Task.FromResult(new Visa2014FamilyMemberEducationCorrectionResult
            {
                Errors = ["Person id-map is empty - import Person first."],
            });
        }

        var legacyFamilyMembers = LoadLegacyFamilyMemberOids(legacyConnectionString, verbose);

        int familyInScope = 0, updated = 0, created = 0, unchanged = 0, skippedChild = 0, skippedNoCountry = 0, unresolved = 0;

        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.Person));
        MigrationImportContext.ApplyImportObjectSpaceHooks(objectSpace);

        foreach (var (legacyOid, targetId) in personIdMap)
        {
            if (!legacyFamilyMembers.Contains(legacyOid))
                continue;

            familyInScope++;

            var person = objectSpace.GetObjectByKey<Bo.Person>(targetId);
            if (person == null)
            {
                errors.Add($"{legacyOid}: target Person {targetId} not found");
                continue;
            }

            if (person.IsEmployee || person.PersonRole == Bo.PersonRecordRole.TemporaryVisitor)
                continue;

            if (!FamilyMemberDefaultEducation.IsAdultFamilyMember(isFamilyMember: true, person.DateOfBirth)
                || ChildDependentEducationCaption.Applies(person))
            {
                skippedChild++;
                continue;
            }

            var educations = objectSpace.GetObjectsQuery<Bo.Education>()
                .Where(e => e.Person != null && e.Person.ID == person.ID)
                .ToList();
            if (educations.Count == 0)
            {
                if (person.CountryOfBirth == null)
                {
                    skippedNoCountry++;
                    if (verbose)
                        Console.WriteLine($"WRN {legacyOid}: adult FM has no Education and no CountryOfBirth");
                    continue;
                }

                if (!dryRun)
                {
                    var createdRow = FamilyMemberDefaultEducation.Ensure(objectSpace, person, ignoreImportGuard: true);
                    if (createdRow == null)
                    {
                        unresolved++;
                        errors.Add($"{legacyOid}: could not create default Education (catalog miss)");
                        continue;
                    }
                }

                created++;
                continue;
            }

            var anyChanged = false;
            var anyUnresolved = false;
            foreach (var education in educations)
            {
                var alreadyMatches = education.EducationLevel?.NameTm == FamilyMemberDefaultEducation.LevelNameTm
                    && education.EducationInstitution?.NameTm == FamilyMemberDefaultEducation.InstitutionNameTm
                    && education.Specialty?.NameTm == FamilyMemberDefaultEducation.SpecialtyNameTm;
                if (alreadyMatches)
                    continue;

                if (dryRun)
                {
                    anyChanged = true;
                    continue;
                }

                if (!FamilyMemberDefaultEducation.TryApplyLookups(objectSpace, education, out var changed))
                {
                    anyUnresolved = true;
                    continue;
                }

                if (changed)
                    anyChanged = true;
            }

            if (anyUnresolved)
            {
                unresolved++;
                errors.Add($"{legacyOid}: default Education lookups not in catalog");
                continue;
            }

            if (anyChanged)
                updated++;
            else
                unchanged++;
        }

        if (!dryRun && (updated > 0 || created > 0))
            objectSpace.CommitChanges();

        return Task.FromResult(new Visa2014FamilyMemberEducationCorrectionResult
        {
            FamilyMembersInScope = familyInScope,
            Updated = updated,
            Created = created,
            Unchanged = unchanged,
            SkippedChild = skippedChild,
            SkippedNoCountry = skippedNoCountry,
            UnresolvedLookups = unresolved,
            Errors = errors,
        });
    }

    private static HashSet<Guid> LoadLegacyFamilyMemberOids(string connectionString, bool verbose)
    {
        const string sql = """
            SELECT CAST(p.Oid AS varchar(36)) AS Oid
            FROM dbo.Person p
            WHERE p.GCRecord IS NULL AND p.IsFamilyMember = 1
            """;
        var set = new HashSet<Guid>();
        foreach (var row in Visa2014SqlCmdReader.Query(connectionString, sql, verbose))
        {
            if (Guid.TryParse(row.GetValueOrDefault("Oid")?.Trim(), out var oid))
                set.Add(oid);
        }

        if (verbose)
            Console.WriteLine($"INF Legacy family-member persons: {set.Count}");
        return set;
    }

    private static string ResolvePersonIdMapPath(string dataImporterRoot, Visa2014LegacySourceProfile source)
    {
        var path = source.IdMapPath(dataImporterRoot, "Person");
        if (File.Exists(path))
            return path;

        var solutionRoot = Visa2014ContentRoot.FindSolutionRoot();
        if (string.IsNullOrWhiteSpace(solutionRoot))
            return path;

        var fromProject = source.IdMapPath(Path.Combine(solutionRoot, "Visa2026.DataImporter"), "Person");
        return File.Exists(fromProject) ? fromProject : path;
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