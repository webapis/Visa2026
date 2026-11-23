using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.PersonIntake;

/// <summary>
/// "Used N×" counts for person intake wizard lookup fields. Each field counts rows that reference
/// the value in that same property (Nationality counts <c>Person.Nationality</c>, not every Country use).
/// </summary>
public static class PersonIntakeLookupUsage
{
    public const string PersonGender = "Person.Gender";
    public const string PersonCountryOfBirth = "Person.CountryOfBirth";
    public const string PersonNationality = "Person.Nationality";
    public const string PersonForeignAddressCountry = "Person.ForeignAddressCountry";
    public const string PersonMaritalStatus = "Person.MaritalStatus";
    public const string PersonProjectContract = "Person.ProjectContract";
    public const string PersonSubcontractor = "Person.Subcontractor";
    public const string PersonRelationship = "Person.Relationship";
    public const string PersonSponsoringEmployee = "Person.SponsoringEmployee";
    public const string PassportType = "Passport.PassportType";
    public const string PassportIssuedCountry = "Passport.IssuedCountry";
    public const string EducationLevel = "Education.EducationLevel";
    public const string EducationInstitution = "Education.EducationInstitution";
    public const string EducationCountry = "Education.EducationCountry";
    public const string EducationSpecialty = "Education.Specialty";
    public const string PositionVisaReports = "EmployeePositionHistory.Position";
    public const string PositionActual = "EmployeePositionHistory.ActualPosition";
    public const string PositionDepartment = "EmployeePositionHistory.Department";

    private static readonly IReadOnlyDictionary<Guid, int> Empty = new Dictionary<Guid, int>();

    public static IReadOnlyDictionary<Guid, int> Load(IObjectSpace? objectSpace, string? usageKey)
    {
        if (objectSpace == null || string.IsNullOrWhiteSpace(usageKey))
            return Empty;

        try
        {
            return usageKey switch
            {
                PersonGender => Count<Person>(objectSpace, p => (Guid?)p.Gender.ID),
                PersonCountryOfBirth => Count<Person>(objectSpace, p => (Guid?)p.CountryOfBirth.ID),
                PersonNationality => Count<Person>(objectSpace, p => (Guid?)p.Nationality.ID),
                PersonForeignAddressCountry => Count<Person>(objectSpace, p => (Guid?)p.ForeignAddressCountry.ID),
                PersonMaritalStatus => Count<Person>(objectSpace, p => (Guid?)p.MaritalStatus.ID),
                PersonProjectContract => Count<Person>(objectSpace, p => (Guid?)p.ProjectContract.ID),
                PersonSubcontractor => Count<Person>(objectSpace, p => (Guid?)p.Subcontractor.ID),
                PersonRelationship => Count<Person>(objectSpace, p => (Guid?)p.Relationship.ID),
                PersonSponsoringEmployee => Count<Person>(objectSpace, p => (Guid?)p.SponsoringEmployee.ID),
                PassportType => Count<Passport>(objectSpace, p => (Guid?)p.PassportType.ID),
                PassportIssuedCountry => Count<Passport>(objectSpace, p => (Guid?)p.IssuedCountry.ID),
                EducationLevel => Count<Education>(objectSpace, e => (Guid?)e.EducationLevel.ID),
                EducationInstitution => Count<Education>(objectSpace, e => (Guid?)e.EducationInstitution.ID),
                EducationCountry => Count<Education>(objectSpace, e => (Guid?)e.EducationCountry.ID),
                EducationSpecialty => Count<Education>(objectSpace, e => (Guid?)e.Specialty.ID),
                PositionVisaReports => Count<EmployeePositionHistory>(objectSpace, h => (Guid?)h.Position.ID),
                PositionActual => Count<EmployeePositionHistory>(objectSpace, h => (Guid?)h.ActualPosition.ID),
                PositionDepartment => Count<EmployeePositionHistory>(objectSpace, h => (Guid?)h.Department.ID),
                _ => Empty
            };
        }
        catch
        {
            // Counts only rank suggestions; a failed query must not break the wizard.
            return Empty;
        }
    }

    private static IReadOnlyDictionary<Guid, int> Count<TOwner>(
        IObjectSpace objectSpace,
        Expression<Func<TOwner, Guid?>> key) where TOwner : class
    {
        return objectSpace.GetObjectsQuery<TOwner>()
            .Select(key)
            .Where(id => id != null)
            .GroupBy(id => id!.Value)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionary(row => row.Key, row => row.Count);
    }
}
