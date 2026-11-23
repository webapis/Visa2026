using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.PersonIntake;

/// <summary>In-memory wizard state before save (pending toggles, follow-up note, child-record cards).</summary>
public sealed class PersonIntakeWizardState
{
    private Guid? _loadedPersonId;
    private bool _childrenLoaded;
    private bool _pendingLoaded;

    public HashSet<string> PendingKeys { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> RecordNotes { get; } = new(StringComparer.Ordinal);

    public string FollowNote { get; set; }

    public List<PersonIntakePassportDraft> PassportDrafts { get; } = new();
    public List<PersonIntakeEducationDraft> EducationDrafts { get; } = new();
    public List<PersonIntakeMedicalDraft> MedicalDrafts { get; } = new();
    public List<PersonIntakeResidenceDraft> ResidenceDrafts { get; } = new();
    public List<PersonIntakePersonDocumentDraft> DocumentDrafts { get; } = new();
    public List<PersonIntakeTravelDraft> TravelDrafts { get; } = new();
    public List<PersonIntakeSalaryDraft> SalaryDrafts { get; } = new();
    public List<PersonIntakePositionDraft> PositionDrafts { get; } = new();
    public PersonIntakeOtherDrafts OtherDrafts { get; set; } = new();

    /// <summary>Backward-compatible single passport draft used by older call sites.</summary>
    public PersonIntakePassportDraft PassportDraft =>
        PassportDrafts.Count > 0 ? PassportDrafts[0] : AddPassportDraft(expand: true);

    /// <summary>Backward-compatible single education draft used by older call sites.</summary>
    public PersonIntakeEducationDraft EducationDraft =>
        EducationDrafts.Count > 0 ? EducationDrafts[0] : AddEducationDraft(expand: true);

    public void LoadFromPerson(Person person, bool reloadChildren = false, bool reloadPending = false)
    {
        var personId = person?.ID;
        if (reloadPending || !_pendingLoaded || _loadedPersonId != personId)
        {
            PendingKeys.Clear();
            RecordNotes.Clear();
            if (person?.PendingItems != null)
            {
                foreach (var item in person.PendingItems)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.TargetKey))
                        continue;

                    PendingKeys.Add(item.TargetKey);
                    if (!string.IsNullOrWhiteSpace(item.Note))
                        RecordNotes[item.TargetKey] = item.Note;
                }
            }

