using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.PersonIntake;

public sealed class PersonIntakeSaveResult
{
    public bool Succeeded { get; init; }
    public string Message { get; init; }
    public IReadOnlyList<PersonIntakeFinding> Unresolved { get; init; } = Array.Empty<PersonIntakeFinding>();
}

public static class PersonIntakeWizardPersistHelper
{
    public static PersonIntakeSaveResult TrySave(
        IObjectSpace objectSpace,
        Person person,
        PersonIntakeWizardState state,
        string markedBy,
        PersonIntakeWizardAccess access = null)
    {
        if (objectSpace == null || person == null || state == null)
            return new PersonIntakeSaveResult { Succeeded = false, Message = PersonIntakeText.Get("Save.NothingToSave") };

        access ??= PersonIntakeWizardAccess.Unrestricted;
        if (access.IsViewOnly)
            return new PersonIntakeSaveResult { Succeeded = false, Message = PersonIntakeWizardAccess.ViewOnlyMessage };

        if (string.IsNullOrWhiteSpace(person.FirstName) || string.IsNullOrWhiteSpace(person.LastName))
            return new PersonIntakeSaveResult { Succeeded = false, Message = PersonIntakeText.Get("Save.NameRequired") };

        PersonIntakeDeferredRecords.EnsureCreateTimePending(person, state);
        PersonIntakeCompleteness.AcceptMissingAsNotReceived(person, state);
        var violation = access.FindSaveViolation(person, state);
        if (violation != null)
            return new PersonIntakeSaveResult { Succeeded = false, Message = violation };

        SyncPendingItems(objectSpace, person, state);

        if (!state.PendingKeys.Contains(PersonPendingTargets.Passport))
            SyncPassports(objectSpace, person, state.PassportDrafts);

        if (!state.PendingKeys.Contains(PersonPendingTargets.Education))
            SyncEducations(objectSpace, person, state.EducationDrafts);

        // Medical records are added later from their own navigation. Do not sync or delete them here.

        if (!state.PendingKeys.Contains(PersonPendingTargets.AddressOfResidence))
            SyncResidence(objectSpace, person, state.ResidenceDrafts);

        if (!state.PendingKeys.Contains(PersonPendingTargets.PersonDocument))
            SyncDocuments(objectSpace, person, state.DocumentDrafts);

        // Travel history is added later from its own navigation. Do not sync or delete it here.

        if (!state.PendingKeys.Contains(PersonPendingTargets.EmployeeSalary))
            SyncSalaries(objectSpace, person, state.SalaryDrafts);

        if (!state.PendingKeys.Contains(PersonPendingTargets.EmployeePositionHistory))
            SyncPositions(objectSpace, person, state.PositionDrafts);

        if (!state.PendingKeys.Contains(PersonPendingTargets.WorkDuty))
            SyncWorkDuties(objectSpace, person, state.WorkDutyDrafts);

        ApplyOtherDrafts(objectSpace, person, state);

        // Clear from the marks being saved. Rows left over from the previous save are
        // deleted above and must not wipe a date, lookup, or address the officer just entered.
        PersonIntakeCompleteness.ClearPendingScalars(person, state.PendingKeys);

        var unresolved = PersonIntakeCompleteness
            .Evaluate(person, state.PendingKeys, state.DraftSatisfiedKeys())
            .Where(f => f.State == PersonIntakeItemState.Unresolved)
            .ToList();
        if (unresolved.Count > 0)
        {
            return new PersonIntakeSaveResult
            {
                Succeeded = false,
                Message = PersonIntakeText.Format("Save.Unresolved", unresolved.Count),
                Unresolved = unresolved
            };
        }

        var pending = PersonIntakeCompleteness
            .Evaluate(person, state.PendingKeys)
            .Where(f => f.State == PersonIntakeItemState.Pending)
            .ToList();

        if (pending.Count > 0)
            person.IncompleteNotes = string.IsNullOrWhiteSpace(state.FollowNote) ? null : state.FollowNote.Trim();
        else
            person.IncompleteNotes = null;

        PersonPendingRollup.Apply(person, replaceLegacy: true, markedBy ?? string.Empty);

        objectSpace.CommitChanges();
        // Commit can leave the aggregated collection empty even though the rows were written.
        // Put the committed rows back so the wizard still treats those fields as Not received.
        AttachCommittedPendingItems(objectSpace, person);
        return new PersonIntakeSaveResult { Succeeded = true, Message = pending.Count > 0 ? PersonIntakeText.Get("Save.SavedIncomplete") : PersonIntakeText.Get("Save.SavedComplete") };
    }

