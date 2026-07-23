using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>Idempotent seed for <see cref="ApplicationTypeGroup"/> (Registration and members).</summary>
public static class ApplicationTypeGroupSeed
{
    /// <summary>Same set as ShowRegistrations / former UserReportTemplateUpdater registration type list.</summary>
    public static readonly string[] RegistrationApplicationTypeNames =
    {
        "App_Reg_Check_In",
        "App_Reg_Check_In_Internal",
        "App_Reg_Check_Out",
        "App_Reg_Check_Out_Internal",
        "App_Reg_ext",
        "App_Reg_Info_Change_Address",
        "App_Reg_Info_Change_Passport",
        "App_Reg_Info_Change_Visa",
    };

    public static void EnsureRegistrationGroup(IObjectSpace objectSpace)
    {
        if (objectSpace == null)
            return;

        // SeedGate can run when ModuleInfo skipped EF schema sync (Postgres pilot).
        ApplicationTypeGroupSchemaSql.EnsureTables(objectSpace);

        var group = objectSpace.FirstOrDefault<ApplicationTypeGroup>(
            g => g.Name == ApplicationTypeGroupNames.Registration);
        if (group == null)
        {
            group = objectSpace.CreateObject<ApplicationTypeGroup>();
            group.Name = ApplicationTypeGroupNames.Registration;
        }

        group.NameTm = "Hasaba alyş";
        group.Code = "registration";
        group.LocalizationKey = "ApplicationTypeGroup.Registration";
        group.SortOrder = 10;
        group.IsActive = true;
        objectSpace.SetModified(group);

        var typesByName = objectSpace.GetObjectsQuery<ApplicationType>()
            .AsEnumerable()
            .Where(t => !string.IsNullOrWhiteSpace(t.Name))
            .GroupBy(t => t.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var existingTypeIds = group.Members
            .Where(m => m.ApplicationType != null || m.ApplicationTypeId != Guid.Empty)
            .Select(m => m.ApplicationType?.ID ?? m.ApplicationTypeId)
            .ToHashSet();

        foreach (var typeName in RegistrationApplicationTypeNames)
        {
            if (!typesByName.TryGetValue(typeName, out var appType))
            {
                Console.WriteLine(
                    $"ApplicationTypeGroupSeed: ApplicationType '{typeName}' not found — Registration group missing member.");
                continue;
            }

            appType = objectSpace.GetObject(appType);
            if (!existingTypeIds.Add(appType.ID))
                continue;

            var member = objectSpace.CreateObject<ApplicationTypeGroupMember>();
            member.ApplicationTypeGroup = group;
            member.ApplicationType = appType;
            group.Members.Add(member);
        }
    }
}