            FollowNote = person?.IncompleteNotes ?? string.Empty;
            PersonIntakeDeferredRecords.EnsureCreateTimePending(person, this);
            _pendingLoaded = true;
        }

        if (reloadChildren || !_childrenLoaded || _loadedPersonId != personId)
        {
            LoadChildren(person);
            _childrenLoaded = true;
            _loadedPersonId = personId;
        }
    }

    public void LoadChildren(Person person)
    {
        PassportDrafts.Clear();
        EducationDrafts.Clear();
        MedicalDrafts.Clear();
        ResidenceDrafts.Clear();
        DocumentDrafts.Clear();
        TravelDrafts.Clear();
        SalaryDrafts.Clear();
        PositionDrafts.Clear();

        if (person == null)
            return;

        if (person.Passports != null)
        {
            foreach (var passport in person.Passports.Where(p => p != null))
                PassportDrafts.Add(PersonIntakePassportDraft.FromEntity(passport, expand: PassportDrafts.Count == 0));
        }

        if (person.Educations != null)
        {
            foreach (var education in person.Educations.Where(e => e != null))
                EducationDrafts.Add(PersonIntakeEducationDraft.FromEntity(education, expand: EducationDrafts.Count == 0));
        }

        if (person.MedicalRecords != null)
        {
            foreach (var medical in person.MedicalRecords.Where(m => m != null))
                MedicalDrafts.Add(PersonIntakeMedicalDraft.FromEntity(medical, expand: MedicalDrafts.Count == 0));
        }

        if (person.AddressesOfResidence != null)
        {
            foreach (var address in person.AddressesOfResidence.Where(a => a != null))
                ResidenceDrafts.Add(PersonIntakeResidenceDraft.FromEntity(address, expand: ResidenceDrafts.Count == 0));
        }

        if (person.Documents != null)
        {
            foreach (var doc in person.Documents.Where(d => d != null))
                DocumentDrafts.Add(PersonIntakePersonDocumentDraft.FromEntity(doc, expand: DocumentDrafts.Count == 0));
        }

        if (person.TravelHistories != null)
        {
            foreach (var travel in person.TravelHistories.Where(t => t != null))
                TravelDrafts.Add(PersonIntakeTravelDraft.FromEntity(travel, expand: TravelDrafts.Count == 0));
        }

        if (person.Salaries != null)
        {
            foreach (var salary in person.Salaries.Where(s => s != null))
                SalaryDrafts.Add(PersonIntakeSalaryDraft.FromEntity(salary, expand: SalaryDrafts.Count == 0));
        }

        if (person.PositionHistory != null)
        {
            foreach (var row in person.PositionHistory.Where(p => p != null))
                PositionDrafts.Add(PersonIntakePositionDraft.FromEntity(row, expand: PositionDrafts.Count == 0));
        }
    }

    public PersonIntakePassportDraft AddPassportDraft(bool expand = true)
    {
        if (expand)
        {
            foreach (var draft in PassportDrafts)
                draft.Expanded = false;
        }

        var card = new PersonIntakePassportDraft { Expanded = expand };
        PassportDrafts.Add(card);
        return card;
    }

    public PersonIntakeEducationDraft AddEducationDraft(bool expand = true)
    {
        if (expand)
        {
            foreach (var draft in EducationDrafts)
                draft.Expanded = false;
        }

        var card = new PersonIntakeEducationDraft { Expanded = expand };
        EducationDrafts.Add(card);
        return card;
    }

    public PersonIntakeMedicalDraft AddMedicalDraft(bool expand = true)
    {
        if (expand)
        {
            foreach (var draft in MedicalDrafts)
                draft.Expanded = false;
        }

        var card = new PersonIntakeMedicalDraft { Expanded = expand };
        MedicalDrafts.Add(card);
        return card;
    }

    public PersonIntakeResidenceDraft AddResidenceDraft(bool expand = true)
    {
        if (expand)
        {
            foreach (var draft in ResidenceDrafts)
                draft.Expanded = false;
        }

        var card = new PersonIntakeResidenceDraft { Expanded = expand };
        ResidenceDrafts.Add(card);
        return card;
    }

    public PersonIntakePersonDocumentDraft AddDocumentDraft(bool expand = true)
    {
        if (expand)
        {
            foreach (var draft in DocumentDrafts)
                draft.Expanded = false;
        }

        var card = new PersonIntakePersonDocumentDraft { Expanded = expand };
        DocumentDrafts.Add(card);
        return card;
    }

    public PersonIntakeTravelDraft AddTravelDraft(bool expand = true)
    {
        if (expand)
        {
            foreach (var draft in TravelDrafts)
                draft.Expanded = false;
        }

        var card = new PersonIntakeTravelDraft { Expanded = expand };
        TravelDrafts.Add(card);
        return card;
    }

    public PersonIntakeSalaryDraft AddSalaryDraft(bool expand = true)
    {
        if (expand)
        {
            foreach (var draft in SalaryDrafts)
                draft.Expanded = false;
        }

        var card = new PersonIntakeSalaryDraft { Expanded = expand };
        SalaryDrafts.Add(card);
        return card;
    }
    public PersonIntakePositionDraft AddPositionDraft(bool expand = true)
    {
        if (expand)
        {
            foreach (var draft in PositionDrafts)
                draft.Expanded = false;
        }

        var card = new PersonIntakePositionDraft { Expanded = expand };
        PositionDrafts.Add(card);
        return card;
    }

    public IReadOnlyCollection<string> DraftSatisfiedKeys()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        if (PassportDrafts.Any(d => d.HasContent || d.ExistingId.HasValue))
            keys.Add(PersonPendingTargets.Passport);
        if (EducationDrafts.Any(d => d.HasContent || d.ExistingId.HasValue))
            keys.Add(PersonPendingTargets.Education);
        if (MedicalDrafts.Any(d => d.HasContent || d.ExistingId.HasValue))
            keys.Add(PersonPendingTargets.MedicalRecord);
        if (ResidenceDrafts.Any(d => d.HasContent || d.ExistingId.HasValue))
            keys.Add(PersonPendingTargets.AddressOfResidence);
        if (DocumentDrafts.Any(d => d.HasContent || d.ExistingId.HasValue))
            keys.Add(PersonPendingTargets.PersonDocument);
        if (TravelDrafts.Any(d => d.HasContent || d.ExistingId.HasValue))
            keys.Add(PersonPendingTargets.TravelHistory);
        if (SalaryDrafts.Any(d => d.HasContent || d.ExistingId.HasValue))
            keys.Add(PersonPendingTargets.EmployeeSalary);
        if (PositionDrafts.Any(d => d.HasContent || d.ExistingId.HasValue))
            keys.Add(PersonPendingTargets.EmployeePositionHistory);
        return keys;
    }
}