    private static void SyncPassports(IObjectSpace objectSpace, Person person, List<PersonIntakePassportDraft> drafts)
    {
        person.Passports ??= new System.Collections.ObjectModel.ObservableCollection<Passport>();
        var keep = new HashSet<Guid>();
        foreach (var draft in drafts.Where(d => d != null && (d.HasContent || d.ExistingId.HasValue)))
        {
            Passport passport;
            if (draft.ExistingId is Guid existingId)
            {
                passport = person.Passports.FirstOrDefault(p => p != null && p.ID == existingId)
                    ?? objectSpace.GetObjectByKey<Passport>(existingId);
                if (passport == null)
                    continue;
                keep.Add(passport.ID);
            }
            else
            {
                passport = objectSpace.CreateObject<Passport>();
                passport.Person = person;
            }

            passport.PassportNumber = string.IsNullOrWhiteSpace(draft.PassportNumber) ? null : draft.PassportNumber.Trim();
            passport.Authority = string.IsNullOrWhiteSpace(draft.Authority) ? null : draft.Authority.Trim();
            passport.IssueDate = draft.IssueDate;
            passport.ExpirationDate = draft.ExpirationDate;
            passport.IsCancelled = draft.IsCancelled;
            passport.PassportType = draft.PassportTypeId is Guid typeId
                ? objectSpace.GetObjectByKey<PassportType>(typeId)
                : null;
            passport.IssuedCountry = draft.IssuedCountryId is Guid countryId
                ? objectSpace.GetObjectByKey<Country>(countryId)
                : null;

            AttachNewCopies(objectSpace, draft.Copies, (file) =>
            {
                passport.Documents ??= new System.Collections.ObjectModel.ObservableCollection<PassportDocument>();
                var doc = objectSpace.CreateObject<PassportDocument>();
                doc.Passport = passport;
                doc.File = file;
                passport.Documents.Add(doc);
            }, "passport-copy.pdf");

            if (!draft.ExistingId.HasValue)
                keep.Add(passport.ID);
            else
                keep.Add(passport.ID);
        }

        foreach (var existing in person.Passports.Where(p => p != null && !keep.Contains(p.ID)).ToList())
            objectSpace.Delete(existing);
    }

    private static void SyncEducations(IObjectSpace objectSpace, Person person, List<PersonIntakeEducationDraft> drafts)
    {
        person.Educations ??= new System.Collections.ObjectModel.ObservableCollection<Education>();
        var keep = new HashSet<Guid>();
        foreach (var draft in drafts.Where(d => d != null && (d.HasContent || d.ExistingId.HasValue)))
        {
            Education education;
            if (draft.ExistingId is Guid existingId)
            {
                education = person.Educations.FirstOrDefault(e => e != null && e.ID == existingId)
                    ?? objectSpace.GetObjectByKey<Education>(existingId);
                if (education == null)
                    continue;
            }
            else
            {
                education = objectSpace.CreateObject<Education>();
                education.Person = person;
            }

            education.GraduationYear = string.IsNullOrWhiteSpace(draft.GraduationYear) ? null : draft.GraduationYear.Trim();
            education.EducationLevel = draft.EducationLevelId is Guid levelId
                ? objectSpace.GetObjectByKey<EducationLevel>(levelId)
                : null;
            education.EducationInstitution = draft.EducationInstitutionId is Guid institutionId
                ? objectSpace.GetObjectByKey<EducationInstitution>(institutionId)
                : null;
            education.EducationCountry = draft.EducationCountryId is Guid countryId
                ? objectSpace.GetObjectByKey<Country>(countryId)
                : null;
            education.Specialty = draft.SpecialtyId is Guid specialtyId
                ? objectSpace.GetObjectByKey<Specialty>(specialtyId)
                : null;

            AttachNewCopies(objectSpace, draft.Copies, (file) =>
            {
                education.Documents ??= new System.Collections.ObjectModel.ObservableCollection<EducationDocument>();
                var doc = objectSpace.CreateObject<EducationDocument>();
                doc.Education = education;
                doc.File = file;
                education.Documents.Add(doc);
            }, "diploma-copy.pdf");

            keep.Add(education.ID);
        }

        foreach (var existing in person.Educations.Where(e => e != null && !keep.Contains(e.ID)).ToList())
            objectSpace.Delete(existing);
    }

