using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using DevExpress.ExpressApp;
using Microsoft.EntityFrameworkCore;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.PersonLinkedDocuments;

/// <summary>
/// Attaches scanned PDF/image files to an existing person-linked document owner
/// (Passport, Education, Position, Address, Medical). Does not create the owner BO.
/// </summary>
public static class PersonLinkedDocumentUploadService
{
    public static bool SupportsLinkKind(ApplicationProfileInstancePersonLinkKind kind) =>
        kind is ApplicationProfileInstancePersonLinkKind.Passport
            or ApplicationProfileInstancePersonLinkKind.Education
            or ApplicationProfileInstancePersonLinkKind.Position
            or ApplicationProfileInstancePersonLinkKind.AddressOfResidence
            or ApplicationProfileInstancePersonLinkKind.MedicalRecord;

    public static string BuildRecordKey(ApplicationProfileInstancePersonLinkKind kind, Guid sourceObjectId)
    {
        if (sourceObjectId == Guid.Empty)
            return string.Empty;

        return kind switch
        {
            ApplicationProfileInstancePersonLinkKind.Passport => $"Passport:{sourceObjectId:N}",
            ApplicationProfileInstancePersonLinkKind.Education => $"Education:{sourceObjectId:N}",
            ApplicationProfileInstancePersonLinkKind.Position => $"EmployeePositionHistory:{sourceObjectId:N}",
            ApplicationProfileInstancePersonLinkKind.AddressOfResidence => $"AddressOfResidence:{sourceObjectId:N}",
            ApplicationProfileInstancePersonLinkKind.MedicalRecord => $"MedicalRecord:{sourceObjectId:N}",
            _ => string.Empty,
        };
    }

    public static bool HasScanFiles(
        IObjectSpace objectSpace,
        ApplicationProfileInstancePersonLinkKind kind,
        Guid sourceObjectId) =>
        ListDocuments(objectSpace, kind, sourceObjectId).Any(d => d.HasContent);
    public static ApplicationProfileInstancePersonLinkKind? TryMapPeopleRecordKey(string? recordKey) =>
        recordKey?.Trim() switch
        {
            "passport" => ApplicationProfileInstancePersonLinkKind.Passport,
            "education" => ApplicationProfileInstancePersonLinkKind.Education,
            "position" => ApplicationProfileInstancePersonLinkKind.Position,
            "address" => ApplicationProfileInstancePersonLinkKind.AddressOfResidence,
            "medical" => ApplicationProfileInstancePersonLinkKind.MedicalRecord,
            _ => null,
        };

    public static Guid? ResolvePreferredSourceObjectId(
        Person person,
        ApplicationProfileInstancePersonLinkKind kind)
    {
        if (person == null)
            return null;

        return kind switch
        {
            ApplicationProfileInstancePersonLinkKind.Passport =>
                PersonCurrentItems.GetCurrentPassport(person)?.ID
                ?? person.Passports?.Where(p => p != null).OrderByDescending(p => p.IssueDate ?? DateTime.MinValue)
                    .ThenByDescending(p => p.ID).FirstOrDefault()?.ID,
            ApplicationProfileInstancePersonLinkKind.Education =>
                PersonCurrentItems.GetCurrentEducation(person)?.ID
                ?? person.Educations?.Where(e => e != null).OrderByDescending(e => e.ID).FirstOrDefault()?.ID,
            ApplicationProfileInstancePersonLinkKind.Position =>
                PersonCurrentItems.GetCurrentPositionHistory(person)?.ID
                ?? person.PositionHistory?.Where(p => p != null).OrderByDescending(p => p.ID).FirstOrDefault()?.ID,
            ApplicationProfileInstancePersonLinkKind.AddressOfResidence =>
                PersonCurrentItems.GetCurrentAddressOfResidence(person)?.ID
                ?? person.AddressesOfResidence?.Where(a => a != null).OrderByDescending(a => a.ID).FirstOrDefault()?.ID,
            ApplicationProfileInstancePersonLinkKind.MedicalRecord =>
                PersonCurrentItems.GetCurrentMedicalRecord(person)?.ID
                ?? person.MedicalRecords?.Where(m => m != null).OrderByDescending(m => m.ID).FirstOrDefault()?.ID,
            _ => null,
        };
    }


