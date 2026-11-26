using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.PersonLinkedDocuments;

namespace Visa2026.Module.Services.MedicalRecordWorkspace;

public static class MedicalRecordWorkspaceService
{
    public const int ListLimit = 400;
    public const int PersonHitLimit = 25;

    public static MedicalRecordListResult List(IObjectSpace objectSpace, string search)
    {
        var query = objectSpace.GetObjectsQuery<MedicalRecord>()
            .Include(record => record.Person)
            .Include(record => record.ValidityDuration)
            .Where(record => record.Person != null);

        var pattern = LikePattern(search);
        if (pattern != null)
        {
            query = query.Where(record =>
                (record.DocumentNumber != null && EF.Functions.ILike(record.DocumentNumber, pattern))
                || (record.Person.FirstName != null && EF.Functions.ILike(record.Person.FirstName, pattern))
                || (record.Person.LastName != null && EF.Functions.ILike(record.Person.LastName, pattern))
                || (record.Person.MiddleName != null && EF.Functions.ILike(record.Person.MiddleName, pattern))
                || (record.Person.PersonalNumber != null && EF.Functions.ILike(record.Person.PersonalNumber, pattern)));
        }

        var loaded = query
            .OrderByDescending(record => record.IssueDate)
            .ThenBy(record => record.DocumentNumber)
            .Take(ListLimit + 1)
            .ToList();

        var truncated = loaded.Count > ListLimit;
        if (truncated)
            loaded = loaded.Take(ListLimit).ToList();

        var ids = loaded.Select(record => record.ID).ToList();
        var withCopies = ids.Count == 0
            ? new HashSet<Guid>()
            : objectSpace.GetObjectsQuery<MedicalRecordDocument>()
                .Where(document => document.MedicalRecord != null
                    && ids.Contains(document.MedicalRecord.ID)
                    && document.File != null
                    && document.File.Size > 0)
                .Select(document => document.MedicalRecord.ID)
                .Distinct()
                .ToHashSet();

        return new MedicalRecordListResult
        {
            Truncated = truncated,
            Rows = loaded.Select(record => ToRow(record, withCopies.Contains(record.ID))).ToList()
        };
    }

    public static IReadOnlyList<MedicalRecordPersonHit> SearchPeople(IObjectSpace objectSpace, string search)
    {
        var pattern = LikePattern(search);
        if (pattern == null)
            return Array.Empty<MedicalRecordPersonHit>();

        return objectSpace.GetObjectsQuery<Person>()
            .Where(person =>
                (person.FirstName != null && EF.Functions.ILike(person.FirstName, pattern))
                || (person.LastName != null && EF.Functions.ILike(person.LastName, pattern))
                || (person.MiddleName != null && EF.Functions.ILike(person.MiddleName, pattern))
                || (person.PersonalNumber != null && EF.Functions.ILike(person.PersonalNumber, pattern)))
            .OrderBy(person => person.LastName)
            .ThenBy(person => person.FirstName)
            .Take(PersonHitLimit)
            .ToList()
            .Select(person => new MedicalRecordPersonHit
            {
                Id = person.ID,
                Name = PersonName(person),
                Role = RoleLabel(person.PersonRole),
                PersonalNumber = person.PersonalNumber ?? string.Empty
            })
            .ToList();
    }

    public static IReadOnlyList<MedicalRecordValidityOption> ListValidity(IObjectSpace objectSpace) =>
        objectSpace.GetObjectsQuery<ValidityDuration>()
            .OrderBy(item => item.NumberOfDays)
            .ThenBy(item => item.NameTm)
            .ToList()
            .Select(item => new MedicalRecordValidityOption
            {
                Id = item.ID,
                Caption = ValidityCaption(item),
                NumberOfDays = item.NumberOfDays
            })
            .ToList();

    public static MedicalRecordDetailDto Load(IObjectSpace objectSpace, Guid id)
    {
        var record = objectSpace.GetObjectsQuery<MedicalRecord>()
            .Include(item => item.Person)
            .Include(item => item.ValidityDuration)
            .Include(item => item.Documents)
            .ThenInclude(document => document.File)
            .FirstOrDefault(item => item.ID == id);

        if (record == null)
            return null;

        return ToDetail(record);
    }