    private static void SyncMedical(IObjectSpace objectSpace, Person person, List<PersonIntakeMedicalDraft> drafts)
    {
        person.MedicalRecords ??= new System.Collections.ObjectModel.ObservableCollection<MedicalRecord>();
        var keep = new HashSet<Guid>();
        foreach (var draft in drafts.Where(d => d != null && (d.HasContent || d.ExistingId.HasValue)))
        {
            MedicalRecord medical;
            if (draft.ExistingId is Guid existingId)
            {
                medical = person.MedicalRecords.FirstOrDefault(m => m != null && m.ID == existingId)
                    ?? objectSpace.GetObjectByKey<MedicalRecord>(existingId);
                if (medical == null)
                    continue;
            }
            else
            {
                medical = objectSpace.CreateObject<MedicalRecord>();
                medical.Person = person;
            }

            medical.DocumentNumber = string.IsNullOrWhiteSpace(draft.DocumentNumber) ? null : draft.DocumentNumber.Trim();
            medical.IssueDate = draft.IssueDate ?? DateTime.Today;
            medical.ValidityDuration = draft.ValidityDurationId is Guid durationId
                ? objectSpace.GetObjectByKey<ValidityDuration>(durationId)
                : medical.ValidityDuration;

            AttachNewCopies(objectSpace, draft.Copies, (file) =>
            {
                medical.Documents ??= new System.Collections.ObjectModel.ObservableCollection<MedicalRecordDocument>();
                var doc = objectSpace.CreateObject<MedicalRecordDocument>();
                doc.MedicalRecord = medical;
                doc.File = file;
                medical.Documents.Add(doc);
            }, "medical-copy.pdf");

            keep.Add(medical.ID);
        }

        foreach (var existing in person.MedicalRecords.Where(m => m != null && !keep.Contains(m.ID)).ToList())
            objectSpace.Delete(existing);
    }