public sealed class PersonIntakeDocumentCopyDraft
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public bool IsExisting { get; set; }
    /// <summary>Saved child document row (<c>PassportDocument</c> and the other <c>*Document</c> types). Empty for a file chosen in this session.</summary>
    public Guid? ExistingDocumentId { get; set; }
}

public sealed class PersonIntakePassportDraft
{
    public Guid CardId { get; set; } = Guid.NewGuid();
    /// <summary>Default lookups are filled once per card; a cleared value stays cleared.</summary>
    public bool DefaultsApplied { get; set; }
    public Guid? ExistingId { get; set; }
    public bool Expanded { get; set; } = true;
    public string PassportNumber { get; set; } = string.Empty;
    public Guid? PassportTypeId { get; set; }
    public Guid? IssuedCountryId { get; set; }
    public string Authority { get; set; } = string.Empty;
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public bool IsCancelled { get; set; }
    public List<PersonIntakeDocumentCopyDraft> Copies { get; } = new();

    public string SavedPassportNumber { get; set; } = string.Empty;
    public Guid? SavedPassportTypeId { get; set; }
    public Guid? SavedIssuedCountryId { get; set; }
    public string SavedAuthority { get; set; } = string.Empty;
    public DateTime? SavedIssueDate { get; set; }
    public DateTime? SavedExpirationDate { get; set; }
    public bool SavedIsCancelled { get; set; }

    /// <summary>Required passport fields are present and the expiration date is after the issue date.</summary>
    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(PassportNumber)
        && PassportTypeId.HasValue
        && IssuedCountryId.HasValue
        && !string.IsNullOrWhiteSpace(Authority)
        && IssueDate.HasValue
        && ExpirationDate.HasValue
        && ExpirationDate.Value.Date > IssueDate.Value.Date;

    /// <summary>This card already exists on the person and its required fields match the last save.</summary>
    public bool IsSaved =>
        ExistingId.HasValue
        && string.Equals(PassportNumber?.Trim() ?? string.Empty, SavedPassportNumber?.Trim() ?? string.Empty, StringComparison.Ordinal)
        && PassportTypeId == SavedPassportTypeId
        && IssuedCountryId == SavedIssuedCountryId
        && string.Equals(Authority?.Trim() ?? string.Empty, SavedAuthority?.Trim() ?? string.Empty, StringComparison.Ordinal)
        && IssueDate == SavedIssueDate
        && ExpirationDate == SavedExpirationDate
        && IsCancelled == SavedIsCancelled;

    public bool HasContent =>
        !string.IsNullOrWhiteSpace(PassportNumber)
        || PassportTypeId.HasValue
        || IssuedCountryId.HasValue
        || !string.IsNullOrWhiteSpace(Authority)
        || IssueDate.HasValue
        || ExpirationDate.HasValue
        || IsCancelled
        || Copies.Any(c => c != null && (!c.IsExisting || (c.Content?.Length ?? 0) > 0));

    public int? DaysRemaining
    {
        get
        {
            if (!ExpirationDate.HasValue || IsCancelled)
                return null;
            return (ExpirationDate.Value.Date - DateTime.Today).Days;
        }
    }

    public string SummaryTitle =>
        string.IsNullOrWhiteSpace(PassportNumber) ? PersonIntakeText.Get("Card.Passport") : $"{PersonIntakeText.Get("Card.Passport")} · {PassportNumber.Trim()}";

    public string StatusLabel =>
        IsCancelled ? PersonIntakeText.Get("Card.Status.Cancelled")
        : ExpirationDate.HasValue && ExpirationDate.Value.Date < DateTime.Today ? PersonIntakeText.Get("Card.Status.Expired")
        : ExistingId.HasValue || HasContent ? PersonIntakeText.Get("Card.Status.Current")
        : "New";

