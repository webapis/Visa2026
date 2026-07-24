using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Services.PersonLinkedDocuments;

/// <summary>
/// Resolves sectioned document copies for a <see cref="Person"/> from live child collections.
/// </summary>
public static class PersonLinkedDocumentsResolver
{
    public static PersonLinkedDocumentsSnapshot Resolve(IObjectSpace objectSpace, Person person)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);

        if (person == null)
        {
            return new PersonLinkedDocumentsSnapshot
            {
                PersonId = Guid.Empty,
                Sections = Array.Empty<PersonLinkedDocumentSection>()
            };
        }

        person = objectSpace.GetObject(person);
        var sections = new List<PersonLinkedDocumentSection>
        {
            BuildPassportsSection(objectSpace, person),
            BuildEducationSection(objectSpace, person),
            BuildMedicalSection(objectSpace, person),
            BuildAddressSection(objectSpace, person),
        };

        if (person.IsEmployee)
        {
            sections.Add(BuildWorkPermitSection(objectSpace, person));
            sections.Add(BuildInvitationSection(objectSpace, person));
            sections.Add(BuildPersonDocumentsSection(objectSpace, person));
        }
        else
        {
            sections.Add(BuildFamilyRelationDocumentsSection(objectSpace, person));
        }

        sections.Add(BuildRejectionSection(objectSpace, person));

        return new PersonLinkedDocumentsSnapshot
        {
            PersonId = person.ID,
            PersonDisplayName = person.FullName,
            PersonalNumber = string.IsNullOrWhiteSpace(person.PersonalNumber) ? null : person.PersonalNumber.Trim(),
            Sections = sections
                .Where(section => section.Records.Count > 0)
                .OrderBy(section => section.SortOrder)
                .ToList()
        };
    }

    private static PersonLinkedDocumentSection BuildPassportsSection(IObjectSpace os, Person person)
    {
        var currentPassport = PersonCurrentItems.GetCurrentPassport(person);
        var currentVisa = PersonCurrentItems.GetCurrentVisa(person);
        var records = new List<PersonLinkedDocumentRecord>();

        var passports = person.Passports?
            .Where(p => p != null)
            .OrderByDescending(p => p.IssueDate ?? DateTime.MinValue)
            .ThenByDescending(p => p.ID)
            .ToList() ?? new List<Passport>();

        foreach (var passportEntry in passports)
        {
            var passport = os.GetObject(passportEntry);
            var passportKey = $"Passport:{passport.ID:N}";
            records.Add(new PersonLinkedDocumentRecord
            {
                RecordKey = passportKey,
                RecordLabel = PersonDocumentCopiesLocalization.FormatPassportRecord(passport.PassportNumber),
                SourceCaption = passport.PassportNumber,
                SourceObjectType = typeof(Passport),
                SourceObjectId = passport.ID,
                IsCurrent = currentPassport?.ID == passport.ID,
                Files = LoadDocumentFiles<PassportDocument>(os, d => d.Passport.ID == passport.ID)
            });

            var visas = passport.Visas?
                .Where(v => v != null)
                .OrderByDescending(v => v.StartDate)
                .ThenByDescending(v => v.IssueDate)
                .ThenByDescending(v => v.ID)
                .ToList() ?? new List<Visa>();

            foreach (var visaEntry in visas)
            {
                var visa = os.GetObject(visaEntry);
                records.Add(new PersonLinkedDocumentRecord
                {
                    RecordKey = $"{passportKey}/Visa:{visa.ID:N}",
                    RecordLabel = PersonDocumentCopiesLocalization.FormatVisaRecord(visa.VisaNumber),
                    SourceCaption = visa.VisaNumber,
                    SourceObjectType = typeof(Visa),
                    SourceObjectId = visa.ID,
                    IsCurrent = currentVisa?.ID == visa.ID,
                    IsNested = true,
                    Files = LoadDocumentFiles<VisaDocument>(os, d => d.Visa.ID == visa.ID)
                });
            }
        }

        return new PersonLinkedDocumentSection
        {
            SectionId = "Passports",
            SectionLabel = VisaUiMessages.Get("PersonDocumentCopies.Section.Passports"),
            SortOrder = 10,
            Records = records
        };
    }

    private static PersonLinkedDocumentSection BuildEducationSection(IObjectSpace os, Person person)
    {
        var current = PersonCurrentItems.GetCurrentEducation(person);
        var records = (person.Educations?
            .Where(e => e != null)
            .OrderByDescending(e => ParseGraduationYear(e.GraduationYear))
            .ThenByDescending(e => e.ID)
            .Select(education =>
            {
                education = os.GetObject(education);
                return new PersonLinkedDocumentRecord
                {
                    RecordKey = $"Education:{education.ID:N}",
                    RecordLabel = PersonDocumentCopiesLocalization.FormatEducationRecord(BuildEducationCaption(education)),
                    SourceCaption = BuildEducationCaption(education),
                    SourceObjectType = typeof(Education),
                    SourceObjectId = education.ID,
                    IsCurrent = current?.ID == education.ID,
                    Files = LoadDocumentFiles<EducationDocument>(os, d => d.Education.ID == education.ID)
                };
            })
            .ToList()) ?? new List<PersonLinkedDocumentRecord>();

        return new PersonLinkedDocumentSection
        {
            SectionId = "Education",
            SectionLabel = VisaUiMessages.Get("PersonDocumentCopies.Section.Education"),
            SortOrder = 20,
            Records = records
        };
    }

    private static PersonLinkedDocumentSection BuildMedicalSection(IObjectSpace os, Person person)
    {
        var current = PersonCurrentItems.GetCurrentMedicalRecord(person);
        var records = (person.MedicalRecords?
            .Where(m => m != null)
            .OrderByDescending(m => m.IssueDate)
            .ThenByDescending(m => m.ID)
            .Select(medical =>
            {
                medical = os.GetObject(medical);
                return new PersonLinkedDocumentRecord
                {
                    RecordKey = $"MedicalRecord:{medical.ID:N}",
                    RecordLabel = PersonDocumentCopiesLocalization.FormatMedicalRecord(medical.DocumentNumber),
                    SourceCaption = medical.DocumentNumber,
                    SourceObjectType = typeof(MedicalRecord),
                    SourceObjectId = medical.ID,
                    IsCurrent = current?.ID == medical.ID,
                    Files = LoadDocumentFiles<MedicalRecordDocument>(os, d => d.MedicalRecord.ID == medical.ID)
                };
            })
            .ToList()) ?? new List<PersonLinkedDocumentRecord>();

        return new PersonLinkedDocumentSection
        {
            SectionId = "MedicalRecords",
            SectionLabel = VisaUiMessages.Get("PersonDocumentCopies.Section.MedicalRecords"),
            SortOrder = 30,
            Records = records
        };
    }

    private static PersonLinkedDocumentSection BuildAddressSection(IObjectSpace os, Person person)
    {
        var current = PersonCurrentItems.GetCurrentAddressOfResidence(person);
        var records = new List<PersonLinkedDocumentRecord>();

        var addresses = person.AddressesOfResidence?
            .Where(a => a != null)
            .OrderByDescending(a => a.ExpirationDate ?? DateTime.MaxValue)
            .ThenByDescending(a => a.ID)
            .ToList() ?? new List<AddressOfResidence>();

        foreach (var addressEntry in addresses)
        {
            var address = os.GetObject(addressEntry);
            records.Add(new PersonLinkedDocumentRecord
            {
                RecordKey = $"AddressOfResidence:{address.ID:N}",
                RecordLabel = PersonDocumentCopiesLocalization.FormatAddressRecord(address.FullAddress),
                SourceCaption = address.FullAddress,
                SourceObjectType = typeof(AddressOfResidence),
                SourceObjectId = address.ID,
                IsCurrent = current?.ID == address.ID,
                Files = LoadDocumentFiles<AddressOfResidenceDocument>(os, d => d.AddressOfResidence.ID == address.ID)
            });

            if (address.Type == ResidenceType.Lodging && address.Lodging != null)
            {
                var lodging = os.GetObject(address.Lodging);
                records.Add(new PersonLinkedDocumentRecord
                {
                    RecordKey = $"AddressOfResidence:{address.ID:N}/Lodging:{lodging.ID:N}",
                    RecordLabel = PersonDocumentCopiesLocalization.FormatLodgingRecord(lodging.FullAddress),
                    SourceCaption = lodging.FullAddress,
                    SourceObjectType = typeof(Lodging),
                    SourceObjectId = lodging.ID,
                    IsNested = true,
                    Files = LoadDocumentFiles<LodgingDocument>(os, d => d.Lodging.ID == lodging.ID)
                });
            }
        }

        return new PersonLinkedDocumentSection
        {
            SectionId = "Addresses",
            SectionLabel = VisaUiMessages.Get("PersonDocumentCopies.Section.Addresses"),
            SortOrder = 40,
            Records = records
        };
    }

    private static PersonLinkedDocumentSection BuildWorkPermitSection(IObjectSpace os, Person person)
    {
        var current = PersonCurrentItems.GetCurrentWorkPermitItem(person);
        var records = (person.WorkPermitItems?
            .Where(w => w != null)
            .OrderByDescending(w => w.StartDate)
            .ThenByDescending(w => w.ID)
            .Select(item =>
            {
                item = os.GetObject(item);
                var workPermit = item.WorkPermit != null ? os.GetObject(item.WorkPermit) : null;
                return new PersonLinkedDocumentRecord
                {
                    RecordKey = $"WorkPermitItem:{item.ID:N}",
                    RecordLabel = PersonDocumentCopiesLocalization.FormatWorkPermitRecord(workPermit?.WorkPermitNumber),
                    SourceCaption = workPermit?.WorkPermitNumber,
                    SourceObjectType = typeof(WorkPermitItem),
                    SourceObjectId = item.ID,
                    IsCurrent = current?.ID == item.ID,
                    Files = workPermit == null
                        ? Array.Empty<PersonLinkedDocumentFile>()
                        : LoadDocumentFiles<WorkPermitDocument>(os, d => d.WorkPermit.ID == workPermit.ID)
                };
            })
            .ToList()) ?? new List<PersonLinkedDocumentRecord>();

        return new PersonLinkedDocumentSection
        {
            SectionId = "WorkPermits",
            SectionLabel = VisaUiMessages.Get("PersonDocumentCopies.Section.WorkPermits"),
            SortOrder = 50,
            Records = records
        };
    }

    private static PersonLinkedDocumentSection BuildInvitationSection(IObjectSpace os, Person person)
    {
        var current = PersonCurrentItems.GetCurrentInvitationItem(person);
        var records = (person.InvitationItems?
            .Where(i => i != null)
            .OrderByDescending(i => i.Invitation?.IssuedDate ?? default)
            .ThenByDescending(i => i.ID)
            .Select(item =>
            {
                item = os.GetObject(item);
                var invitation = item.Invitation != null ? os.GetObject(item.Invitation) : null;
                return new PersonLinkedDocumentRecord
                {
                    RecordKey = $"InvitationItem:{item.ID:N}",
                    RecordLabel = PersonDocumentCopiesLocalization.FormatInvitationRecord(invitation?.InvitationNumber),
                    SourceCaption = invitation?.InvitationNumber,
                    SourceObjectType = typeof(InvitationItem),
                    SourceObjectId = item.ID,
                    IsCurrent = current?.ID == item.ID,
                    Files = invitation == null
                        ? Array.Empty<PersonLinkedDocumentFile>()
                        : LoadDocumentFiles<InvitationDocument>(os, d => d.Invitation.ID == invitation.ID)
                };
            })
            .ToList()) ?? new List<PersonLinkedDocumentRecord>();

        return new PersonLinkedDocumentSection
        {
            SectionId = "Invitations",
            SectionLabel = VisaUiMessages.Get("PersonDocumentCopies.Section.Invitations"),
            SortOrder = 60,
            Records = records
        };
    }

    private static PersonLinkedDocumentSection BuildRejectionSection(IObjectSpace os, Person person)
    {
        var current = PersonCurrentItems.GetCurrentRejectionItem(person);
        var records = (person.RejectionItems?
            .Where(i => i != null)
            .OrderByDescending(i => i.Rejection?.Date ?? default)
            .ThenByDescending(i => i.ID)
            .Select(item =>
            {
                item = os.GetObject(item);
                var rejection = item.Rejection != null ? os.GetObject(item.Rejection) : null;
                var caption = rejection?.Date is DateTime date ? date.ToString("dd.MM.yyyy") : null;
                return new PersonLinkedDocumentRecord
                {
                    RecordKey = $"RejectionItem:{item.ID:N}",
                    RecordLabel = PersonDocumentCopiesLocalization.FormatRejectionRecord(caption),
                    SourceCaption = caption,
                    SourceObjectType = typeof(RejectionItem),
                    SourceObjectId = item.ID,
                    IsCurrent = current?.ID == item.ID,
                    Files = rejection == null
                        ? Array.Empty<PersonLinkedDocumentFile>()
                        : LoadDocumentFiles<RejectionDocument>(os, d => d.Rejection.ID == rejection.ID)
                };
            })
            .ToList()) ?? new List<PersonLinkedDocumentRecord>();

        return new PersonLinkedDocumentSection
        {
            SectionId = "Rejections",
            SectionLabel = VisaUiMessages.Get("PersonDocumentCopies.Section.Rejections"),
            SortOrder = 70,
            Records = records
        };
    }

    private static PersonLinkedDocumentSection BuildPersonDocumentsSection(IObjectSpace os, Person person)
    {
        var records = (person.Documents?
            .Where(d => d != null)
            .OrderBy(d => d.ID)
            .Select(doc =>
            {
                doc = os.GetObject(doc);
                return new PersonLinkedDocumentRecord
                {
                    RecordKey = $"PersonDocument:{doc.ID:N}",
                    RecordLabel = PersonDocumentCopiesLocalization.FormatPersonDocumentRecord(doc.File?.FileName),
                    SourceCaption = doc.File?.FileName,
                    SourceObjectType = typeof(PersonDocument),
                    SourceObjectId = doc.ID,
                    Files = MapSingleDocumentFile(doc)
                };
            })
            .ToList()) ?? new List<PersonLinkedDocumentRecord>();

        return new PersonLinkedDocumentSection
        {
            SectionId = "PersonDocuments",
            SectionLabel = VisaUiMessages.Get("PersonDocumentCopies.Section.PersonDocuments"),
            SortOrder = 80,
            Records = records
        };
    }

    private static PersonLinkedDocumentSection BuildFamilyRelationDocumentsSection(IObjectSpace os, Person person)
    {
        var records = (person.FamilyRelationDocuments?
            .Where(d => d != null)
            .OrderBy(d => d.ID)
            .Select(doc =>
            {
                doc = os.GetObject(doc);
                return new PersonLinkedDocumentRecord
                {
                    RecordKey = $"FamilyRelationDocument:{doc.ID:N}",
                    RecordLabel = PersonDocumentCopiesLocalization.FormatFamilyRelationDocumentRecord(doc.File?.FileName),
                    SourceCaption = doc.File?.FileName,
                    SourceObjectType = typeof(PersonFamilyRelationDocument),
                    SourceObjectId = doc.ID,
                    Files = MapSingleDocumentFile(doc)
                };
            })
            .ToList()) ?? new List<PersonLinkedDocumentRecord>();

        return new PersonLinkedDocumentSection
        {
            SectionId = "FamilyRelationDocuments",
            SectionLabel = VisaUiMessages.Get("PersonDocumentCopies.Section.FamilyRelationDocuments"),
            SortOrder = 80,
            Records = records
        };
    }

    private static IReadOnlyList<PersonLinkedDocumentFile> MapSingleDocumentFile(DocumentBase doc)
    {
        if (doc?.File == null)
        {
            return new[]
            {
                new PersonLinkedDocumentFile
                {
                    DocumentRowId = doc?.ID ?? Guid.Empty,
                    DocumentTypeName = doc?.GetType().Name ?? string.Empty,
                    FileName = string.Empty,
                    HasContent = false
                }
            };
        }

        return new[] { MapDocumentFile(doc) };
    }

    private static string? BuildEducationCaption(Education education)
    {
        var inst = education.EducationInstitution?.NameTm
                   ?? education.EducationInstitution?.Name;
        var year = education.GraduationYear?.Trim();
        if (string.IsNullOrWhiteSpace(inst) && string.IsNullOrWhiteSpace(year))
            return null;
        if (string.IsNullOrWhiteSpace(inst))
            return year;
        if (string.IsNullOrWhiteSpace(year))
            return inst;
        return $"{year} — {inst}";
    }

    private static int ParseGraduationYear(string year) =>
        int.TryParse(year?.Trim(), out var parsed) ? parsed : int.MinValue;

    private static IReadOnlyList<PersonLinkedDocumentFile> LoadDocumentFiles<TDocument>(
        IObjectSpace os,
        System.Linq.Expressions.Expression<Func<TDocument, bool>> filter)
        where TDocument : DocumentBase
    {
        return os.GetObjectsQuery<TDocument>()
            .Where(filter)
            .OrderBy(d => d.ID)
            .Include(d => d.File)
            .AsEnumerable()
            .Select(MapDocumentFile)
            .ToList();
    }

    private static PersonLinkedDocumentFile MapDocumentFile(DocumentBase doc)
    {
        var file = doc.File;
        if (file == null)
        {
            return new PersonLinkedDocumentFile
            {
                DocumentRowId = doc.ID,
                DocumentTypeName = doc.GetType().Name,
                FileName = string.Empty,
                HasContent = false
            };
        }

        bool hasContent = file.Size > 0;
        return new PersonLinkedDocumentFile
        {
            FileDataId = file.ID,
            DocumentRowId = doc.ID,
            DocumentTypeName = doc.GetType().Name,
            FileName = string.IsNullOrWhiteSpace(file.FileName) ? "document" : file.FileName,
            SizeBytes = (int)Math.Min(int.MaxValue, file.Size),
            HasContent = hasContent
        };
    }
}