    private static void SyncResidence(IObjectSpace objectSpace, Person person, List<PersonIntakeResidenceDraft> drafts)
    {
        person.AddressesOfResidence ??= new System.Collections.ObjectModel.ObservableCollection<AddressOfResidence>();
        var keep = new HashSet<Guid>();
        foreach (var draft in drafts.Where(d => d != null && (d.HasContent || d.ExistingId.HasValue)))
        {
            AddressOfResidence address;
            if (draft.ExistingId is Guid existingId)
            {
                address = person.AddressesOfResidence.FirstOrDefault(a => a != null && a.ID == existingId)
                    ?? objectSpace.GetObjectByKey<AddressOfResidence>(existingId);
                if (address == null)
                    continue;
            }
            else
            {
                address = objectSpace.CreateObject<AddressOfResidence>();
                address.Person = person;
            }

            address.Type = draft.Type ?? ResidenceType.PrivateHouse;
            address.Region = draft.RegionId is Guid regionId
                ? objectSpace.GetObjectByKey<Region>(regionId)
                : null;
            address.City = draft.CityId is Guid cityId
                ? objectSpace.GetObjectByKey<City>(cityId)
                : null;
            address.FullAddress = string.IsNullOrWhiteSpace(draft.FullAddress) ? null : draft.FullAddress.Trim();
            address.ExpirationDate = draft.ExpirationDate;
            address.Lodging = draft.LodgingId is Guid lodgingId
                ? objectSpace.GetObjectByKey<Lodging>(lodgingId)
                : null;
            address.Hotel = draft.HotelId is Guid hotelId
                ? objectSpace.GetObjectByKey<Hotel>(hotelId)
                : null;
            address.Hospital = draft.HospitalId is Guid hospitalId
                ? objectSpace.GetObjectByKey<Hospital>(hospitalId)
                : null;
            address.OtherSite = draft.OtherSiteId is Guid otherId
                ? objectSpace.GetObjectByKey<OtherSite>(otherId)
                : null;

            AttachNewCopies(objectSpace, draft.Copies, (file) =>
            {
                address.Documents ??= new System.Collections.ObjectModel.ObservableCollection<AddressOfResidenceDocument>();
                var doc = objectSpace.CreateObject<AddressOfResidenceDocument>();
                doc.AddressOfResidence = address;
                doc.File = file;
                address.Documents.Add(doc);
            }, "address-copy.pdf");

            keep.Add(address.ID);
        }

        foreach (var existing in person.AddressesOfResidence.Where(a => a != null && !keep.Contains(a.ID)).ToList())
            objectSpace.Delete(existing);
    }

    private static void AttachNewCopies(
        IObjectSpace objectSpace,
        List<PersonIntakeDocumentCopyDraft> copies,
        Action<DevExpress.Persistent.BaseImpl.EF.FileData> attach,
        string fallbackName)
    {
        if (copies == null)
            return;

        foreach (var copy in copies)
        {
            if (copy == null || copy.IsExisting || copy.Content == null || copy.Content.Length == 0)
                continue;
            attach(CreateFileData(objectSpace, copy, fallbackName));
        }
    }


    private static void SyncDocuments(IObjectSpace objectSpace, Person person, List<PersonIntakePersonDocumentDraft> drafts)
    {
        person.Documents ??= new System.Collections.ObjectModel.ObservableCollection<PersonDocument>();
        var keep = new HashSet<Guid>();
        foreach (var draft in drafts.Where(d => d != null && (d.HasContent || d.ExistingId.HasValue)))
        {
            PersonDocument document;
            if (draft.ExistingId is Guid existingId)
            {
                document = person.Documents.FirstOrDefault(d => d != null && d.ID == existingId)
                    ?? objectSpace.GetObjectByKey<PersonDocument>(existingId);
                if (document == null)
                    continue;
            }
            else
            {
                if (draft.Content == null || draft.Content.Length == 0)
                    continue;
                document = objectSpace.CreateObject<PersonDocument>();
                document.Person = person;
                var file = objectSpace.CreateObject<DevExpress.Persistent.BaseImpl.EF.FileData>();
                file.FileName = string.IsNullOrWhiteSpace(draft.FileName) ? "person-document.pdf" : System.IO.Path.GetFileName(draft.FileName.Trim());
                file.Content = draft.Content;
                file.Size = draft.Content.Length;
                document.File = file;
            }

            keep.Add(document.ID);
        }

        foreach (var existing in person.Documents.Where(d => d != null && !keep.Contains(d.ID)).ToList())
            objectSpace.Delete(existing);
    }