    public static bool TrySave(IObjectSpace objectSpace, MedicalRecordSaveRequest request, out Guid id, out string error)
    {
        id = Guid.Empty;
        error = null;
        if (request == null)
        {
            error = "Nothing to save.";
            return false;
        }

        if (request.PersonId == Guid.Empty)
        {
            error = "Choose a person.";
            return false;
        }

        var documentNumber = (request.DocumentNumber ?? string.Empty).Trim();
        if (documentNumber.Length == 0)
        {
            error = "Document number is required.";
            return false;
        }

        if (documentNumber.Length > 50)
        {
            error = "Document number must be 50 characters or fewer.";
            return false;
        }

        if (request.IssueDate == default)
        {
            error = "Issue date is required.";
            return false;
        }

        if (request.ValidityDurationId == Guid.Empty)
        {
            error = "Validity is required.";
            return false;
        }

        var adds = request.AddScans ?? Array.Empty<MedicalRecordFileChange>();
        var replaces = (request.ReplaceScans ?? Array.Empty<MedicalRecordFileChange>())
            .Where(file => file != null && file.ScanId != Guid.Empty)
            .ToList();
        foreach (var file in adds.Concat(replaces))
        {
            var fileError = PersonLinkedDocumentUploadService.ValidateBytes(
                objectSpace,
                file?.FileName,
                file?.Content);
            if (!string.IsNullOrEmpty(fileError))
            {
                error = fileError;
                return false;
            }
        }

        try
        {
            var person = objectSpace.GetObjectByKey<Person>(request.PersonId);
            if (person == null)
            {
                error = "The person could not be found.";
                return false;
            }

            var validity = objectSpace.GetObjectByKey<ValidityDuration>(request.ValidityDurationId);
            if (validity == null)
            {
                error = "The validity could not be found.";
                return false;
            }

            MedicalRecord record;
            if (request.Id is Guid existingId && existingId != Guid.Empty)
            {
                record = objectSpace.GetObjectByKey<MedicalRecord>(existingId);
                if (record == null)
                {
                    error = "The medical record could not be found.";
                    return false;
                }
            }
            else
            {
                record = objectSpace.CreateObject<MedicalRecord>();
            }

            record.Person = person;
            record.DocumentNumber = documentNumber;
            record.ValidityDuration = validity;
            record.IssueDate = request.IssueDate.Date;

            var removeIds = new HashSet<Guid>(request.RemoveScanIds ?? Array.Empty<Guid>());
            foreach (var scanId in removeIds)
                DeleteScan(objectSpace, record, scanId);

            foreach (var replacement in replaces.Where(file => !removeIds.Contains(file.ScanId)))
                ReplaceScan(objectSpace, record, replacement);

            record.Documents ??= new System.Collections.ObjectModel.ObservableCollection<MedicalRecordDocument>();
            foreach (var added in adds.Where(file => file?.Content != null && file.Content.Length > 0))
            {
                var file = CreateFile(objectSpace, added.FileName, added.Content);
                var document = objectSpace.CreateObject<MedicalRecordDocument>();
                document.MedicalRecord = record;
                document.File = file;
                record.Documents.Add(document);
            }

            objectSpace.CommitChanges();
            id = record.ID;
            return true;
        }
        catch (ValidationException ex)
        {
            error = string.IsNullOrWhiteSpace(ex.Message)
                ? "The medical record could not be saved."
                : ex.Message;
            return false;
        }
        catch (UserFriendlySecurityException ex)
        {
            error = string.IsNullOrWhiteSpace(ex.Message)
                ? "You do not have permission to change medical records."
                : ex.Message;
            return false;
        }
    }

    private static void DeleteScan(IObjectSpace objectSpace, MedicalRecord record, Guid scanId)
    {
        var document = record.Documents?.FirstOrDefault(item => item != null && item.ID == scanId)
            ?? objectSpace.GetObjectByKey<MedicalRecordDocument>(scanId);
        if (document == null || document.MedicalRecord == null || document.MedicalRecord.ID != record.ID)
            return;

        var file = document.File;
        record.Documents?.Remove(document);
        objectSpace.Delete(document);
        if (file != null)
            objectSpace.Delete(file);
    }

