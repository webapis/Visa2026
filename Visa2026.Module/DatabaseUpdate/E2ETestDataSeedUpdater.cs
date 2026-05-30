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

        if (ObjectSpace.GetObjectsQuery<Person>()
            .Any(p => p.PersonalNumber == E2ETestDataSeed.PersonPersonalNumber))
            return;

        var gender = FirstOrDefault<Gender>();
        var country = FirstOrDefault<Country>();
        var maritalStatus = FirstOrDefault<MaritalStatus>();
        var passportType = FirstOrDefault<PassportType>();
        var projectContract = FirstOrDefault<ProjectContract>();

        if (gender == null || country == null || maritalStatus == null || passportType == null || projectContract == null)
        {
            Tracing.Tracer.LogText("E2ETestDataSeedUpdater: skipped — required lookups missing.");
            return;
        }

        var person = ObjectSpace.CreateObject<Person>();
        person.IsEmployee = true;
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

        var passport = ObjectSpace.CreateObject<Passport>();
        passport.Person = person;
        passport.PassportNumber = E2ETestDataSeed.PassportNumber;
        passport.PassportType = passportType;
        passport.IssuedCountry = country;
        passport.Authority = "E2E Test Authority";
        passport.IssueDate = DateTime.Today.AddYears(-2);
        passport.ExpirationDate = DateTime.Today.AddYears(3);
        passport.IsActive = true;

        ObjectSpace.CommitChanges();

        Tracing.Tracer.LogText($"E2ETestDataSeedUpdater: seeded employee {E2ETestDataSeed.PersonFullName}.");
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