    private static void SyncTravels(IObjectSpace objectSpace, Person person, List<PersonIntakeTravelDraft> drafts)
    {
        person.TravelHistories ??= new System.Collections.ObjectModel.ObservableCollection<TravelHistory>();
        var keep = new HashSet<Guid>();
        foreach (var draft in drafts.Where(d => d != null && (d.HasContent || d.ExistingId.HasValue)))
        {
            TravelHistory travel;
            if (draft.ExistingId is Guid existingId)
            {
                travel = person.TravelHistories.FirstOrDefault(t => t != null && t.ID == existingId)
                    ?? objectSpace.GetObjectByKey<TravelHistory>(existingId);
                if (travel == null)
                    continue;
                ApplyTravelFields(objectSpace, travel, draft);
            }
            else
            {
                travel = CreateTravelRow(objectSpace, draft);
                travel.Person = person;
                ApplyTravelFields(objectSpace, travel, draft);
            }

            keep.Add(travel.ID);
        }

        foreach (var existing in person.TravelHistories.Where(t => t != null && !keep.Contains(t.ID)).ToList())
            objectSpace.Delete(existing);
    }

    private static TravelHistory CreateTravelRow(IObjectSpace objectSpace, PersonIntakeTravelDraft draft)
    {
        var travelType = draft.TravelType ?? TravelType.External;
        var movement = draft.MovementType ?? MovementType.Entry;
        return (travelType, movement) switch
        {
            (TravelType.External, MovementType.Entry) => objectSpace.CreateObject<ExternalArrival>(),
            (TravelType.External, MovementType.Exit) => objectSpace.CreateObject<ExternalDeparture>(),
            (TravelType.Internal, MovementType.Entry) => objectSpace.CreateObject<InternalArrival>(),
            _ => objectSpace.CreateObject<InternalDeparture>()
        };
    }

    private static void ApplyTravelFields(IObjectSpace objectSpace, TravelHistory travel, PersonIntakeTravelDraft draft)
    {
        var travelType = draft.TravelType ?? TravelType.External;
        var movement = draft.MovementType ?? MovementType.Entry;
        travel.TravelType = travelType;
        travel.MovementType = movement;
        travel.TravelDate = draft.TravelDate ?? DateTime.Today;
        travel.Notes = string.IsNullOrWhiteSpace(draft.Notes) ? null : draft.Notes.Trim();

        if (travelType == TravelType.External)
        {
            travel.CheckPoint = draft.CheckPointId is Guid checkPointId
                ? objectSpace.GetObjectByKey<CheckPoint>(checkPointId)
                : null;
            travel.Country = draft.CountryId is Guid countryId
                ? objectSpace.GetObjectByKey<Country>(countryId)
                : null;
            travel.Region = null;
            travel.City = null;
        }
        else
        {
            travel.Region = draft.RegionId is Guid regionId
                ? objectSpace.GetObjectByKey<Region>(regionId)
                : null;
            travel.City = draft.CityId is Guid cityId
                ? objectSpace.GetObjectByKey<City>(cityId)
                : null;
            travel.CheckPoint = null;
            travel.Country = null;
        }
    }

    private static void SyncSalaries(IObjectSpace objectSpace, Person person, List<PersonIntakeSalaryDraft> drafts)
    {
        person.Salaries ??= new System.Collections.ObjectModel.ObservableCollection<EmployeeSalary>();
        var keep = new HashSet<Guid>();
        foreach (var draft in drafts.Where(d => d != null && (d.HasContent || d.ExistingId.HasValue)))
        {
            EmployeeSalary salary;
            if (draft.ExistingId is Guid existingId)
            {
                salary = person.Salaries.FirstOrDefault(s => s != null && s.ID == existingId)
                    ?? objectSpace.GetObjectByKey<EmployeeSalary>(existingId);
                if (salary == null)
                    continue;
            }
            else
            {
                salary = objectSpace.CreateObject<EmployeeSalary>();
                salary.Person = person;
            }

            salary.Amount = string.IsNullOrWhiteSpace(draft.Amount) ? null : draft.Amount.Trim();
            salary.StartDate = draft.StartDate ?? DateTime.Today;
            salary.EndDate = draft.EndDate;
            keep.Add(salary.ID);
        }

        foreach (var existing in person.Salaries.Where(s => s != null && !keep.Contains(s.ID)).ToList())
            objectSpace.Delete(existing);
    }