    public sealed record ExistingDocument(Guid DocumentRowId, Guid FileDataId, string FileName, bool HasContent);

    public static IReadOnlyList<ExistingDocument> ListDocuments(
        IObjectSpace objectSpace,
        ApplicationProfileInstancePersonLinkKind kind,
        Guid sourceObjectId)
    {
        if (objectSpace == null || sourceObjectId == Guid.Empty || !SupportsLinkKind(kind))
            return Array.Empty<ExistingDocument>();

        return kind switch
        {
            ApplicationProfileInstancePersonLinkKind.Passport =>
                MapDocs(objectSpace.GetObjectsQuery<PassportDocument>()
                    .Where(d => d.Passport != null && d.Passport.ID == sourceObjectId)
                    .OrderBy(d => d.ID)
                    .Include(d => d.File)
                    .AsEnumerable()),
            ApplicationProfileInstancePersonLinkKind.Education =>
                MapDocs(objectSpace.GetObjectsQuery<EducationDocument>()
                    .Where(d => d.Education != null && d.Education.ID == sourceObjectId)
                    .OrderBy(d => d.ID)
                    .Include(d => d.File)
                    .AsEnumerable()),
            ApplicationProfileInstancePersonLinkKind.Position =>
                MapDocs(objectSpace.GetObjectsQuery<EmployeePositionHistoryDocument>()
                    .Where(d => d.EmployeePositionHistory != null && d.EmployeePositionHistory.ID == sourceObjectId)
                    .OrderBy(d => d.ID)
                    .Include(d => d.File)
                    .AsEnumerable()),
            ApplicationProfileInstancePersonLinkKind.AddressOfResidence =>
                MapDocs(objectSpace.GetObjectsQuery<AddressOfResidenceDocument>()
                    .Where(d => d.AddressOfResidence != null && d.AddressOfResidence.ID == sourceObjectId)
                    .OrderBy(d => d.ID)
                    .Include(d => d.File)
                    .AsEnumerable()),
            ApplicationProfileInstancePersonLinkKind.MedicalRecord =>
                MapDocs(objectSpace.GetObjectsQuery<MedicalRecordDocument>()
                    .Where(d => d.MedicalRecord != null && d.MedicalRecord.ID == sourceObjectId)
                    .OrderBy(d => d.ID)
                    .Include(d => d.File)
                    .AsEnumerable()),
            _ => Array.Empty<ExistingDocument>(),
        };
    }

    private static IReadOnlyList<ExistingDocument> MapDocs(IEnumerable<DocumentBase> docs) =>
        docs.Select(doc =>
        {
            var file = doc.File;
            return new ExistingDocument(
                doc.ID,
                file?.ID ?? Guid.Empty,
                string.IsNullOrWhiteSpace(file?.FileName) ? "document" : file!.FileName!,
                file != null && file.Size > 0);
        }).ToList();

    public static bool TryApplyChanges(
        IObjectSpace objectSpace,
        ApplicationProfileInstancePersonLinkKind kind,
        Guid sourceObjectId,
        IReadOnlyList<(string FileName, byte[] Content)> addFiles,
        IReadOnlyList<Guid> removeDocumentRowIds,
        out int addedCount,
        out int removedCount,
        out string? errorMessage)
    {
        addedCount = 0;
        removedCount = 0;
        errorMessage = null;
        if (objectSpace == null || sourceObjectId == Guid.Empty || !SupportsLinkKind(kind))
        {
            errorMessage = "Upload is not available.";
            return false;
        }

        addFiles ??= Array.Empty<(string, byte[])>();
        removeDocumentRowIds ??= Array.Empty<Guid>();
        if (addFiles.Count == 0 && removeDocumentRowIds.Count == 0)
        {
            errorMessage = "Nothing to save.";
            return false;
        }

        foreach (var rowId in removeDocumentRowIds.Where(id => id != Guid.Empty).Distinct())
        {
            if (!TryDeleteDocumentRow(objectSpace, kind, sourceObjectId, rowId, out errorMessage))
            {
                if (removedCount > 0)
                    objectSpace.CommitChanges();
                return removedCount > 0;
            }

            removedCount++;
        }

        foreach (var file in addFiles)
        {
            var validationError = ValidateBytes(objectSpace, file.FileName, file.Content);
            if (!string.IsNullOrEmpty(validationError))
            {
                errorMessage = validationError;
                if (addedCount > 0 || removedCount > 0)
                {
                    objectSpace.CommitChanges();
                    return true;
                }

                return false;
            }

            if (!TryAttachOne(objectSpace, kind, sourceObjectId, file.FileName, file.Content, out errorMessage))
            {
                if (addedCount > 0 || removedCount > 0)
                {
                    objectSpace.CommitChanges();
                    return true;
                }

                return false;
            }

            addedCount++;
        }

        objectSpace.CommitChanges();
        return true;
    }