    public static PersonIntakePassportDraft FromEntity(Passport passport, bool expand)
    {
        var draft = new PersonIntakePassportDraft
        {
            ExistingId = passport.ID,
            Expanded = expand,
            PassportNumber = passport.PassportNumber ?? string.Empty,
            PassportTypeId = passport.PassportType?.ID,
            IssuedCountryId = passport.IssuedCountry?.ID,
            Authority = passport.Authority ?? string.Empty,
            IssueDate = passport.IssueDate,
            ExpirationDate = passport.ExpirationDate,
            IsCancelled = passport.IsCancelled,
            SavedPassportNumber = passport.PassportNumber ?? string.Empty,
            SavedPassportTypeId = passport.PassportType?.ID,
            SavedIssuedCountryId = passport.IssuedCountry?.ID,
            SavedAuthority = passport.Authority ?? string.Empty,
            SavedIssueDate = passport.IssueDate,
            SavedExpirationDate = passport.ExpirationDate,
            SavedIsCancelled = passport.IsCancelled
        };

        if (passport.Documents != null)
        {
            foreach (var doc in passport.Documents.Where(d => d?.File != null))
            {
                draft.Copies.Add(new PersonIntakeDocumentCopyDraft
                {
                    FileName = doc.File.FileName ?? "passport-copy.pdf",
                    Content = Array.Empty<byte>(),
                    IsExisting = true,
                    ExistingDocumentId = doc.ID
                });
            }
        }

        return draft;
    }
}

public sealed class PersonIntakeEducationDraft
{
    public Guid CardId { get; set; } = Guid.NewGuid();
    /// <summary>Default lookups are filled once per card; a cleared value stays cleared.</summary>
    public bool DefaultsApplied { get; set; }
    public Guid? ExistingId { get; set; }
    public bool Expanded { get; set; } = true;
    public Guid? EducationLevelId { get; set; }
    public Guid? EducationInstitutionId { get; set; }
    public Guid? EducationCountryId { get; set; }
    public Guid? SpecialtyId { get; set; }
    public string GraduationYear { get; set; } = string.Empty;
    public List<PersonIntakeDocumentCopyDraft> Copies { get; } = new();

    public Guid? SavedEducationLevelId { get; set; }
    public Guid? SavedEducationInstitutionId { get; set; }
    public Guid? SavedEducationCountryId { get; set; }
    public Guid? SavedSpecialtyId { get; set; }
    public string SavedGraduationYear { get; set; } = string.Empty;

    public bool IsComplete =>
        EducationLevelId.HasValue
        && EducationInstitutionId.HasValue
        && EducationCountryId.HasValue
        && SpecialtyId.HasValue;

    public bool IsSaved =>
        ExistingId.HasValue
        && EducationLevelId == SavedEducationLevelId
        && EducationInstitutionId == SavedEducationInstitutionId
        && EducationCountryId == SavedEducationCountryId
        && SpecialtyId == SavedSpecialtyId
        && string.Equals(GraduationYear?.Trim() ?? string.Empty, SavedGraduationYear?.Trim() ?? string.Empty, StringComparison.Ordinal);

    public bool HasContent =>
        EducationLevelId.HasValue
        || EducationInstitutionId.HasValue
        || EducationCountryId.HasValue
        || SpecialtyId.HasValue
        || !string.IsNullOrWhiteSpace(GraduationYear)
        || Copies.Any(c => c != null && (!c.IsExisting || (c.Content?.Length ?? 0) > 0));

    public string SummaryTitle => PersonIntakeText.Get("Card.Education");

    public static PersonIntakeEducationDraft FromEntity(Education education, bool expand)
    {
        var draft = new PersonIntakeEducationDraft
        {
            ExistingId = education.ID,
            Expanded = expand,
            EducationLevelId = education.EducationLevel?.ID,
            EducationInstitutionId = education.EducationInstitution?.ID,
            EducationCountryId = education.EducationCountry?.ID,
            SpecialtyId = education.Specialty?.ID,
            GraduationYear = education.GraduationYear ?? string.Empty,
            SavedEducationLevelId = education.EducationLevel?.ID,
            SavedEducationInstitutionId = education.EducationInstitution?.ID,
            SavedEducationCountryId = education.EducationCountry?.ID,
            SavedSpecialtyId = education.Specialty?.ID,
            SavedGraduationYear = education.GraduationYear ?? string.Empty
        };

        if (education.Documents != null)
        {
            foreach (var doc in education.Documents.Where(d => d?.File != null))
            {
                draft.Copies.Add(new PersonIntakeDocumentCopyDraft
                {
                    FileName = doc.File.FileName ?? "diploma-copy.pdf",
                    Content = Array.Empty<byte>(),
                    IsExisting = true,
                    ExistingDocumentId = doc.ID
                });
            }
        }

        return draft;
    }
}

