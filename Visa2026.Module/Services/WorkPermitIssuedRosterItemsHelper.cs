using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.Module.Services;

/// <summary>
/// When a work permit is issued from an <see cref="ApplicationProfileInstance"/>, ensure one
/// <see cref="WorkPermitItem"/> per roster employee (output lines on the letter).
/// </summary>
public static class WorkPermitIssuedRosterItemsHelper
{
    public static void EnsureRosterWorkPermitItems(WorkPermit? workPermit)
    {
        if (workPermit == null || workPermit.ApplicationProfileInstance == null)
            return;

        if (MigrationImportContext.IsDataImport)
            return;

        var objectSpace = ObjectSpaceHelper.Get(workPermit);
        if (objectSpace == null)
            return;

        var instance = objectSpace.GetObject(workPermit.ApplicationProfileInstance);
        if (instance == null)
            return;

        var defaultLocations = instance.MovementPermitLocation?.Trim() ?? string.Empty;

        foreach (var person in ApplicationRosterHelper.GetRosterPeople(instance).Where(p => p != null && p.IsEmployee))
        {
            if (person.ID == Guid.Empty)
                continue;

            var personId = person.ID;
            if (workPermit.WorkPermitItems?.Any(wpi => wpi.Person != null && wpi.Person.ID == personId) == true)
                continue;

            var trackedPerson = objectSpace.GetObject(person);
            if (trackedPerson == null)
                continue;

            var item = objectSpace.CreateObject<WorkPermitItem>();
            item.WorkPermit = workPermit;
            item.Person = trackedPerson;
            item.Passport = ApplicationProfileInstancePersonValidItems.ResolvePassport(trackedPerson);
            item.CurrentPositionHistory = PersonCurrentItems.GetCurrentPositionHistory(trackedPerson);
            item.WorkPermittedLocations = defaultLocations;

            var visa = PersonCurrentItems.GetCurrentVisa(trackedPerson);
            if (visa != null && visa.ExpirationDate.HasValue && visa.ExpirationDate.Value.Date >= DateTime.Today)
            {
                if (item.StartDate == default)
                    item.StartDate = visa.StartDate;
                if (item.ExpirationDate == default)
                    item.ExpirationDate = visa.ExpirationDate.Value;
            }

            workPermit.WorkPermitItems ??= new System.Collections.ObjectModel.ObservableCollection<WorkPermitItem>();
            workPermit.WorkPermitItems.Add(item);
        }
    }
}
