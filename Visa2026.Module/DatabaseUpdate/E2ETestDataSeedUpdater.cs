using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Seeds one employee with a valid passport for E2E tests (Visa2026EasyTest database only).
/// Runs after lookup catalogs are synced.
/// </summary>
public sealed class E2ETestDataSeedUpdater : ModuleUpdater
{
    public E2ETestDataSeedUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();

        if (!IsEasyTestDatabase())
            return;

        Person? existing = ObjectSpace.GetObjectsQuery<Person>()
            .FirstOrDefault(p => p.PersonalNumber == E2ETestDataSeed.PersonPersonalNumber);

        if (existing != null)
        {
            EnsureSeedEmployeeRole(existing);
            EnsureSeedPassport(existing);
            ObjectSpace.CommitChanges();
            return;
        }

        var gender = FirstOrDefault<Gender>();
        var country = FirstOrDefault<Country>();
        var maritalStatus = FirstOrDefault<MaritalStatus>();
        var passportType = FirstOrDefault<PassportType>();
        var projectContract = ObjectSpace.GetObjectsQuery<ProjectContract>()
            .FirstOrDefault(p => p.NameTm != null && p.NameTm.Contains(E2ETestEmployeeCreateValues.ProjectContractDisplay))
            ?? FirstOrDefault<ProjectContract>();

        if (gender == null || country == null || maritalStatus == null || passportType == null || projectContract == null)
        {
            Tracing.Tracer.LogText("E2ETestDataSeedUpdater: skipped — required lookups missing.");
            return;
        }

        var person = ObjectSpace.CreateObject<Person>();
        PersonRoleHelper.ApplyRole(person, PersonRecordRole.Employee);
        person.FirstName = E2ETestDataSeed.PersonFirstName;
        person.LastName = E2ETestDataSeed.PersonLastName;
        person.PersonalNumber = E2ETestDataSeed.PersonPersonalNumber;
        person.DateOfBirth = new DateTime(1990, 1, 15);
        person.BirthPlace = "Ashgabat";
        person.CountryOfBirth = country;
        person.Gender = gender;
        person.MaritalStatus = maritalStatus;
        person.Nationality = country;
        person.ForeignAddress = "E2E test foreign address";
        person.ForeignAddressCountry = country;
        person.ProjectContract = projectContract;
        person.HireDate = DateTime.Today.AddYears(-1);

        CreateSeedPassport(person, passportType, country);

        ObjectSpace.CommitChanges();

        Tracing.Tracer.LogText($"E2ETestDataSeedUpdater: seeded employee {E2ETestDataSeed.PersonFullName}.");
    }

    private static void EnsureSeedEmployeeRole(Person person)
    {
        if (person.PersonRole == PersonRecordRole.Employee)
            return;

        var previousRole = person.PersonRole;
        PersonRoleHelper.ApplyRole(person, PersonRecordRole.Employee);
        Tracing.Tracer.LogText(
            $"E2ETestDataSeedUpdater: corrected {E2ETestDataSeed.PersonPersonalNumber} from {previousRole} to Employee.");
    }

    private void EnsureSeedPassport(Person person)
    {
        bool hasSeedPassport = ObjectSpace.GetObjectsQuery<Passport>()
            .Any(p => p.Person != null
                      && p.Person.ID == person.ID
                      && p.PassportNumber == E2ETestDataSeed.PassportNumber);

        if (hasSeedPassport)
            return;

        var passportType = FirstOrDefault<PassportType>();
        var country = FirstOrDefault<Country>();
        if (passportType == null || country == null)
        {
            Tracing.Tracer.LogText("E2ETestDataSeedUpdater: skipped seed passport — lookups missing.");
            return;
        }

        CreateSeedPassport(person, passportType, country);
        Tracing.Tracer.LogText($"E2ETestDataSeedUpdater: added seed passport {E2ETestDataSeed.PassportNumber}.");
    }

    private void CreateSeedPassport(Person person, PassportType passportType, Country country)
    {
        var passport = ObjectSpace.CreateObject<Passport>();
        passport.Person = person;
        passport.PassportNumber = E2ETestDataSeed.PassportNumber;
        passport.PassportType = passportType;
        passport.IssuedCountry = country;
        passport.Authority = "E2E Test Authority";
        passport.IssueDate = DateTime.Today.AddYears(-2);
        passport.ExpirationDate = DateTime.Today.AddYears(3);
    }

    private bool IsEasyTestDatabase()
    {
        if (ObjectSpace is not EFCoreObjectSpace efObjectSpace)
            return false;

        var connectionString = efObjectSpace.DbContext.Database.GetConnectionString();
        return connectionString != null
               && connectionString.Contains("Visa2026EasyTest", StringComparison.OrdinalIgnoreCase);
    }

    private T? FirstOrDefault<T>() where T : class =>
        ObjectSpace.GetObjectsQuery<T>().FirstOrDefault();
}
