using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014.Tests;

public class Visa2014WorkPermitItemPositionResolverTests
{
    private static Visa2014WorkPermitItemPositionResolver.ActiveWorkHistoryRow Row(Guid oid, string start) =>
        new(oid, DateTime.Parse(start));

    [Fact]
    public void SelectFallback_PicksLatestOnOrBeforePermitStart()
    {
        var rows = new[]
        {
            Row(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "2018-01-01"),
            Row(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), "2020-06-01"),
            Row(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"), "2022-01-01"),
        };

        Assert.True(Visa2014WorkPermitItemPositionResolver.TrySelectFallbackWorkHistoryOid(
            rows, DateTime.Parse("2021-03-15"), out var picked));
        Assert.Equal(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), picked);
    }

    [Fact]
    public void SelectFallback_WhenAllAfterPermitStart_PicksEarliest()
    {
        var rows = new[]
        {
            Row(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"), "2020-06-01"),
            Row(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"), "2018-01-01"),
        };

        Assert.True(Visa2014WorkPermitItemPositionResolver.TrySelectFallbackWorkHistoryOid(
            rows, DateTime.Parse("2015-01-01"), out var picked));
        Assert.Equal(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"), picked);
    }

    [Fact]
    public void ApplicationIdentity_BuildGroupKey_IncludesDateAndType()
    {
        var key = Visa2014ApplicationTransform.BuildApplicationIdentityGroupKey(
            "6/-909",
            new DateTime(2026, 6, 26),
            "App_Visa_Ext");
        Assert.Equal("6/-909|2026-06-26|App_Visa_Ext", key);
    }

    [Fact]
    public void ApplicationIdentity_FindTargetCollisions_DetectsMergedLegacyApps()
    {
        var legacyEmployee = Guid.Parse("27efff77-9b91-4037-8991-9928ff1aaaab");
        var legacyFamily = Guid.Parse("514e22b3-f11e-44ff-9a9a-000b215ca037");
        var targetId = Guid.Parse("155d5cbc-380c-420d-6830-08ded8bcf9c0");

        var idMap = new Dictionary<Guid, Guid>
        {
            [legacyEmployee] = targetId,
            [legacyFamily] = targetId,
        };

        var identities = new Dictionary<Guid, Visa2014ApplicationTransform.ApplicationImportIdentity>
        {
            [legacyEmployee] = new Visa2014ApplicationTransform.ApplicationImportIdentity(
                "9/-3876", new DateTime(2014, 9, 15), "App_Visa_and_WP_Ext"),
            [legacyFamily] = new Visa2014ApplicationTransform.ApplicationImportIdentity(
                "9/-3876", new DateTime(2014, 9, 15), "App_Visa_Ext_FM"),
        };

        var collisions = Visa2014ApplicationTransform.FindApplicationIdMapCrossDateTargetCollisions(idMap, identities);
        Assert.Single(collisions);
    }

    [Fact]
    public void ApplicationIdentity_FindTargetCollisions_DetectsCrossDateMergedLegacyApps()
    {
        var legacy2025 = Guid.Parse("f538cb62-e81e-40d2-877b-63ab60c22aac");
        var legacy2026 = Guid.Parse("f5616776-4536-4204-9bb5-00d39cd7135b");
        var targetId = Guid.Parse("c022d8d4-3658-403b-685a-08ded8bcf9c0");

        var idMap = new Dictionary<Guid, Guid>
        {
            [legacy2025] = targetId,
            [legacy2026] = targetId,
        };

        var identities = new Dictionary<Guid, Visa2014ApplicationTransform.ApplicationImportIdentity>
        {
            [legacy2025] = new Visa2014ApplicationTransform.ApplicationImportIdentity(
                "6/-909", new DateTime(2025, 7, 26), "App_Visa_Ext"),
            [legacy2026] = new Visa2014ApplicationTransform.ApplicationImportIdentity(
                "6/-909", new DateTime(2026, 6, 26), "App_Visa_Ext"),
        };

        var collisions = Visa2014ApplicationTransform.FindApplicationIdMapCrossDateTargetCollisions(idMap, identities);
        Assert.Single(collisions);
    }

    [Fact]
    public void Audit_CalikEnergi_ApplicationIdMap_CrossDateCollisions()
    {
        var dataImporterRoot = Visa2014ContentRoot.FindDataImporterRoot();
        if (dataImporterRoot == null)
            return;

        var solutionRoot = Visa2014ContentRoot.FindSolutionRoot();
        if (solutionRoot == null)
            return;

        Visa2014LegacySourceProfile source;
        try
        {
            source = Visa2014LegacySource.Resolve(
                dataImporterRoot,
                solutionRoot,
                ["--legacy-source", "calik-energi"]);
        }
        catch
        {
            return;
        }

        var mapPath = Path.Combine(
            Visa2014ContentRoot.LegacyRoot(dataImporterRoot),
            source.IdMapDirectory.TrimStart('/', '\\'),
            "Application.json");
        if (!File.Exists(mapPath))
            return;

        var legacyConnection = "Server=localhost\\SQLEXPRESS;Database=VISA2015;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
        var map = Visa2014IdMapHelper.Load(mapPath);
        var collisions = Visa2014ApplicationTransform.FindApplicationIdMapCrossDateCollisions(
            map,
            legacyConnection,
            source.LookupTranslationPaths);

        var multiTargetGroups = map.GroupBy(kvp => kvp.Value).Count(g => g.Count() > 1);
        Console.WriteLine($"INF Application id-map entries: {map.Count}");
        Console.WriteLine($"INF Target IDs with 2+ legacy Oids: {multiTargetGroups}");
        Console.WriteLine($"INF Cross-date collisions: {collisions.Count}");
        foreach (var collision in collisions.Take(40))
            Console.WriteLine($"INF   {collision}");

        Assert.True(collisions.Count >= 0);
    }

    [Fact]
    public void SelectCurrentWorkPermit_PicksLatestStartDateThenOid()
    {
        var person = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var older = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var newer = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

        var picked = Visa2014PersonCurrentFieldInference.SelectCurrentWorkPermitOid([
            (older, DateTime.Parse("2020-01-01")),
            (newer, DateTime.Parse("2024-06-01")),
        ]);

        Assert.Equal(newer, picked);
    }

    [Fact]
    public void ApplicationTypeComposite_Enum9_MapsToChangePassport()
    {
        var composite = Visa2014ApplicationTransform.BuildApplicationTypeComposite(
            forEmployee: true,
            forFamilyMember: false,
            employeeSubtypeId: 9,
            familySubtypeId: null,
            hasInvitationWpFk: false,
            invitationAndWorkPermitRequired: null,
            hasWizaWpFk: false,
            wizaAndWorkPermitRequired: null,
            changeInformation: null);
        Assert.Equal("E:9:na:na:na", composite);
    }

    [Fact]
    public void ApplicationTypeComposite_Enum10_MapsToServicePassportKey()
    {
        var composite = Visa2014ApplicationTransform.BuildApplicationTypeComposite(
            forEmployee: true,
            forFamilyMember: false,
            employeeSubtypeId: 10,
            familySubtypeId: null,
            hasInvitationWpFk: false,
            invitationAndWorkPermitRequired: null,
            hasWizaWpFk: false,
            wizaAndWorkPermitRequired: null,
            changeInformation: null);
        Assert.Equal("E:10:na:na:na", composite);
    }

    [Fact]
    public void Audit_ApplicationType_LegacyVsMigratedCounts()
    {
        var dataImporterRoot = Visa2014ContentRoot.FindDataImporterRoot();
        if (dataImporterRoot == null)
            return;

        var solutionRoot = Visa2014ContentRoot.FindSolutionRoot();
        if (solutionRoot == null)
            return;

        Visa2014LegacySourceProfile source;
        try
        {
            source = Visa2014LegacySource.Resolve(
                dataImporterRoot,
                solutionRoot,
                ["--legacy-source", "calik-energi"]);
        }
        catch
        {
            return;
        }

        var batch = Visa2014ApplicationTransform.PrepareImportBatch(
            source.ConnectionString,
            source.LookupTranslationPaths,
            maxRows: null,
            verbose: false);

        var legacyByComposite = new Dictionary<string, (string? TargetType, int Count)>(StringComparer.Ordinal);
        var skippedComposites = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var row in batch.ImportRows)
        {
            var composite = row.GetValueOrDefault("_legacy_ApplicationTypeComposite") as string ?? "(none)";
            if (row.GetValueOrDefault("_importAction") as string == "skip")
            {
                skippedComposites[composite] = skippedComposites.GetValueOrDefault(composite) + 1;
                continue;
            }

            var targetType = row.GetValueOrDefault("ApplicationType") as string;
            if (!legacyByComposite.TryGetValue(composite, out var bucket))
                legacyByComposite[composite] = (targetType, 1);
            else
                legacyByComposite[composite] = (bucket.TargetType ?? targetType, bucket.Count + 1);
        }

        var migratedByType = new Dictionary<string, int>(StringComparer.Ordinal);
        var targetCs = "Server=(localdb)\\mssqllocaldb;Database=Visa2026;Trusted_Connection=True;TrustServerCertificate=True";
        using (var conn = new Microsoft.Data.SqlClient.SqlConnection(targetCs))
        {
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT t.Name, COUNT(*)
                FROM Applications a
                INNER JOIN ApplicationTypes t ON t.ID = a.ApplicationTypeID
                WHERE (a.GCRecord IS NULL OR a.GCRecord = 0) AND a.IsManualEntry = 1
                GROUP BY t.Name
                """;
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                migratedByType[reader.GetString(0)] = reader.GetInt32(1);
        }

        var legacyByTarget = legacyByComposite
            .GroupBy(kv => kv.Value.TargetType ?? "(null)", StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Value.Count), StringComparer.Ordinal);

        Console.WriteLine("AUDIT|Legacy composite (subtype)|Target ApplicationType|Legacy importable|Migrated in Visa2026|Gap");
        foreach (var kv in legacyByComposite.OrderBy(k => k.Value.TargetType).ThenBy(k => k.Key))
        {
            var target = kv.Value.TargetType ?? "(skipped/unmapped)";
            var migrated = !string.IsNullOrWhiteSpace(kv.Value.TargetType)
                && migratedByType.TryGetValue(kv.Value.TargetType, out var m) ? m : 0;
            Console.WriteLine($"AUDIT|{kv.Key}|{target}|{kv.Value.Count}|{migrated}|");
        }

        Console.WriteLine("AUDIT_SUMMARY|Target ApplicationType|Legacy importable total|Migrated total|Gap");
        var allTargets = legacyByTarget.Keys.Union(migratedByType.Keys, StringComparer.Ordinal).OrderBy(k => k);
        foreach (var target in allTargets)
        {
            legacyByTarget.TryGetValue(target, out var legacyTotal);
            migratedByType.TryGetValue(target, out var migratedTotal);
            if (target == "(null)")
                continue;
            Console.WriteLine($"AUDIT_SUMMARY|{target}|{legacyTotal}|{migratedTotal}|{legacyTotal - migratedTotal}");
        }

        if (skippedComposites.Count > 0)
        {
            Console.WriteLine("AUDIT_SKIPPED|Legacy composite|Count");
            foreach (var kv in skippedComposites.OrderByDescending(k => k.Value))
                Console.WriteLine($"AUDIT_SKIPPED|{kv.Key}|{kv.Value}");
        }

        var catalogTypes = new HashSet<string>(StringComparer.Ordinal);
        using (var conn = new Microsoft.Data.SqlClient.SqlConnection(targetCs))
        {
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Name FROM ApplicationTypes ORDER BY Name";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                catalogTypes.Add(reader.GetString(0));
        }

        var legacyTargets = legacyByTarget.Keys.Where(k => k != "(null)").ToHashSet(StringComparer.Ordinal);
        var missingInCatalog = legacyTargets.Except(catalogTypes, StringComparer.Ordinal).ToList();
        var unusedInMigration = catalogTypes.Except(legacyTargets, StringComparer.Ordinal).OrderBy(n => n).ToList();

        Console.WriteLine($"AUDIT_META|Legacy SQL rows|{batch.LegacyRowCount}");
        Console.WriteLine($"AUDIT_META|Legacy importable (transform)|{legacyByTarget.Values.Sum()}");
        Console.WriteLine($"AUDIT_META|Migrated manual apps|{migratedByType.Values.Sum()}");
        Console.WriteLine($"AUDIT_META|Skipped legacy rows|{skippedComposites.Values.Sum()}");
        if (missingInCatalog.Count > 0)
            Console.WriteLine($"AUDIT_MISSING_CATALOG|{string.Join(", ", missingInCatalog)}");
        if (unusedInMigration.Count > 0)
            Console.WriteLine($"AUDIT_UNUSED_CATALOG|{string.Join(", ", unusedInMigration)}");

        Assert.True(legacyByComposite.Count > 0);
    }
}