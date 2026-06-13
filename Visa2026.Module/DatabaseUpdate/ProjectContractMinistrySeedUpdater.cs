using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures <see cref="ApprovingMinistry"/> rows and assigns ministry legs to <see cref="ProjectContract"/> processes.
/// Migration service is <em>not</em> an approving ministry — it is the fixed post-ministry progress step
/// (<c>PROCESS_STARTED</c> @ <c>AT_MIGRATION_SERVICE</c>) and the separate <see cref="MigrationService"/> lookup on Application.
/// </summary>
public sealed class ProjectContractMinistrySeedUpdater : ModuleUpdater
{
    public ProjectContractMinistrySeedUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        EnsureDefaultMinistries();
        EnsureLegsForContracts();
        ObjectSpace.CommitChanges();
    }

    private void EnsureDefaultMinistries()
    {
        EnsureMinistry("Gurluşyk ministrligi", "Gurluşyk");
        EnsureMinistry("Söwda we daşky guramalar ministrligi", "Söwda");
        EnsureMinistry("Energetika ministrligi", "Energetika");
    }

    private ApprovingMinistry EnsureMinistry(string nameTm, string shortNameTm)
    {
        var existing = ObjectSpace.GetObjectsQuery<ApprovingMinistry>()
            .FirstOrDefault(m => m.ShortNameTm == shortNameTm || m.NameTm == nameTm);
        if (existing != null)
            return existing;

        var ministry = ObjectSpace.CreateObject<ApprovingMinistry>();
        ministry.NameTm = nameTm;
        ministry.ShortNameTm = shortNameTm;
        ministry.IsActive = true;
        return ministry;
    }

    private void EnsureLegsForContracts()
    {
        foreach (var contract in ObjectSpace.GetObjectsQuery<ProjectContract>().ToList())
        {
            if (ProjectContractMinistryHelper.HasConfiguredLegs(contract))
                continue;

            var legShortNames = ResolveLegShortNames(contract);
            for (var i = 0; i < legShortNames.Count; i++)
            {
                var ministry = ObjectSpace.GetObjectsQuery<ApprovingMinistry>()
                    .FirstOrDefault(m => m.IsActive && m.ShortNameTm == legShortNames[i]);
                if (ministry == null)
                    continue;

                var leg = ObjectSpace.CreateObject<ProjectContractMinistryLeg>();
                leg.ProjectContract = contract;
                leg.Sequence = i + 1;
                leg.ApprovingMinistry = ministry;
            }

            contract.IsActive = true;
            var line = $"ProjectContractMinistrySeedUpdater: added {legShortNames.Count} leg(s) to '{contract.NameTm}'.";
            Tracing.Tracer.LogText(line);
            Console.WriteLine(line);
        }
    }

    private static IReadOnlyList<string> ResolveLegShortNames(ProjectContract contract)
    {
        var name = contract.NameTm ?? string.Empty;
        if (name.Contains("3 ministrlik", StringComparison.OrdinalIgnoreCase))
            return ["Gurluşyk", "Söwda", "Energetika"];

        if (name.Contains("2 ministrlik", StringComparison.OrdinalIgnoreCase)
            || contract.MinistryReviewDepth == MinistryReviewDepth.FirstAndSecondMinistry)
            return ["Gurluşyk", "Söwda"];

        if (name.Contains("1 ministrlik", StringComparison.OrdinalIgnoreCase)
            || name.Contains("gysga", StringComparison.OrdinalIgnoreCase))
            return ["Gurluşyk"];

        return ["Gurluşyk"];
    }
}
