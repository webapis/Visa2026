namespace Visa2026.PortalFiling.Worker;

/// <summary>Job statuses mirrored from the prototype algorithm (Visa2026 job BO later).</summary>
public static class FilingJobStatuses
{
    public const string Ready = "Ready";
    public const string Filling = "Filling";
    public const string DraftOnScreen = "DraftOnScreen";
    public const string Confirmed = "Confirmed";
    public const string Saving = "Saving";
    public const string Saved = "Saved";
    public const string Failed = "Failed";
}

/// <summary>Payload the worker expects once Visa2026 exposes portal-filing jobs.</summary>
public sealed class FilingJobDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = FilingJobStatuses.Ready;
    public string? PortalApplicationNumber { get; set; }
    public string? FailureReason { get; set; }
    public FilingJobPayload? Payload { get; set; }
}

public sealed class FilingJobPayload
{
    public string ApplicationType { get; set; } = "REGISTRATION";
    public string? TypeOfLocation { get; set; }
    public string? PlaceOfStay { get; set; }
    public string? AddressDistrict { get; set; }
    public DateOnly? RegistrationDateFrom { get; set; }
    public DateOnly? RegistrationDateTo { get; set; }
    public string? Street { get; set; }
    public string? Building { get; set; }
    public string? Block { get; set; }
    public string? Apartment { get; set; }
    public string? Workplace { get; set; }
    public string? LetterNumber { get; set; }
    public DateOnly? LetterDate { get; set; }
    public string? Note { get; set; }
    public FilingCitizenDto? Citizen { get; set; }
}

public sealed class FilingCitizenDto
{
    public string? FullName { get; set; }
    public string? PassportNumber { get; set; }
}