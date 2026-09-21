using System;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Visa2026.Module;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Seeds extra employees on the EasyTest database so Find-and-open can search among more than one row.
/// </summary>
internal static class EasyTestEmployeeRosterSeed
{
    internal static void Ensure(IObjectSpace objectSpace)
    {
        var country = FindLookup<Country>(objectSpace, E2ETestEmployeeCreateValues.CountryDisplay);
        var gender = FindLookup<Gender>(objectSpace, E2ETestEmployeeCreateValues.GenderDisplay);
        var marital = FindLookup<MaritalStatus>(objectSpace, E2ETestEmployeeCreateValues.MaritalStatusDisplay);
        var contract = FindLookup<ProjectContract>(objectSpace, E2ETestEmployeeCreateValues.ProjectContractDisplay);
        var subcontractor = FindLookup<Subcontractor>(objectSpace, E2ETestEmployeeCreateValues.SubcontractorDisplay);

        if (country == null || gender == null || marital == null || contract == null || subcontractor == null)
        {
            Tracing.Tracer.LogText(
                $"EasyTestEmployeeRosterSeed skipped: missing lookup (country={country != null}, gender={gender != null}, marital={marital != null}, contract={contract != null}, subcontractor={subcontractor != null}).");
            return;
        }

        var birthDate = DateTime.ParseExact(
            E2ETestEmployeeCreateValues.DateOfBirth,
            "dd.MM.yyyy",
            CultureInfo.InvariantCulture);

        foreach (var spec in E2ETestFindEmployeeDecoyValues.All)
        {
            if (objectSpace.GetObjectsQuery<Person>().Any(p => p.PersonalNumber == spec.PersonalNumber))
                continue;

            var person = objectSpace.CreateObject<Person>();
            PersonRoleHelper.ApplyRole(person, PersonRecordRole.Employee);
            person.FirstName = spec.FirstName;
            person.LastName = spec.LastName;
            person.PersonalNumber = spec.PersonalNumber;
            person.DateOfBirth = birthDate;
            person.BirthPlace = E2ETestEmployeeCreateValues.BirthPlace;
            person.CountryOfBirth = country;
            person.Gender = gender;
            person.MaritalStatus = marital;
            person.Nationality = country;
            person.ForeignAddress = E2ETestEmployeeCreateValues.ForeignAddress;
            person.ForeignAddressCountry = country;
            person.ProjectContract = contract;
            person.Subcontractor = subcontractor;
            person.VisaApplicationFamilyMembersText = VisaFamilyMemberLinesHelper.NoneValue;
        }
    }

    private static T? FindLookup<T>(IObjectSpace objectSpace, string display)
        where T : LookupBase
    {
        foreach (var row in objectSpace.GetObjectsQuery<T>().ToList())
        {
            if (LookupMatches(row, display))
                return row;
        }

        return null;
    }

    private static bool LookupMatches(LookupBase row, string display)
    {
        if (string.IsNullOrWhiteSpace(display))
            return false;

#pragma warning disable CS0618
        if (ContainsIgnoreCase(row.Name, display) || ContainsIgnoreCase(row.NameTm, display))
            return true;
#pragma warning restore CS0618

        return ContainsIgnoreCase(row.LocalizationKey, display)
            || ContainsIgnoreCase(row.Code, display);
    }

    private static bool ContainsIgnoreCase(string value, string display) =>
        !string.IsNullOrWhiteSpace(value)
        && (value.Equals(display, StringComparison.OrdinalIgnoreCase)
            || value.Contains(display, StringComparison.OrdinalIgnoreCase)
            || display.Contains(value, StringComparison.OrdinalIgnoreCase));
}