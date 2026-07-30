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

    [Fact]
    [SupportedOSPlatform("windows")]
    public void PersonOfficerJourney_LoginCreateEmployeeMasterDataCrud()
    {
        Login(E2ETestLoginValues.StandardUserName, E2ETestLoginValues.StandardUserPassword);
        AssertAuthenticatedAppShell();

        NavigateEmployeesList();
        Assert.NotNull(AppContext.GetAction("New"));

        CreateEmployeeWithRequiredFields();

        OpenEmployeeInListByPersonalNumber(E2ETestEmployeeCreateValues.PersonalNumber);
        Assert.Equal(E2ETestEmployeeCreateValues.FirstName, AppContext.GetForm().GetPropertyValue("First Name"));
        Assert.Equal(E2ETestEmployeeCreateValues.LastName, AppContext.GetForm().GetPropertyValue("Last Name"));
        Assert.Equal(
            E2ETestEmployeeCreateValues.PersonalNumber,
            AppContext.GetForm().GetPropertyValue("Personal Number"));

        // Passport → Visa (Visa is nested under Passport, not a Person tab).
        ExecutePersonPassportsNestedNew();
        FillPassportRequiredFields();
        SavePassportDetail();
        AssertPassportDetailShowsNumber(E2ETestPassportCreateValues.PassportNumber);

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

        // Address of residence (Private house)
        ReturnToSavedEmployeeDetail();
        ExecutePersonAddressesNestedNew();
        FillAddressPrivateHouseRequiredFields();
        SaveAddressDetail();
        AssertAddressShowsFullAddress(E2ETestAddressCreateValues.FullAddress);

        // Medical record — create, update, delete
        ReturnToSavedEmployeeDetail();
        ExecutePersonMedicalRecordsNestedNew();
        FillMedicalRecordRequiredFields();
        SaveMedicalRecordDetail();
        AssertMedicalRecordShowsNumber(E2ETestMedicalRecordCreateValues.DocumentNumber);

        UpdateMedicalRecordDocumentNumber();
        _ = TryDeleteMedicalRecord();

        // Phase B — employee-only tabs
        ReturnToSavedEmployeeDetail();
        ExecutePersonPositionHistoryNestedNew();
        FillPositionHistoryRequiredFields();
        SavePositionHistoryDetail();

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

        ReturnToSavedEmployeeDetail();
        ExecutePersonTravelExternalArrivalNestedNew();
        SaveTravelHistoryDetail();
    }
}