    private static bool TryDeleteDocumentRow(
        IObjectSpace objectSpace,
        ApplicationProfileInstancePersonLinkKind kind,
        Guid sourceObjectId,
        Guid documentRowId,
        out string? errorMessage)
    {
        errorMessage = null;
        DocumentBase? doc = null;
        switch (kind)
        {
            case ApplicationProfileInstancePersonLinkKind.Passport:
            {
                var row = objectSpace.GetObjectByKey<PassportDocument>(documentRowId);
                if (row?.Passport?.ID == sourceObjectId)
                    doc = row;
                break;
            }
            case ApplicationProfileInstancePersonLinkKind.Education:
            {
                var row = objectSpace.GetObjectByKey<EducationDocument>(documentRowId);
                if (row?.Education?.ID == sourceObjectId)
                    doc = row;
                break;
            }
            case ApplicationProfileInstancePersonLinkKind.Position:
            {
                var row = objectSpace.GetObjectByKey<EmployeePositionHistoryDocument>(documentRowId);
                if (row?.EmployeePositionHistory?.ID == sourceObjectId)
                    doc = row;
                break;
            }
            case ApplicationProfileInstancePersonLinkKind.AddressOfResidence:
            {
                var row = objectSpace.GetObjectByKey<AddressOfResidenceDocument>(documentRowId);
                if (row?.AddressOfResidence?.ID == sourceObjectId)
                    doc = row;
                break;
            }
            case ApplicationProfileInstancePersonLinkKind.MedicalRecord:
            {
                var row = objectSpace.GetObjectByKey<MedicalRecordDocument>(documentRowId);
                if (row?.MedicalRecord?.ID == sourceObjectId)
                    doc = row;
                break;
            }
        }

        if (doc == null)
        {
            errorMessage = "Document row was not found on this record.";
            return false;
        }

        var file = doc.File;
        objectSpace.Delete(doc);
        if (file != null)
            objectSpace.Delete(file);
        return true;
    }

    public static string? ValidateBytes(IObjectSpace objectSpace, string fileName, byte[] content)
    {
        if (content == null || content.Length == 0)
            return "The file is empty.";

        var probe = objectSpace.CreateObject<FileData>();
        probe.FileName = string.IsNullOrWhiteSpace(fileName) ? "scan.pdf" : Path.GetFileName(fileName.Trim());
        probe.Content = content;
        probe.Size = content.Length;
        if (!DocumentFileUploadConstraints.TryValidate(probe, out var error))
            return error;

        var maxMb = SystemSettings.TryGetInstance(objectSpace)?.MaxDocumentSizeInMB
                    ?? SystemSettings.DefaultMaxDocumentSizeInMB;
        if (content.Length > maxMb * 1024L * 1024L)
            return $"The uploaded document exceeds the maximum allowed size of {maxMb}MB.";

        objectSpace.Delete(probe);
        return null;
    }

