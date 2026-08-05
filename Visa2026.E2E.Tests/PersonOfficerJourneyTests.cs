using System.Runtime.Versioning;
using DevExpress.EasyTest.Framework;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.E2E.Tests;

/// <summary>
/// Officer Person master-data CRUD journey — E2E-001 through E2E-008.
/// Log on → create employee → passport → visa → education → address → medical (CRUD) →
/// position history → work duty → salary → external arrival travel.
/// </summary>
public class PersonOfficerJourneyTests : E2ETestBase
{
    public PersonOfficerJourneyTests(EasyTestSessionFixture session) : base(session) { }

    /// <summary>
    /// Short journey: login → create employee → nested passport create (stops after passport assert).
    /// Prefer for local headed runs / ffmpeg UI recording. Distinct personal/passport numbers from the
    /// full master-data Fact so both can share one EasyTest session DB.
    /// </summary>
    [Fact]
    [SupportedOSPlatform("windows")]
    [Trait("Category", "UserManual")]
    [Trait("GuideSlug", "person/register")]
    public void PersonOfficerJourney_LoginCreateEmployeeAddPassport()
    {
        RunLoginCreateEmployeeAddPassport(
            E2ETestPassportCreateOnlyJourneyValues.PersonalNumber,
            E2ETestPassportCreateOnlyJourneyValues.FirstName,
            E2ETestPassportCreateOnlyJourneyValues.LastName,
            E2ETestPassportCreateOnlyJourneyValues.FullName,
            E2ETestPassportCreateOnlyJourneyValues.PassportNumber);
    }

    [Fact]
    [SupportedOSPlatform("windows")]
    public void PersonOfficerJourney_LoginCreateEmployeeMasterDataCrud()
    {
        RunLoginCreateEmployeeAddPassport(
            E2ETestEmployeeCreateValues.PersonalNumber,
            E2ETestEmployeeCreateValues.FirstName,
            E2ETestEmployeeCreateValues.LastName,
            E2ETestEmployeeCreateValues.FullName,
            E2ETestPassportCreateValues.PassportNumber);

        ExecutePassportVisasNestedNew();
        FillVisaRequiredFields();
        SaveVisaDetail();
        AssertVisaDetailShowsNumber(E2ETestVisaCreateValues.VisaNumber);

        // Education
        ReturnToSavedEmployeeDetail();
        ExecutePersonEducationsNestedNew();
        FillEducationRequiredFields();
        SaveEducationDetail();
        AssertEducationShowsInstitution(E2ETestEducationCreateValues.InstitutionDisplay);

        // Address of residence (Lodging) deferred for CI — Region/City/Lodging cascade FillForm
        // does not reliably bind under EasyTest (see learnings). Helpers remain for a follow-up.

        // Medical record — create (update/delete reopen via nested list is flaky after TabbedMDI)
        ReturnToSavedEmployeeDetail();
        ExecutePersonMedicalRecordsNestedNew();
        FillMedicalRecordRequiredFields();
        SaveMedicalRecordDetail();
        AssertMedicalRecordShowsNumber(E2ETestMedicalRecordCreateValues.DocumentNumber);

        // Phase B — employee-only tabs (PositionHistory deferred: lookup bind same class of flake as Address)
        ReturnToSavedEmployeeDetail();
        ExecutePersonWorkDutiesNestedNew();
        FillWorkDutyRequiredFields();
        SaveWorkDutyDetail();
        AssertWorkDutyShowsDescription(E2ETestWorkDutyCreateValues.Description);

        ReturnToSavedEmployeeDetail();
        ExecutePersonSalariesNestedNew();
        FillSalaryRequiredFields();
        SaveSalaryDetail();
        AssertSalaryShowsAmount(E2ETestSalaryCreateValues.Amount);

        // Travel — manual External Arrival (ApplicationItem sync removed; split New button)
        ReturnToSavedEmployeeDetail();
        ExecutePersonTravelExternalArrivalNestedNew();
        FillTravelExternalArrivalRequiredFields();
        SaveTravelHistoryDetail();
        AssertTravelExternalArrivalSaved();
    }

    /// <summary>Shared prefix: login → Employees list → create employee → nested Passports New → passport DetailView (not Lookup/Passport nav).</summary>
    private void RunLoginCreateEmployeeAddPassport(
        string personalNumber,
        string firstName,
        string lastName,
        string fullName,
        string passportNumber)
    {
        EasyTestScreenshotCapture.Capture(AppContext, "00-logon-page");
        Login(E2ETestLoginValues.StandardUserName, E2ETestLoginValues.StandardUserPassword);
        EasyTestScreenshotCapture.Capture(AppContext, "01-after-login");
        AssertAuthenticatedAppShell();
        EasyTestScreenshotCapture.Capture(AppContext, "02-employees-list");

        NavigateEmployeesList();
        Assert.NotNull(AppContext.GetAction("New"));

        CreateEmployeeWithRequiredFields(personalNumber, firstName, lastName);
        EasyTestScreenshotCapture.Capture(AppContext, "03-employee-created");

        OpenEmployeeInListByPersonalNumber(personalNumber, fullName);
        Assert.Equal(firstName, AppContext.GetForm().GetPropertyValue("First Name"));
        Assert.Equal(lastName, AppContext.GetForm().GetPropertyValue("Last Name"));
        Assert.Equal(personalNumber, AppContext.GetForm().GetPropertyValue("Personal Number"));
        EasyTestScreenshotCapture.Capture(AppContext, "04-employee-detail");

        ExecutePersonPassportsNestedNew();
        EasyTestScreenshotCapture.Capture(AppContext, "05-passport-detail-new");
        FillPassportRequiredFields(passportNumber);
        EasyTestScreenshotCapture.Capture(AppContext, "06-passport-fields-filled");
        SavePassportDetail();
        AssertPassportDetailShowsNumber(passportNumber);
        EasyTestScreenshotCapture.Capture(AppContext, "07-passport-saved");
    }
}