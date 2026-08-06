using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// One-time backfill: copy linked <see cref="Person.FamilyMembers"/> into
/// <see cref="Person.VisaApplicationFamilyMembersText"/> when manual text is empty or <c>Ýok</c>.
/// </summary>
public sealed class VisaFamilyManualFromFamilyMembersMigrationUpdater : ModuleUpdater
{
    public VisaFamilyManualFromFamilyMembersMigrationUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();

        var migrated = 0;
        foreach (var person in ObjectSpace.GetObjects<Person>().Where(p => p.IsEmployee))
        {
            if (!VisaFamilyMemberLinesHelper.IsManualVisaFamilyEmpty(person.VisaApplicationFamilyMembersText))
            {
                continue;
            }

            var formatted = VisaFamilyMemberLinesHelper.FormatLinesFromFamilyMembers(person);
            if (string.IsNullOrWhiteSpace(formatted))
            {
                continue;
            }

            person.VisaApplicationFamilyMembersText = formatted;
            migrated++;
        }

        if (migrated > 0)
        {
            ObjectSpace.CommitChanges();
            Tracing.Tracer.LogText(
                $"VisaFamilyManualFromFamilyMembersMigrationUpdater: copied FamilyMembers to manual text for {migrated} employee(s).");
        }
    }
}