    private static void SyncWorkDuties(IObjectSpace objectSpace, Person person, List<PersonIntakeWorkDutyDraft> drafts)
    {
        person.WorkDuties ??= new System.Collections.ObjectModel.ObservableCollection<WorkDuty>();
        var keep = new HashSet<Guid>();
        foreach (var draft in drafts.Where(d => d != null && (d.HasContent || d.ExistingId.HasValue)))
        {
            WorkDuty duty;
            if (draft.ExistingId is Guid existingId)
            {
                duty = person.WorkDuties.FirstOrDefault(item => item != null && item.ID == existingId)
                    ?? objectSpace.GetObjectByKey<WorkDuty>(existingId);
                if (duty == null)
                    continue;
            }
            else
            {
                duty = objectSpace.CreateObject<WorkDuty>();
                duty.Person = person;
            }

            duty.Description = draft.Description?.Trim() ?? string.Empty;
            keep.Add(duty.ID);
        }

        foreach (var existing in person.WorkDuties.Where(item => item != null && !keep.Contains(item.ID)).ToList())
            objectSpace.Delete(existing);
    }

    private static void ApplyOtherDrafts(IObjectSpace objectSpace, Person person, PersonIntakeWizardState state)
    {
        var other = state.OtherDrafts;
        if (other == null)
            return;

        if (!state.PendingKeys.Contains(PersonPendingTargets.PersonFamilyRelationDocument)
            && other.FamilyRelationDocuments.Count > 0)
        {
            person.FamilyRelationDocuments ??= new System.Collections.ObjectModel.ObservableCollection<PersonFamilyRelationDocument>();
            foreach (var copy in other.FamilyRelationDocuments.Where(c => c != null && !c.IsExisting))
                AttachFamilyRelationDocument(objectSpace, person, copy);
        }


    }

    private static void SyncPositions(IObjectSpace objectSpace, Person person, List<PersonIntakePositionDraft> drafts)
    {
        person.PositionHistory ??= new System.Collections.ObjectModel.ObservableCollection<EmployeePositionHistory>();
        var keep = new HashSet<Guid>();
        foreach (var draft in drafts.Where(d => d != null && (d.HasContent || d.ExistingId.HasValue)))
        {
            EmployeePositionHistory row;
            if (draft.ExistingId is Guid existingId)
            {
                row = person.PositionHistory.FirstOrDefault(p => p != null && p.ID == existingId)
                    ?? objectSpace.GetObjectByKey<EmployeePositionHistory>(existingId);
                if (row == null)
                    continue;
            }
            else
            {
                row = objectSpace.CreateObject<EmployeePositionHistory>();
                row.Person = person;
            }

            if (draft.StartDate.HasValue)
                row.StartDate = draft.StartDate.Value;
            row.EndDate = draft.EndDate;
            row.Position = draft.PositionId is Guid positionId
                ? objectSpace.GetObjectByKey<Position>(positionId)
                : null;
            row.ActualPosition = draft.ActualPositionId is Guid actualId
                ? objectSpace.GetObjectByKey<ActualPosition>(actualId)
                : null;
            row.Department = draft.DepartmentId is Guid departmentId
                ? objectSpace.GetObjectByKey<Department>(departmentId)
                : null;

            row.Documents ??= new System.Collections.ObjectModel.ObservableCollection<EmployeePositionHistoryDocument>();
            foreach (var copy in draft.OrderDocuments.Where(c => c != null && !c.IsExisting))
            {
                if (copy.Content == null || copy.Content.Length == 0)
                    continue;
                var file = CreateFileData(objectSpace, copy, "order-for-assignment.pdf");
                var doc = objectSpace.CreateObject<EmployeePositionHistoryDocument>();
                doc.EmployeePositionHistory = row;
                doc.File = file;
                row.Documents.Add(doc);
            }

            keep.Add(row.ID);
        }

        foreach (var existing in person.PositionHistory.Where(p => p != null && !keep.Contains(p.ID)).ToList())
            objectSpace.Delete(existing);
    }
    private static void AttachFamilyRelationDocument(IObjectSpace objectSpace, Person person, PersonIntakeDocumentCopyDraft copy)
    {
        if (copy?.Content == null || copy.Content.Length == 0)
            return;
        var file = CreateFileData(objectSpace, copy, "family-relation.pdf");
        var doc = objectSpace.CreateObject<PersonFamilyRelationDocument>();
        doc.Person = person;
        doc.File = file;
        person.FamilyRelationDocuments.Add(doc);
    }