    public static bool TryAddFiles(
        IObjectSpace objectSpace,
        ApplicationProfileInstancePersonLinkKind kind,
        Guid sourceObjectId,
        IReadOnlyList<(string FileName, byte[] Content)> files,
        out int addedCount,
        out string? errorMessage)
    {
        addedCount = 0;
        errorMessage = null;
        if (objectSpace == null || sourceObjectId == Guid.Empty || files == null || files.Count == 0)
        {
            errorMessage = "Upload is not available.";
            return false;
        }

        if (!SupportsLinkKind(kind))
        {
            errorMessage = "This record type does not support file upload here.";
            return false;
        }

        foreach (var file in files)
        {
            var validationError = ValidateBytes(objectSpace, file.FileName, file.Content);
            if (!string.IsNullOrEmpty(validationError))
            {
                errorMessage = validationError;
                if (addedCount > 0)
                    objectSpace.CommitChanges();
                return addedCount > 0;
            }

            if (!TryAttachOne(objectSpace, kind, sourceObjectId, file.FileName, file.Content, out errorMessage))
            {
                if (addedCount > 0)
                    objectSpace.CommitChanges();
                return addedCount > 0;
            }

            addedCount++;
        }

        objectSpace.CommitChanges();
        return true;
    }

    private static bool TryAttachOne(
        IObjectSpace objectSpace,
        ApplicationProfileInstancePersonLinkKind kind,
        Guid sourceObjectId,
        string fileName,
        byte[] content,
        out string? errorMessage)
    {
        errorMessage = null;
        var file = objectSpace.CreateObject<FileData>();
        file.FileName = string.IsNullOrWhiteSpace(fileName) ? "scan.pdf" : Path.GetFileName(fileName.Trim());
        file.Content = content;
        file.Size = content.Length;

        switch (kind)
        {
            case ApplicationProfileInstancePersonLinkKind.Passport:
            {
                var passport = objectSpace.GetObjectByKey<Passport>(sourceObjectId);
                if (passport == null)
                {
                    errorMessage = "Passport record was not found.";
                    return false;
                }

                var doc = objectSpace.CreateObject<PassportDocument>();
                doc.Passport = passport;
                doc.File = file;
                passport.Documents ??= new ObservableCollection<PassportDocument>();
                passport.Documents.Add(doc);
                return true;
            }
            case ApplicationProfileInstancePersonLinkKind.Education:
            {
                var education = objectSpace.GetObjectByKey<Education>(sourceObjectId);
                if (education == null)
                {
                    errorMessage = "Education record was not found.";
                    return false;
                }

                var doc = objectSpace.CreateObject<EducationDocument>();
                doc.Education = education;
                doc.File = file;
                education.Documents ??= new ObservableCollection<EducationDocument>();
                education.Documents.Add(doc);
                return true;
            }
            case ApplicationProfileInstancePersonLinkKind.Position:
            {
                var position = objectSpace.GetObjectByKey<EmployeePositionHistory>(sourceObjectId);
                if (position == null)
                {
                    errorMessage = "Position record was not found.";
                    return false;
                }

                var doc = objectSpace.CreateObject<EmployeePositionHistoryDocument>();
                doc.EmployeePositionHistory = position;
                doc.File = file;
                position.Documents ??= new ObservableCollection<EmployeePositionHistoryDocument>();
                position.Documents.Add(doc);
                return true;
            }
            case ApplicationProfileInstancePersonLinkKind.AddressOfResidence:
            {
                var address = objectSpace.GetObjectByKey<AddressOfResidence>(sourceObjectId);
                if (address == null)
                {
                    errorMessage = "Address record was not found.";
                    return false;
                }

                var doc = objectSpace.CreateObject<AddressOfResidenceDocument>();
                doc.AddressOfResidence = address;
                doc.File = file;
                address.Documents ??= new ObservableCollection<AddressOfResidenceDocument>();
                address.Documents.Add(doc);
                return true;
            }
            case ApplicationProfileInstancePersonLinkKind.MedicalRecord:
            {
                var medical = objectSpace.GetObjectByKey<MedicalRecord>(sourceObjectId);
                if (medical == null)
                {
                    errorMessage = "Medical record was not found.";
                    return false;
                }

                var doc = objectSpace.CreateObject<MedicalRecordDocument>();
                doc.MedicalRecord = medical;
                doc.File = file;
                medical.Documents ??= new ObservableCollection<MedicalRecordDocument>();
                medical.Documents.Add(doc);
                return true;
            }
            default:
                errorMessage = "This record type does not support file upload here.";
                return false;
        }
    }
}