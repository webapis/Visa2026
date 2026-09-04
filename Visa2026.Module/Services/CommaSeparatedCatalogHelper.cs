using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

/// <summary>Loads and extends lookup catalogs used by comma-separated multi-select editors.</summary>
public static class CommaSeparatedCatalogHelper
{
    public static IReadOnlyList<string> LoadCatalogNames(IObjectSpace objectSpace, Type catalogEntityType, string? noneValue)
    {
        if (objectSpace == null || catalogEntityType == null)
        {
            return Array.Empty<string>();
        }

        // Use GetObjects (not GetObjectsQuery) so pending Delete/Create in this ObjectSpace
        // is reflected before CommitChanges — required for in-popup catalog management.
        if (catalogEntityType == typeof(BorderZoneName))
        {
            return objectSpace.GetObjects<BorderZoneName>()
                .Select(z => z.NameTm)
                .Where(n => IsValidCatalogName(n, noneValue))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        if (catalogEntityType == typeof(WorkPermittedLocationName))
        {
            return objectSpace.GetObjects<WorkPermittedLocationName>()
                .Select(z => z.NameTm)
                .Where(n => IsValidCatalogName(n, noneValue))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        if (!typeof(LookupBase).IsAssignableFrom(catalogEntityType))
        {
            return Array.Empty<string>();
        }

        var objects = objectSpace.GetObjects(catalogEntityType);
        return objects.Cast<LookupBase>()
            .Select(x => x.NameTm)
            .Where(n => IsValidCatalogName(n, noneValue))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool TryAddCatalogEntry(
        IObjectSpace objectSpace,
        Type catalogEntityType,
        string nameTm,
        bool commitChanges = true)
    {
        if (objectSpace == null || catalogEntityType == null || string.IsNullOrWhiteSpace(nameTm))
        {
            return false;
        }

        var trimmed = nameTm.Trim();

        if (FindCatalogEntryByName(objectSpace, catalogEntityType, trimmed) != null)
        {
            return true;
        }

        if (catalogEntityType == typeof(BorderZoneName))
        {
            var zone = objectSpace.CreateObject<BorderZoneName>();
            zone.NameTm = trimmed;
            zone.Name = trimmed;
            if (commitChanges)
            {
                objectSpace.CommitChanges();
            }

            return true;
        }

        if (catalogEntityType == typeof(WorkPermittedLocationName))
        {
            var location = objectSpace.CreateObject<WorkPermittedLocationName>();
            location.NameTm = trimmed;
            location.Name = trimmed;
            if (commitChanges)
            {
                objectSpace.CommitChanges();
            }

            return true;
        }

        if (!typeof(LookupBase).IsAssignableFrom(catalogEntityType))
        {
            return false;
        }

        var entry = (LookupBase)objectSpace.CreateObject(catalogEntityType);
        entry.NameTm = trimmed;
        entry.Name = trimmed;
        if (commitChanges)
        {
            objectSpace.CommitChanges();
        }

        return true;
    }

    public static IReadOnlyList<string> MergeCatalogWithSelected(
        IReadOnlyList<string> catalog,
        IEnumerable<string> selected)
    {
        var merged = new HashSet<string>(catalog, StringComparer.OrdinalIgnoreCase);
        foreach (var item in selected)
        {
            if (!string.IsNullOrWhiteSpace(item))
            {
                merged.Add(item.Trim());
            }
        }

        return merged.OrderBy(z => z, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static int CountCatalogUsage(
        IObjectSpace objectSpace,
        Type catalogEntityType,
        string nameTm,
        CatalogUsageContext? usageContext = null)
    {
        if (objectSpace == null || catalogEntityType == null || string.IsNullOrWhiteSpace(nameTm))
        {
            return 0;
        }

        var label = nameTm.Trim();
        if (catalogEntityType == typeof(BorderZoneName))
        {
            return CountBorderZoneUsage(objectSpace, label, usageContext);
        }

        if (catalogEntityType == typeof(WorkPermittedLocationName))
        {
            return CountWorkPermittedLocationUsage(objectSpace, label, usageContext);
        }

        return 0;
    }

    public static CatalogOperationResult TryRenameCatalogEntry(
        IObjectSpace objectSpace,
        Type catalogEntityType,
        string oldNameTm,
        string newNameTm,
        string? noneValue,
        bool commitChanges = false)
    {
        if (objectSpace == null || catalogEntityType == null)
        {
            return CatalogOperationResult.Fail("CommaMultiSelect.Error.ObjectSpaceUnavailable");
        }

        var oldLabel = oldNameTm?.Trim();
        var newLabel = newNameTm?.Trim();
        if (string.IsNullOrWhiteSpace(oldLabel) || string.IsNullOrWhiteSpace(newLabel))
        {
            return CatalogOperationResult.Fail("CommaMultiSelect.Error.NameRequired");
        }

        if (CommaSeparatedSelectionHelper.IsNoneValue(newLabel, noneValue))
        {
            return CatalogOperationResult.Fail("CommaMultiSelect.Error.NameNotAllowed");
        }

        if (string.Equals(oldLabel, newLabel, StringComparison.OrdinalIgnoreCase))
        {
            return CatalogOperationResult.Ok();
        }

        if (FindCatalogEntryByName(objectSpace, catalogEntityType, newLabel) != null)
        {
            return CatalogOperationResult.Fail("CommaMultiSelect.Error.DuplicateName");
        }

        var entry = FindCatalogEntryByName(objectSpace, catalogEntityType, oldLabel);
        if (entry == null)
        {
            return CatalogOperationResult.Fail("CommaMultiSelect.Error.EntryNotFound");
        }

        entry.NameTm = newLabel;
        entry.Name = newLabel;

        if (catalogEntityType == typeof(BorderZoneName))
        {
            RenameBorderZoneOnAllItems(objectSpace, oldLabel, newLabel);
        }
        else if (catalogEntityType == typeof(WorkPermittedLocationName))
        {
            RenameWorkPermittedLocationOnAllItems(objectSpace, oldLabel, newLabel);
        }

        if (commitChanges)
        {
            objectSpace.CommitChanges();
        }

        return CatalogOperationResult.Ok();
    }

    public static CatalogOperationResult TryDeleteCatalogEntry(
        IObjectSpace objectSpace,
        Type catalogEntityType,
        string nameTm,
        string? noneValue,
        bool commitChanges = false,
        CatalogUsageContext? usageContext = null)
    {
        if (objectSpace == null || catalogEntityType == null)
        {
            return CatalogOperationResult.Fail("CommaMultiSelect.Error.ObjectSpaceUnavailable");
        }

        var label = nameTm?.Trim();
        if (string.IsNullOrWhiteSpace(label))
        {
            return CatalogOperationResult.Fail("CommaMultiSelect.Error.NameRequired");
        }

        var usageCount = CountCatalogUsage(objectSpace, catalogEntityType, label, usageContext);

        if (catalogEntityType == typeof(BorderZoneName))
        {
            StripBorderZoneLabelFromAllItems(objectSpace, label, usageContext);
        }
        else if (catalogEntityType == typeof(WorkPermittedLocationName))
        {
            StripWorkPermittedLocationLabelFromAllItems(objectSpace, label, usageContext);
        }

        var entry = FindCatalogEntryByName(objectSpace, catalogEntityType, label);
        if (entry == null)
        {
            return CatalogOperationResult.Ok("Entry was already removed.", usageCount);
        }

        objectSpace.Delete(entry);
        if (commitChanges)
        {
            objectSpace.CommitChanges();
        }

        return CatalogOperationResult.Ok(usageCount: usageCount);
    }

    private static int CountBorderZoneUsage(IObjectSpace objectSpace, string label, CatalogUsageContext? usageContext) =>
        CountApplicationBorderZoneUsage(objectSpace, label, usageContext)
        + CountVisaBorderZoneUsage(objectSpace, label, usageContext)
        + CountInvitationBorderZoneUsage(objectSpace, label, usageContext);

    private static int CountInvitationBorderZoneUsage(
        IObjectSpace objectSpace,
        string label,
        CatalogUsageContext? usageContext) =>
        objectSpace.GetObjectsQuery<Invitation>()
            .AsEnumerable()
            .Count(i => CommaSeparatedSelectionHelper.ContainsLabel(
                GetInvitationBorderZoneStored(i, usageContext),
                label,
                CommaSeparatedSelectionHelper.NoneValue));

    private static int CountApplicationBorderZoneUsage(
        IObjectSpace objectSpace,
        string label,
        CatalogUsageContext? usageContext) =>
        objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .AsEnumerable()
            .Count(app => CommaSeparatedSelectionHelper.ContainsLabel(
                GetApplicationBorderZoneStored(app, usageContext),
                label,
                CommaSeparatedSelectionHelper.NoneValue));

    private static int CountVisaBorderZoneUsage(
        IObjectSpace objectSpace,
        string label,
        CatalogUsageContext? usageContext) =>
        objectSpace.GetObjectsQuery<Visa>()
            .AsEnumerable()
            .Count(v => CommaSeparatedSelectionHelper.ContainsLabel(
                GetVisaBorderZoneStored(v, usageContext),
                label,
                string.Empty));

    private static int CountWorkPermittedLocationUsage(
        IObjectSpace objectSpace,
        string label,
        CatalogUsageContext? usageContext) =>
        objectSpace.GetObjectsQuery<WorkPermitItem>()
            .AsEnumerable()
            .Count(wpi => CommaSeparatedSelectionHelper.ContainsLabel(
                GetWorkPermitItemWorkPermittedLocationsStored(wpi, usageContext),
                label,
                string.Empty))
        + objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .AsEnumerable()
            .Count(app => CommaSeparatedSelectionHelper.ContainsLabel(
                GetApplicationWorkPermitLocationStored(app, usageContext),
                label,
                string.Empty))
        + objectSpace.GetObjectsQuery<ApplicationProfile>()
            .AsEnumerable()
            .Count(profile => CommaSeparatedSelectionHelper.ContainsLabel(
                profile.DefaultWorkPermitLocation,
                label,
                string.Empty));

    private static string? GetApplicationBorderZoneStored(ApplicationProfileInstance application, CatalogUsageContext? usageContext)
    {
        if (usageContext?.EditingObjectId != null
            && application.ID == usageContext.EditingObjectId
            && usageContext.EditingEffectiveStored != null)
        {
            return usageContext.EditingEffectiveStored;
        }

        return application.BorderZoneLocation;
    }

    private static string? GetVisaBorderZoneStored(Visa visa, CatalogUsageContext? usageContext)
    {
        if (usageContext?.EditingObjectId != null
            && visa.ID == usageContext.EditingObjectId
            && usageContext.EditingEffectiveStored != null)
        {
            return usageContext.EditingEffectiveStored;
        }

        return visa.BorderZoneLocation;
    }

    private static string? GetInvitationBorderZoneStored(Invitation invitation, CatalogUsageContext? usageContext)
    {
        if (usageContext?.EditingObjectId != null
            && invitation.ID == usageContext.EditingObjectId
            && usageContext.EditingEffectiveStored != null)
        {
            return usageContext.EditingEffectiveStored;
        }

        return invitation.BorderZoneLocation;
    }

    private static string? GetWorkPermitItemWorkPermittedLocationsStored(
        WorkPermitItem item,
        CatalogUsageContext? usageContext)
    {
        if (usageContext?.EditingObjectId != null
            && item.ID == usageContext.EditingObjectId
            && usageContext.EditingEffectiveStored != null)
        {
            return usageContext.EditingEffectiveStored;
        }

        return item.WorkPermittedLocations;
    }

    private static string? GetApplicationWorkPermitLocationStored(
        ApplicationProfileInstance application,
        CatalogUsageContext? usageContext)
    {
        if (usageContext?.EditingObjectId != null
            && application.ID == usageContext.EditingObjectId
            && usageContext.EditingEffectiveStored != null)
        {
            return usageContext.EditingEffectiveStored;
        }

        return application.MovementPermitLocation;
    }

    private static void StripBorderZoneLabelFromAllItems(
        IObjectSpace objectSpace,
        string label,
        CatalogUsageContext? usageContext)
    {
        foreach (var application in objectSpace.GetObjectsQuery<ApplicationProfileInstance>().ToList())
        {
            var stored = GetApplicationBorderZoneStored(application, usageContext);
            if (!CommaSeparatedSelectionHelper.ContainsLabel(
                    stored,
                    label,
                    CommaSeparatedSelectionHelper.NoneValue))
            {
                continue;
            }

            var parsed = CommaSeparatedSelectionHelper.ParseSelected(
                    stored,
                    CommaSeparatedSelectionHelper.NoneValue)
                .Where(z => !string.Equals(z, label, StringComparison.OrdinalIgnoreCase));
            application.BorderZoneLocation = CommaSeparatedSelectionHelper.FormatSelected(
                parsed,
                CommaSeparatedSelectionHelper.NoneValue);
        }

        foreach (var visa in objectSpace.GetObjectsQuery<Visa>().ToList())
        {
            var stored = GetVisaBorderZoneStored(visa, usageContext);
            if (!CommaSeparatedSelectionHelper.ContainsLabel(stored, label, string.Empty))
            {
                continue;
            }

            var parsed = CommaSeparatedSelectionHelper.ParseSelected(stored, string.Empty)
                .Where(z => !string.Equals(z, label, StringComparison.OrdinalIgnoreCase));
            visa.BorderZoneLocation = CommaSeparatedSelectionHelper.FormatSelected(parsed, string.Empty);
        }

        foreach (var invitation in objectSpace.GetObjectsQuery<Invitation>().ToList())
        {
            var stored = GetInvitationBorderZoneStored(invitation, usageContext);
            if (!CommaSeparatedSelectionHelper.ContainsLabel(
                    stored,
                    label,
                    CommaSeparatedSelectionHelper.NoneValue))
            {
                continue;
            }

            var parsed = CommaSeparatedSelectionHelper.ParseSelected(
                    stored,
                    CommaSeparatedSelectionHelper.NoneValue)
                .Where(z => !string.Equals(z, label, StringComparison.OrdinalIgnoreCase));
            invitation.BorderZoneLocation = CommaSeparatedSelectionHelper.FormatSelected(
                parsed,
                CommaSeparatedSelectionHelper.NoneValue);
        }
    }

    private static void StripWorkPermittedLocationLabelFromAllItems(
        IObjectSpace objectSpace,
        string label,
        CatalogUsageContext? usageContext)
    {
        foreach (var item in objectSpace.GetObjectsQuery<WorkPermitItem>().ToList())
        {
            var stored = GetWorkPermitItemWorkPermittedLocationsStored(item, usageContext);
            if (!CommaSeparatedSelectionHelper.ContainsLabel(stored, label, string.Empty))
            {
                continue;
            }

            var parsed = CommaSeparatedSelectionHelper.ParseSelected(stored, string.Empty)
                .Where(z => !string.Equals(z, label, StringComparison.OrdinalIgnoreCase));
            item.WorkPermittedLocations = CommaSeparatedSelectionHelper.FormatSelected(parsed, string.Empty);
        }

        foreach (var application in objectSpace.GetObjectsQuery<ApplicationProfileInstance>().ToList())
        {
            var stored = GetApplicationWorkPermitLocationStored(application, usageContext);
            if (!CommaSeparatedSelectionHelper.ContainsLabel(stored, label, string.Empty))
                continue;

            var parsed = CommaSeparatedSelectionHelper.ParseSelected(stored, string.Empty)
                .Where(z => !string.Equals(z, label, StringComparison.OrdinalIgnoreCase));
            application.MovementPermitLocation = CommaSeparatedSelectionHelper.FormatSelected(parsed, string.Empty);
        }

        foreach (var profile in objectSpace.GetObjectsQuery<ApplicationProfile>().ToList())
        {
            if (!CommaSeparatedSelectionHelper.ContainsLabel(profile.DefaultWorkPermitLocation, label, string.Empty))
                continue;

            var parsed = CommaSeparatedSelectionHelper.ParseSelected(profile.DefaultWorkPermitLocation, string.Empty)
                .Where(z => !string.Equals(z, label, StringComparison.OrdinalIgnoreCase));
            profile.DefaultWorkPermitLocation = CommaSeparatedSelectionHelper.FormatSelected(parsed, string.Empty);
        }
    }

    private static void RenameBorderZoneOnAllItems(IObjectSpace objectSpace, string oldLabel, string newLabel)
    {
        var applications = objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Where(app => app.BorderZoneLocation != null && app.BorderZoneLocation.Contains(oldLabel))
            .ToList();

        foreach (var application in applications)
        {
            if (!CommaSeparatedSelectionHelper.ContainsLabel(
                    application.BorderZoneLocation,
                    oldLabel,
                    CommaSeparatedSelectionHelper.NoneValue))
            {
                continue;
            }

            application.BorderZoneLocation = CommaSeparatedSelectionHelper.ReplaceLabel(
                application.BorderZoneLocation,
                oldLabel,
                newLabel,
                CommaSeparatedSelectionHelper.NoneValue);
        }

        var visas = objectSpace.GetObjectsQuery<Visa>()
            .Where(v => v.BorderZoneLocation != null && v.BorderZoneLocation.Contains(oldLabel))
            .ToList();

        foreach (var visa in visas)
        {
            if (!CommaSeparatedSelectionHelper.ContainsLabel(visa.BorderZoneLocation, oldLabel, string.Empty))
            {
                continue;
            }

            visa.BorderZoneLocation = CommaSeparatedSelectionHelper.ReplaceLabel(
                visa.BorderZoneLocation,
                oldLabel,
                newLabel,
                string.Empty);
        }

        var invitations = objectSpace.GetObjectsQuery<Invitation>()
            .Where(i => i.BorderZoneLocation != null && i.BorderZoneLocation.Contains(oldLabel))
            .ToList();

        foreach (var invitation in invitations)
        {
            if (!CommaSeparatedSelectionHelper.ContainsLabel(
                    invitation.BorderZoneLocation,
                    oldLabel,
                    CommaSeparatedSelectionHelper.NoneValue))
            {
                continue;
            }

            invitation.BorderZoneLocation = CommaSeparatedSelectionHelper.ReplaceLabel(
                invitation.BorderZoneLocation,
                oldLabel,
                newLabel,
                CommaSeparatedSelectionHelper.NoneValue);
        }
    }

    private static void RenameWorkPermittedLocationOnAllItems(IObjectSpace objectSpace, string oldLabel, string newLabel)
    {
        var workPermitItems = objectSpace.GetObjectsQuery<WorkPermitItem>()
            .Where(wpi => wpi.WorkPermittedLocations != null && wpi.WorkPermittedLocations.Contains(oldLabel))
            .ToList();

        foreach (var item in workPermitItems)
        {
            if (!CommaSeparatedSelectionHelper.ContainsLabel(item.WorkPermittedLocations, oldLabel, string.Empty))
            {
                continue;
            }

            item.WorkPermittedLocations = CommaSeparatedSelectionHelper.ReplaceLabel(
                item.WorkPermittedLocations,
                oldLabel,
                newLabel,
                string.Empty);
        }

        var applications = objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Where(app => app.MovementPermitLocation != null && app.MovementPermitLocation.Contains(oldLabel))
            .ToList();

        foreach (var application in applications)
        {
            if (!CommaSeparatedSelectionHelper.ContainsLabel(application.MovementPermitLocation, oldLabel, string.Empty))
                continue;

            application.MovementPermitLocation = CommaSeparatedSelectionHelper.ReplaceLabel(
                application.MovementPermitLocation,
                oldLabel,
                newLabel,
                string.Empty);
        }

        var profiles = objectSpace.GetObjectsQuery<ApplicationProfile>()
            .Where(profile => profile.DefaultWorkPermitLocation != null && profile.DefaultWorkPermitLocation.Contains(oldLabel))
            .ToList();

        foreach (var profile in profiles)
        {
            if (!CommaSeparatedSelectionHelper.ContainsLabel(profile.DefaultWorkPermitLocation, oldLabel, string.Empty))
                continue;

            profile.DefaultWorkPermitLocation = CommaSeparatedSelectionHelper.ReplaceLabel(
                profile.DefaultWorkPermitLocation,
                oldLabel,
                newLabel,
                string.Empty);
        }
    }

    private static LookupBase? FindCatalogEntryByName(IObjectSpace objectSpace, Type catalogEntityType, string nameTm)
    {
        if (string.IsNullOrWhiteSpace(nameTm))
        {
            return null;
        }

        var trimmed = nameTm.Trim();

        if (catalogEntityType == typeof(BorderZoneName))
        {
            return objectSpace.GetObjects<BorderZoneName>()
                .FirstOrDefault(z => string.Equals(z.NameTm, trimmed, StringComparison.OrdinalIgnoreCase));
        }

        if (catalogEntityType == typeof(WorkPermittedLocationName))
        {
            return objectSpace.GetObjects<WorkPermittedLocationName>()
                .FirstOrDefault(z => string.Equals(z.NameTm, trimmed, StringComparison.OrdinalIgnoreCase));
        }

        if (!typeof(LookupBase).IsAssignableFrom(catalogEntityType))
        {
            return null;
        }

        return objectSpace.GetObjects(catalogEntityType)
            .Cast<LookupBase>()
            .FirstOrDefault(x => string.Equals(x.NameTm, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsValidCatalogName(string? name, string? noneValue) =>
        !string.IsNullOrWhiteSpace(name)
        && !CommaSeparatedSelectionHelper.IsNoneValue(name, noneValue);
}