    private static DevExpress.Persistent.BaseImpl.EF.FileData CreateFileData(
        IObjectSpace objectSpace,
        PersonIntakeDocumentCopyDraft copy,
        string fallbackName)
    {
        var file = objectSpace.CreateObject<DevExpress.Persistent.BaseImpl.EF.FileData>();
        file.FileName = string.IsNullOrWhiteSpace(copy.FileName)
            ? fallbackName
            : System.IO.Path.GetFileName(copy.FileName.Trim());
        file.Content = copy.Content;
        file.Size = copy.Content.Length;
        return file;
    }

    private static void SyncPendingItems(IObjectSpace objectSpace, Person person, PersonIntakeWizardState state)
    {
        person.PendingItems ??= new System.Collections.ObjectModel.ObservableCollection<PersonPendingItem>();

        // Keep unchanged rows so a role without Create/Delete on PersonPendingItem can still save the person.
        var kept = new HashSet<string>(StringComparer.Ordinal);
        foreach (var existing in person.PendingItems.Where(item => item != null).ToList())
        {
            var key = existing.TargetKey;
            if (!string.IsNullOrEmpty(key)
                && state.PendingKeys.Contains(key)
                && PersonIntakeAreaMap.TryGetArea(key, out var area)
                && kept.Add(key))
            {
                if (!string.Equals(existing.AreaKey, area, StringComparison.Ordinal))
                    existing.AreaKey = area;
                var note = PendingNote(state, key);
                if (!string.Equals(existing.Note, note, StringComparison.Ordinal))
                    existing.Note = note;
                continue;
            }

            objectSpace.Delete(existing);
            person.PendingItems.Remove(existing);
        }

        foreach (var key in state.PendingKeys)
        {
            if (kept.Contains(key) || !PersonIntakeAreaMap.TryGetArea(key, out var area))
                continue;

            var row = objectSpace.CreateObject<PersonPendingItem>();
            row.Person = person;
            row.TargetKey = key;
            row.AreaKey = area;
            row.Note = PendingNote(state, key);
            if (!person.PendingItems.Contains(row))
                person.PendingItems.Add(row);
        }
    }

    private static void AttachCommittedPendingItems(IObjectSpace objectSpace, Person person)
    {
        if (objectSpace == null || person == null || person.ID == Guid.Empty)
            return;

        person.PendingItems ??= new ObservableCollection<PersonPendingItem>();
        var rows = objectSpace
            .GetObjects<PersonPendingItem>(CriteriaOperator.Parse("[Person.ID] = ?", person.ID))
            .Cast<PersonPendingItem>()
            .Where(row => row != null)
            .ToList();
        if (rows.Count == 0)
            return;

        var ids = new HashSet<Guid>(rows.Select(row => row.ID));
        foreach (var stale in person.PendingItems.Where(item => item == null || !ids.Contains(item.ID)).ToList())
            person.PendingItems.Remove(stale);
        foreach (var row in rows)
        {
            if (!person.PendingItems.Any(item => item != null && item.ID == row.ID))
                person.PendingItems.Add(row);
        }
    }

    private static string PendingNote(PersonIntakeWizardState state, string key) =>
        state.RecordNotes.TryGetValue(key, out var note) && !string.IsNullOrWhiteSpace(note)
            ? note.Trim()
            : null;
}