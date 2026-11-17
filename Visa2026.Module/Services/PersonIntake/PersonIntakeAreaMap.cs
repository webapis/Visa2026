using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.PersonIntake;

internal static class PersonIntakeAreaMap
{
  private static readonly Dictionary<string, string> Map = new()
    {
        [PersonPendingTargets.DateOfBirth] = PersonIncompleteDataLabels.PersonalData,
        [PersonPendingTargets.Gender] = PersonIncompleteDataLabels.PersonalData,
        [PersonPendingTargets.PersonalNumber] = PersonIncompleteDataLabels.PersonalData,
        [PersonPendingTargets.BirthPlace] = PersonIncompleteDataLabels.PersonalData,
        [PersonPendingTargets.CountryOfBirth] = PersonIncompleteDataLabels.PersonalData,
        [PersonPendingTargets.Nationality] = PersonIncompleteDataLabels.PersonalData,
        [PersonPendingTargets.MaritalStatus] = PersonIncompleteDataLabels.PersonalData,
        [PersonPendingTargets.ForeignAddress] = PersonIncompleteDataLabels.PersonalData,
        [PersonPendingTargets.ForeignAddressCountry] = PersonIncompleteDataLabels.PersonalData,
        [PersonPendingTargets.ProjectContract] = PersonIncompleteDataLabels.PersonalData,
        [PersonPendingTargets.Subcontractor] = PersonIncompleteDataLabels.PersonalData,
        [PersonPendingTargets.Photo] = PersonIncompleteDataLabels.Photo,
        [PersonPendingTargets.Passport] = PersonIncompleteDataLabels.Passport,
        [PersonPendingTargets.Education] = PersonIncompleteDataLabels.Education,
        [PersonPendingTargets.MedicalRecord] = PersonIncompleteDataLabels.Medical,
        [PersonPendingTargets.AddressOfResidence] = PersonIncompleteDataLabels.Address,
        [PersonPendingTargets.PersonDocument] = PersonIncompleteDataLabels.Cv,
        [PersonPendingTargets.EmployeePositionHistory] = PersonIncompleteDataLabels.Other,
        [PersonPendingTargets.TravelHistory] = PersonIncompleteDataLabels.Other,
        [PersonPendingTargets.WorkDuty] = PersonIncompleteDataLabels.Other,
        [PersonPendingTargets.EmployeeSalary] = PersonIncompleteDataLabels.Other,
        [PersonPendingTargets.PersonFamilyRelationDocument] = PersonIncompleteDataLabels.FamilyDocs,
    };

    public static bool TryGetArea(string targetKey, out string areaKey) =>
        Map.TryGetValue(targetKey, out areaKey);
}