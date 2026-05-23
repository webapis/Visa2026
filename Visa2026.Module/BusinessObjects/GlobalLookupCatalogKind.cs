namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Identifies a <b>global</b> lookup catalog (embedded <c>LookupCatalogs/*.json</c>, shared across deployments).
/// Used with <see cref="GlobalLookupCatalogAttribute"/> and <see cref="LookupLocalization"/> for Layer B UI strings.
/// </summary>
public enum GlobalLookupCatalogKind
{
    Country = 1,
    Gender = 2,
    MaritalStatus = 3,
    Urgency = 4,
    VisaCategory = 5,
    VisaPeriod = 6,
    VisaType = 7,
    EducationLevel = 8,
    PurposeOfTravel = 9,
    CheckPoint = 10,
    VisaIssuedPlace = 11,
    MigrationService = 12,
    PassportType = 13,
    Relationship = 14,
    ApplicationLocation = 15,
    BorderZoneLocation = 16,
    ValidityDuration = 17,
    ApplicationState = 18,
    Region = 19,
    City = 20,
}