public sealed class PersonIntakeMedicalDraft
{
    public Guid CardId { get; set; } = Guid.NewGuid();
    /// <summary>Default lookups are filled once per card; a cleared value stays cleared.</summary>
    public bool DefaultsApplied { get; set; }
    public Guid? ExistingId { get; set; }
    public bool Expanded { get; set; } = true;
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime? IssueDate { get; set; }
    public Guid? ValidityDurationId { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public List<PersonIntakeDocumentCopyDraft> Copies { get; } = new();

    public bool HasContent =>
        !string.IsNullOrWhiteSpace(DocumentNumber)
        || IssueDate.HasValue
        || ValidityDurationId.HasValue
        || Copies.Any(c => c != null && (!c.IsExisting || (c.Content?.Length ?? 0) > 0));

    public string SummaryTitle =>
        string.IsNullOrWhiteSpace(DocumentNumber) ? PersonIntakeText.Get("Card.Medical") : $"{PersonIntakeText.Get("Card.Medical")} · {DocumentNumber.Trim()}";

    public static PersonIntakeMedicalDraft FromEntity(MedicalRecord medical, bool expand)
    {
        var draft = new PersonIntakeMedicalDraft
        {
            ExistingId = medical.ID,
            Expanded = expand,
            DocumentNumber = medical.DocumentNumber ?? string.Empty,
            IssueDate = medical.IssueDate == default ? null : medical.IssueDate,
            ValidityDurationId = medical.ValidityDuration?.ID,
            ExpirationDate = medical.ExpirationDate
        };

        if (medical.Documents != null)
        {
            foreach (var doc in medical.Documents.Where(d => d?.File != null))
            {
                draft.Copies.Add(new PersonIntakeDocumentCopyDraft
                {
                    FileName = doc.File.FileName ?? "medical-copy.pdf",
                    Content = Array.Empty<byte>(),
                    IsExisting = true,
                    ExistingDocumentId = doc.ID
                });
            }
        }

        return draft;
    }
}

public sealed class PersonIntakeResidenceDraft
{
    public Guid CardId { get; set; } = Guid.NewGuid();
    public Guid? ExistingId { get; set; }
    public bool Expanded { get; set; } = true;
    public ResidenceType? Type { get; set; } = ResidenceType.PrivateHouse;
    public Guid? RegionId { get; set; }
    public Guid? CityId { get; set; }
    public string FullAddress { get; set; } = string.Empty;
    public DateTime? ExpirationDate { get; set; }
    public Guid? LodgingId { get; set; }
    public Guid? HotelId { get; set; }
    public Guid? HospitalId { get; set; }
    public Guid? OtherSiteId { get; set; }
    public List<PersonIntakeDocumentCopyDraft> Copies { get; } = new();

    public bool HasContent =>
        Type.HasValue
        || RegionId.HasValue
        || CityId.HasValue
        || !string.IsNullOrWhiteSpace(FullAddress)
        || ExpirationDate.HasValue
        || LodgingId.HasValue
        || HotelId.HasValue
        || HospitalId.HasValue
        || OtherSiteId.HasValue
        || Copies.Any(c => c != null && (!c.IsExisting || (c.Content?.Length ?? 0) > 0));

    public string SummaryTitle => Type.HasValue ? $"{PersonIntakeText.Get("Card.Residence")} · {Type}" : PersonIntakeText.Get("Card.Residence");