    private static void ReplaceScan(IObjectSpace objectSpace, MedicalRecord record, MedicalRecordFileChange replacement)
    {
        var document = record.Documents?.FirstOrDefault(item => item != null && item.ID == replacement.ScanId)
            ?? objectSpace.GetObjectByKey<MedicalRecordDocument>(replacement.ScanId);
        if (document == null || document.MedicalRecord == null || document.MedicalRecord.ID != record.ID)
            return;

        var file = document.File ?? objectSpace.CreateObject<FileData>();
        file.FileName = SafeFileName(replacement.FileName);
        file.Content = replacement.Content;
        file.Size = replacement.Content.Length;
        document.File = file;
    }

    private static FileData CreateFile(IObjectSpace objectSpace, string fileName, byte[] content)
    {
        var file = objectSpace.CreateObject<FileData>();
        file.FileName = SafeFileName(fileName);
        file.Content = content;
        file.Size = content.Length;
        return file;
    }

    private static string SafeFileName(string fileName)
    {
        var name = string.IsNullOrWhiteSpace(fileName) ? "scan.pdf" : Path.GetFileName(fileName.Trim());
        return string.IsNullOrWhiteSpace(name) ? "scan.pdf" : name;
    }

    private static MedicalRecordListRow ToRow(MedicalRecord record, bool hasCopies)
    {
        DateTime? expiration = record.ExpirationDate;
        return new MedicalRecordListRow
        {
            Id = record.ID,
            PersonId = record.Person?.ID ?? Guid.Empty,
            HasCopies = hasCopies,
            PersonName = record.Person == null ? string.Empty : PersonName(record.Person),
            PersonRole = record.Person == null ? string.Empty : RoleLabel(record.Person.PersonRole),
            DocumentNumber = record.DocumentNumber ?? string.Empty,
            IssueDate = record.IssueDate,
            Validity = ValidityCaption(record.ValidityDuration),
            ExpirationDate = expiration,
            DaysRemaining = expiration.HasValue
                ? ExpirationLogicHelper.CalculateDaysRemaining(expiration)
                : null
        };
    }

    private static MedicalRecordDetailDto ToDetail(MedicalRecord record) =>
        new()
        {
            Id = record.ID,
            PersonId = record.Person?.ID ?? Guid.Empty,
            PersonName = record.Person == null ? string.Empty : PersonName(record.Person),
            DocumentNumber = record.DocumentNumber ?? string.Empty,
            IssueDate = record.IssueDate,
            ValidityDurationId = record.ValidityDuration?.ID,
            ExpirationDate = record.ExpirationDate,
            Scans = (record.Documents ?? new System.Collections.Generic.List<MedicalRecordDocument>())
                .Where(document => document != null)
                .OrderBy(document => document.File?.FileName)
                .Select(document => new MedicalRecordScanDto
                {
                    Id = document.ID,
                    FileId = document.File?.ID ?? Guid.Empty,
                    HasContent = document.File != null && document.File.Size > 0,
                    FileName = string.IsNullOrWhiteSpace(document.File?.FileName)
                        ? "document"
                        : document.File.FileName
                })
                .ToList()
        };

    private static string PersonName(Person person) =>
        string.Join(" ", new[] { person.FirstName, person.MiddleName, person.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part)));

    private static string RoleLabel(PersonRecordRole role) => role switch
    {
        PersonRecordRole.Employee => "Employee",
        PersonRecordRole.FamilyMember => "Family member",
        PersonRecordRole.TemporaryVisitor => "Temporary visitor",
        _ => role.ToString()
    };

    private static string ValidityCaption(ValidityDuration validity)
    {
        if (validity == null)
            return string.Empty;
        if (!string.IsNullOrWhiteSpace(validity.LocalizedDisplayName))
            return validity.LocalizedDisplayName;
        if (!string.IsNullOrWhiteSpace(validity.NameTm))
            return validity.NameTm;
        return string.Empty;
    }

    private static string LikePattern(string search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return null;

        var cleaned = search.Trim().Replace("%", string.Empty).Replace("_", string.Empty);
        return cleaned.Length == 0 ? null : "%" + cleaned + "%";
    }
}