    public static PersonIntakeResidenceDraft FromEntity(AddressOfResidence address, bool expand)
    {
        var draft = new PersonIntakeResidenceDraft
        {
            ExistingId = address.ID,
            Expanded = expand,
            Type = address.Type,
            RegionId = address.Region?.ID,
            CityId = address.City?.ID,
            FullAddress = address.FullAddress ?? string.Empty,
            ExpirationDate = address.ExpirationDate,
            LodgingId = address.Lodging?.ID,
            HotelId = address.Hotel?.ID,
            HospitalId = address.Hospital?.ID,
            OtherSiteId = address.OtherSite?.ID
        };

        if (address.Documents != null)
        {
            foreach (var doc in address.Documents.Where(d => d?.File != null))
            {
                draft.Copies.Add(new PersonIntakeDocumentCopyDraft
                {
                    FileName = doc.File.FileName ?? "address-copy.pdf",
                    Content = Array.Empty<byte>(),
                    IsExisting = true,
                    ExistingDocumentId = doc.ID
                });
            }
        }

        return draft;
    }
}

/// <summary>Optional Other-records drafts (employee / family).</summary>
public sealed class PersonIntakeOtherDrafts
{
    public List<PersonIntakeDocumentCopyDraft> FamilyRelationDocuments { get; } = new();
    public PersonIntakeWorkDutyDraft WorkDuty { get; set; } = new();
}

public sealed class PersonIntakePositionDraft
{
    public Guid CardId { get; set; } = Guid.NewGuid();
    public Guid? ExistingId { get; set; }
    public bool Expanded { get; set; } = true;
    public Guid? PositionId { get; set; }
    public Guid? ActualPositionId { get; set; }
    public Guid? DepartmentId { get; set; }
    public DateTime? StartDate { get; set; } = DateTime.Today;
    public DateTime? EndDate { get; set; }
    public List<PersonIntakeDocumentCopyDraft> OrderDocuments { get; } = new();

    public Guid? SavedPositionId { get; set; }
    public Guid? SavedActualPositionId { get; set; }
    public Guid? SavedDepartmentId { get; set; }
    public DateTime? SavedStartDate { get; set; }
    public DateTime? SavedEndDate { get; set; }

    public bool IsComplete =>
        PositionId.HasValue
        && ActualPositionId.HasValue
        && StartDate.HasValue;

    public bool IsSaved =>
        ExistingId.HasValue
        && PositionId == SavedPositionId
        && ActualPositionId == SavedActualPositionId
        && DepartmentId == SavedDepartmentId
        && StartDate == SavedStartDate
        && EndDate == SavedEndDate;

    public bool HasContent =>
        PositionId.HasValue
        || ActualPositionId.HasValue
        || DepartmentId.HasValue
        || StartDate.HasValue
        || EndDate.HasValue
        || OrderDocuments.Count > 0;

    public string SummaryTitle =>
        StartDate.HasValue
            ? $"{PersonIntakeText.Get("Card.Position")} · {StartDate:dd.MM.yyyy}"
            : PersonIntakeText.Get("Card.Position");

    public static PersonIntakePositionDraft FromEntity(EmployeePositionHistory row, bool expand)
    {
        var draft = new PersonIntakePositionDraft
        {
            ExistingId = row.ID,
            Expanded = expand,
            PositionId = row.Position?.ID,
            ActualPositionId = row.ActualPosition?.ID,
            DepartmentId = row.Department?.ID,
            StartDate = row.StartDate == default ? null : row.StartDate,
            EndDate = row.EndDate,
            SavedPositionId = row.Position?.ID,
            SavedActualPositionId = row.ActualPosition?.ID,
            SavedDepartmentId = row.Department?.ID,
            SavedStartDate = row.StartDate == default ? null : row.StartDate,
            SavedEndDate = row.EndDate
        };

        if (row.Documents != null)
        {
            foreach (var doc in row.Documents.Where(d => d?.File != null))
            {
                draft.OrderDocuments.Add(new PersonIntakeDocumentCopyDraft
                {
                    FileName = doc.File.FileName ?? "order-for-assignment.pdf",
                    Content = Array.Empty<byte>(),
                    IsExisting = true,
                    ExistingDocumentId = doc.ID
                });
            }
        }

        return draft;
    }
}
public sealed class PersonIntakePersonDocumentDraft
{
    public Guid CardId { get; set; } = Guid.NewGuid();
    public Guid? ExistingId { get; set; }
    public bool Expanded { get; set; } = true;
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public bool IsExisting { get; set; }
    public string SavedFileName { get; set; } = string.Empty;

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(FileName)
        && (IsExisting || (Content?.Length ?? 0) > 0);

    public bool IsSaved =>
        ExistingId.HasValue
        && IsExisting
        && string.Equals(FileName?.Trim() ?? string.Empty, SavedFileName?.Trim() ?? string.Empty, StringComparison.Ordinal);

    public bool HasContent =>
        ExistingId.HasValue
        || !string.IsNullOrWhiteSpace(FileName)
        || (Content?.Length ?? 0) > 0;

    public string SummaryTitle =>
        string.IsNullOrWhiteSpace(FileName) ? PersonIntakeText.Get("Card.PersonalFile") : FileName.Trim();

    public static PersonIntakePersonDocumentDraft FromEntity(PersonDocument document, bool expand)
    {
        return new PersonIntakePersonDocumentDraft
        {
            ExistingId = document.ID,
            Expanded = expand,
            FileName = document.File?.FileName ?? "document.pdf",
            Content = Array.Empty<byte>(),
            IsExisting = true,
            SavedFileName = document.File?.FileName ?? "document.pdf"
        };
    }
}

public sealed class PersonIntakeTravelDraft
{
    public Guid CardId { get; set; } = Guid.NewGuid();
    public Guid? ExistingId { get; set; }
    public bool Expanded { get; set; } = true;
    public TravelType? TravelType { get; set; }
    public MovementType? MovementType { get; set; }
    public DateTime? TravelDate { get; set; } = DateTime.Today;
    public Guid? CheckPointId { get; set; }
    public Guid? CountryId { get; set; }
    public Guid? RegionId { get; set; }
    public Guid? CityId { get; set; }
    public string Notes { get; set; } = string.Empty;

    public bool HasContent =>
        TravelType.HasValue
        || MovementType.HasValue
        || TravelDate.HasValue
        || CheckPointId.HasValue
        || CountryId.HasValue
        || RegionId.HasValue
        || CityId.HasValue
        || !string.IsNullOrWhiteSpace(Notes);

    public string SummaryTitle =>
        TravelType.HasValue || MovementType.HasValue
            ? $"{TravelType?.ToString() ?? PersonIntakeText.Get("Card.Travel")} · {MovementType?.ToString() ?? ""}".Trim(' ', '·')
            : PersonIntakeText.Get("Card.Travel");

    public static PersonIntakeTravelDraft FromEntity(TravelHistory travel, bool expand)
    {
        return new PersonIntakeTravelDraft
        {
            ExistingId = travel.ID,
            Expanded = expand,
            TravelType = travel.TravelType,
            MovementType = travel.MovementType,
            TravelDate = travel.TravelDate == default ? null : travel.TravelDate,
            CheckPointId = travel.CheckPoint?.ID,
            CountryId = travel.Country?.ID,
            RegionId = travel.Region?.ID,
            CityId = travel.City?.ID,
            Notes = travel.Notes ?? string.Empty
        };
    }
}

public sealed class PersonIntakeWorkDutyDraft
{
    public string Description { get; set; } = string.Empty;
    public bool HasContent => !string.IsNullOrWhiteSpace(Description);
}

public sealed class PersonIntakeSalaryDraft
{
    public Guid CardId { get; set; } = Guid.NewGuid();
    public Guid? ExistingId { get; set; }
    public bool Expanded { get; set; } = true;
    public string Amount { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; } = DateTime.Today;
    public DateTime? EndDate { get; set; }
    public string SavedAmount { get; set; } = string.Empty;
    public DateTime? SavedStartDate { get; set; }
    public DateTime? SavedEndDate { get; set; }

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(Amount)
        && StartDate.HasValue;

    public bool IsSaved =>
        ExistingId.HasValue
        && string.Equals(Amount?.Trim() ?? string.Empty, SavedAmount?.Trim() ?? string.Empty, StringComparison.Ordinal)
        && StartDate == SavedStartDate
        && EndDate == SavedEndDate;

    public bool HasContent =>
        !string.IsNullOrWhiteSpace(Amount)
        || StartDate.HasValue
        || EndDate.HasValue;

    public string SummaryTitle =>
        string.IsNullOrWhiteSpace(Amount) ? PersonIntakeText.Get("Card.Salary") : $"{PersonIntakeText.Get("Card.Salary")} · {Amount.Trim()}";

    public static PersonIntakeSalaryDraft FromEntity(EmployeeSalary salary, bool expand)
    {
        return new PersonIntakeSalaryDraft
        {
            ExistingId = salary.ID,
            Expanded = expand,
            Amount = salary.Amount ?? string.Empty,
            StartDate = salary.StartDate == default ? null : salary.StartDate,
            EndDate = salary.EndDate,
            SavedAmount = salary.Amount ?? string.Empty,
            SavedStartDate = salary.StartDate == default ? null : salary.StartDate,
            SavedEndDate = salary.EndDate
        };
    }
}
