using System;
using System.Linq;
using System.Threading;
using DevExpress.EasyTest.Framework;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.E2E.Tests;

/// <summary>
/// Nested Person-record CRUD helpers (passport→visa, education, address, medical, position, work duty, salary, travel).
/// </summary>
public abstract partial class E2ETestBase
{
    protected void ActivatePersonNestedTab(params string[] tabCaptions)
    {
        int maxAttempts = EasyTestCITuning.NestedListActionMaxAttempts;
        TimeSpan delay = EasyTestCITuning.FormFieldRetryDelay;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (EasyTestBlazorNavigationHelper.TryClickTabByAnyText(
                    AppContext, TimeSpan.FromSeconds(2), tabCaptions))
            {
                Thread.Sleep(EasyTestCITuning.LayoutTabSettleDelay);
                return;
            }

            foreach (string caption in tabCaptions)
            {
                try
                {
                    var tabAction = AppContext.GetAction(caption);
                    if (tabAction == null)
                        continue;
                    tabAction.Execute();
                    Thread.Sleep(EasyTestCITuning.LayoutTabSettleDelay);
                    return;
                }
                catch (AdapterOperationException)
                {
                    // Try next caption.
                }
            }

            if (attempt < maxAttempts - 1)
                Thread.Sleep(delay);
        }

        EasyTestBlazorNavigationHelper.TryDumpDiagnostics(
            AppContext, EasyTestHostProcessLauncher.LogDirectory, "person-nested-tab");
        throw new InvalidOperationException(
            $"Could not activate Person nested tab [{string.Join(" | ", tabCaptions)}] " +
            $"(URL: '{EasyTestBlazorNavigationHelper.GetCurrentUrl(AppContext)}').");
    }

    protected void ExecutePersonNestedNew(
        string[] tabCaptions,
        string[] newTitles,
        Func<bool> isDetailReady,
        string diagnosticLabel)
    {
        int maxAttempts = EasyTestCITuning.NestedNewClickMaxAttempts;
        TimeSpan delay = EasyTestCITuning.FormFieldRetryDelay;
        var nestedNewClicked = false;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (isDetailReady())
                return;

            ActivatePersonNestedTab(tabCaptions);

            if (TryClickNestedNew(newTitles, isDetailReady))
            {
                nestedNewClicked = true;
                if (TryWaitUntil(isDetailReady, EasyTestCITuning.NestedNewProbeTimeout))
                    return;
            }

            if (attempt < maxAttempts - 1)
                Thread.Sleep(delay);
        }

        EasyTestBlazorNavigationHelper.TryDumpDiagnostics(
            AppContext, EasyTestHostProcessLauncher.LogDirectory, diagnosticLabel);

        if (!nestedNewClicked)
        {
            throw new InvalidOperationException(
                $"Could not execute nested New [{string.Join(" | ", newTitles)}] on tabs [{string.Join(" | ", tabCaptions)}].");
        }

        throw new InvalidOperationException(
            $"{diagnosticLabel} detail did not open after nested New " +
            $"(URL: '{EasyTestBlazorNavigationHelper.GetCurrentUrl(AppContext)}').");
    }

    private bool TryClickNestedNew(string[] newTitles, Func<bool> isDetailReady)
    {
        if (EasyTestBlazorNavigationHelper.TryClickToolbarActionByAnyTitle(
                AppContext, TimeSpan.FromSeconds(5), newTitles))
        {
            return true;
        }

        foreach (string caption in newTitles.Concat(new[] { "New" }))
        {
            try
            {
                var newAction = AppContext.GetAction(caption);
                if (newAction == null)
                    continue;

                newAction.Execute();
                if (TryWaitUntil(isDetailReady, TimeSpan.FromSeconds(4)))
                    return true;
            }
            catch (AdapterOperationException)
            {
                // Try next caption.
            }
        }

        return false;
    }

    private static bool TryWaitUntil(Func<bool> condition, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return true;
            Thread.Sleep(TimeSpan.FromMilliseconds(500));
        }

        return false;
    }

    private bool IsDetailFormReady(string viewIdFragment, string probeCaption)
    {
        if (EasyTestBlazorNavigationHelper.UrlContains(AppContext, viewIdFragment))
            return true;

        try
        {
            if (AppContext.GetAction("Save") == null)
                return false;

            AppContext.GetForm().GetPropertyValue(probeCaption);
            return true;
        }
        catch (AdapterOperationException)
        {
            return false;
        }
    }

    private void WaitForDetailReady(string viewIdFragment, string probeCaption, string label)
    {
        if (TryWaitUntil(
                () => IsDetailFormReady(viewIdFragment, probeCaption),
                EasyTestCITuning.PassportDetailOpenTimeout))
            return;

        throw new InvalidOperationException(
            $"{label} detail did not open (URL: '{EasyTestBlazorNavigationHelper.GetCurrentUrl(AppContext)}').");
    }

    private void FillDetailFormWithRetry(params EasyTestParameter[] fields)
    {
        foreach (EasyTestParameter field in fields)
            FillSingleDetailFieldWithRetry(field);
    }

    private void FillSingleDetailFieldWithRetry(EasyTestParameter field)
    {
        int maxAttempts = EasyTestCITuning.FormFieldMaxAttempts;
        TimeSpan delay = EasyTestCITuning.FormFieldRetryDelay;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                AppContext.GetForm().FillForm(new EasyTestParameter(field.Name, field.Value));
                return;
            }
            catch (AdapterOperationException)
            {
                if (attempt < maxAttempts - 1)
                    Thread.Sleep(delay);
            }
        }

        throw new InvalidOperationException(
            $"Could not fill detail form field: {field.Name} " +
            $"(URL: '{EasyTestBlazorNavigationHelper.GetCurrentUrl(AppContext)}').");
    }

    private void AssertDetailPropertyEquals(string caption, string expected)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                string actual = AppContext.GetForm().GetPropertyValue(caption);
                Assert.Equal(expected, actual);
                return;
            }
            catch (Exception) when (attempt < 19)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(500));
            }
        }

        throw new InvalidOperationException(
            $"Detail property '{caption}' did not equal '{expected}' " +
            $"(URL: '{EasyTestBlazorNavigationHelper.GetCurrentUrl(AppContext)}').");
    }

    // --- Visa (under Passport) ---

    protected void ExecutePassportVisasNestedNew()
    {
        ExecutePersonNestedNew(
            new[] { E2ETestPersonNestedUi.VisasTab },
            new[] { E2ETestPersonNestedUi.VisasNewTitle },
            () => IsDetailFormReady("Visa_DetailView", E2ETestVisaFieldCaptions.VisaNumber),
            "visa");
    }

    protected void FillVisaRequiredFields(
        string visaNumber = E2ETestVisaCreateValues.VisaNumber)
    {
        WaitForDetailReady("Visa_DetailView", E2ETestVisaFieldCaptions.VisaNumber, "Visa");
        FillDetailFormWithRetry(
            new EasyTestParameter(E2ETestVisaFieldCaptions.VisaNumber, visaNumber),
            new EasyTestParameter(E2ETestVisaFieldCaptions.IssueDate, E2ETestVisaCreateValues.IssueDate),
            new EasyTestParameter(E2ETestVisaFieldCaptions.StartDate, E2ETestVisaCreateValues.StartDate),
            new EasyTestParameter(E2ETestVisaFieldCaptions.ExpirationDate, E2ETestVisaCreateValues.ExpirationDate));
    }

    protected void SaveVisaDetail() => ExecuteActionWithRetry("Save");

    protected void AssertVisaDetailShowsNumber(string visaNumber) =>
        AssertDetailPropertyEquals(E2ETestVisaFieldCaptions.VisaNumber, visaNumber);

    // --- Education ---

    protected void ExecutePersonEducationsNestedNew()
    {
        ExecutePersonNestedNew(
            new[] { E2ETestPersonNestedUi.EducationsTab },
            new[] { E2ETestPersonNestedUi.EducationsNewTitle },
            () => IsDetailFormReady("Education_DetailView", E2ETestEducationFieldCaptions.EducationInstitution),
            "education");
    }

    protected void FillEducationRequiredFields(
        string institutionDisplay = E2ETestEducationCreateValues.InstitutionDisplay)
    {
        WaitForDetailReady(
            "Education_DetailView",
            E2ETestEducationFieldCaptions.EducationInstitution,
            "Education");
        FillDetailFormWithRetry(
            new EasyTestParameter(E2ETestEducationFieldCaptions.EducationInstitution, institutionDisplay));
    }

    protected void SaveEducationDetail() => ExecuteActionWithRetry("Save");

    protected void AssertEducationShowsInstitution(string institutionDisplay) =>
        AssertDetailPropertyEquals(E2ETestEducationFieldCaptions.EducationInstitution, institutionDisplay);

    protected void UpdateEducationInstitution(
        string newInstitution = E2ETestEducationCreateValues.UpdatedInstitutionDisplay)
    {
        ReturnToSavedEmployeeDetail();
        ActivatePersonNestedTab(E2ETestPersonNestedUi.EducationsTab);
        EasyTestBlazorNavigationHelper.ClickListRowContaining(
            AppContext, E2ETestEducationCreateValues.InstitutionDisplay);
        WaitForDetailReady(
            "Education_DetailView",
            E2ETestEducationFieldCaptions.EducationInstitution,
            "Education");
        FillDetailFormWithRetry(
            new EasyTestParameter(E2ETestEducationFieldCaptions.EducationInstitution, newInstitution));
        SaveEducationDetail();
        AssertEducationShowsInstitution(newInstitution);
    }

    // --- Address of residence (Private house) ---

    protected void ExecutePersonAddressesNestedNew()
    {
        ExecutePersonNestedNew(
            new[] { E2ETestPersonNestedUi.AddressesTab, E2ETestPersonNestedUi.AddressesTabAlt, E2ETestPersonNestedUi.AddressesTabAlt2 },
            new[] { E2ETestPersonNestedUi.AddressesNewTitle, "New Address of Residence" },
            () => IsDetailFormReady("AddressOfResidence_DetailView", E2ETestAddressFieldCaptions.Type),
            "address");
    }

    protected void FillAddressPrivateHouseRequiredFields()
    {
        WaitForDetailReady(
            "AddressOfResidence_DetailView",
            E2ETestAddressFieldCaptions.Type,
            "AddressOfResidence");

        // Type defaults to Lodging — switch first so Full Address / Expiration appear.
        FillDetailFormWithRetry(
            new EasyTestParameter(E2ETestAddressFieldCaptions.Type, E2ETestAddressCreateValues.TypeDisplay));
        Thread.Sleep(EasyTestCITuning.LayoutTabSettleDelay);

        FillDetailFormWithRetry(
            new EasyTestParameter(E2ETestAddressFieldCaptions.Region, E2ETestAddressCreateValues.RegionDisplay),
            new EasyTestParameter(E2ETestAddressFieldCaptions.City, E2ETestAddressCreateValues.CityDisplay),
            new EasyTestParameter(E2ETestAddressFieldCaptions.FullAddress, E2ETestAddressCreateValues.FullAddress),
            new EasyTestParameter(E2ETestAddressFieldCaptions.ExpirationDate, E2ETestAddressCreateValues.ExpirationDate));
    }

    protected void SaveAddressDetail() => ExecuteActionWithRetry("Save");

    protected void AssertAddressShowsFullAddress(string fullAddress) =>
        AssertDetailPropertyEquals(E2ETestAddressFieldCaptions.FullAddress, fullAddress);

    // --- Medical record ---

    protected void ExecutePersonMedicalRecordsNestedNew()
    {
        ExecutePersonNestedNew(
            new[] { E2ETestPersonNestedUi.MedicalRecordsTab, E2ETestPersonNestedUi.MedicalRecordsTabAlt, E2ETestPersonNestedUi.MedicalRecordsTabAlt2 },
            new[] { E2ETestPersonNestedUi.MedicalRecordsNewTitle },
            () => IsDetailFormReady("MedicalRecord_DetailView", E2ETestMedicalRecordFieldCaptions.DocumentNumber),
            "medical");
    }

    protected void FillMedicalRecordRequiredFields(
        string documentNumber = E2ETestMedicalRecordCreateValues.DocumentNumber)
    {
        WaitForDetailReady(
            "MedicalRecord_DetailView",
            E2ETestMedicalRecordFieldCaptions.DocumentNumber,
            "MedicalRecord");
        FillDetailFormWithRetry(
            new EasyTestParameter(E2ETestMedicalRecordFieldCaptions.DocumentNumber, documentNumber));
    }

    protected void SaveMedicalRecordDetail() => ExecuteActionWithRetry("Save");

    protected void AssertMedicalRecordShowsNumber(string documentNumber) =>
        AssertDetailPropertyEquals(E2ETestMedicalRecordFieldCaptions.DocumentNumber, documentNumber);

    protected void UpdateMedicalRecordDocumentNumber(
        string fromNumber = E2ETestMedicalRecordCreateValues.DocumentNumber,
        string toNumber = E2ETestMedicalRecordCreateValues.UpdatedDocumentNumber)
    {
        ReturnToSavedEmployeeDetail();
        ActivatePersonNestedTab(
            E2ETestPersonNestedUi.MedicalRecordsTab,
            E2ETestPersonNestedUi.MedicalRecordsTabAlt,
            E2ETestPersonNestedUi.MedicalRecordsTabAlt2);
        EasyTestBlazorNavigationHelper.ClickListRowContaining(AppContext, fromNumber);
        WaitForDetailReady(
            "MedicalRecord_DetailView",
            E2ETestMedicalRecordFieldCaptions.DocumentNumber,
            "MedicalRecord");
        FillDetailFormWithRetry(
            new EasyTestParameter(E2ETestMedicalRecordFieldCaptions.DocumentNumber, toNumber));
        SaveMedicalRecordDetail();
        AssertMedicalRecordShowsNumber(toNumber);
    }

    /// <summary>
    /// Deletes the medical record row identified by document number. Returns false if Delete is unavailable.
    /// </summary>
    protected bool TryDeleteMedicalRecord(
        string documentNumber = E2ETestMedicalRecordCreateValues.UpdatedDocumentNumber)
    {
        try
        {
            OpenEmployeeInListByPersonalNumber(E2ETestEmployeeCreateValues.PersonalNumber);
            ActivatePersonNestedTab(
                E2ETestPersonNestedUi.MedicalRecordsTab,
                E2ETestPersonNestedUi.MedicalRecordsTabAlt,
                E2ETestPersonNestedUi.MedicalRecordsTabAlt2);
            EasyTestBlazorNavigationHelper.ClickListRowContaining(AppContext, documentNumber);
            Thread.Sleep(EasyTestCITuning.LayoutTabSettleDelay);

            if (!EasyTestBlazorNavigationHelper.TryClickToolbarActionByAnyTitle(
                    AppContext, TimeSpan.FromSeconds(5), "Delete"))
            {
                var deleteAction = AppContext.GetAction("Delete");
                if (deleteAction == null)
                    return false;
                deleteAction.Execute();
            }

            // Confirmation dialog — Yes / OK when present.
            foreach (string confirm in new[] { "Yes", "OK" })
            {
                try
                {
                    var confirmAction = AppContext.GetAction(confirm);
                    confirmAction?.Execute();
                    break;
                }
                catch (AdapterOperationException)
                {
                    // No confirmation dialog.
                }
            }

            Thread.Sleep(EasyTestCITuning.LayoutTabSettleDelay);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    // --- Position history ---

    protected void ExecutePersonPositionHistoryNestedNew()
    {
        ExecutePersonNestedNew(
            new[] { E2ETestPersonNestedUi.PositionHistoryTab, E2ETestPersonNestedUi.PositionHistoryTabAlt },
            new[] { E2ETestPersonNestedUi.PositionHistoryNewTitle, "New Position History" },
            () => IsDetailFormReady(
                "EmployeePositionHistory_DetailView",
                E2ETestPositionHistoryFieldCaptions.Position),
            "position-history");
    }

    protected void FillPositionHistoryRequiredFields()
    {
        WaitForDetailReady(
            "EmployeePositionHistory_DetailView",
            E2ETestPositionHistoryFieldCaptions.Position,
            "EmployeePositionHistory");
        FillDetailFormWithRetry(
            new EasyTestParameter(
                E2ETestPositionHistoryFieldCaptions.Position,
                E2ETestPositionHistoryCreateValues.PositionDisplay),
            new EasyTestParameter(
                E2ETestPositionHistoryFieldCaptions.ActualPosition,
                E2ETestPositionHistoryCreateValues.ActualPositionDisplay));
    }

    protected void SavePositionHistoryDetail() => ExecuteActionWithRetry("Save");

    // --- Work duty ---

    protected void ExecutePersonWorkDutiesNestedNew()
    {
        ExecutePersonNestedNew(
            new[]
            {
                E2ETestPersonNestedUi.WorkDutiesTab,
                E2ETestPersonNestedUi.WorkDutiesTabAlt,
                E2ETestPersonNestedUi.WorkDutiesTabTm,
            },
            new[]
            {
                E2ETestPersonNestedUi.WorkDutiesNewTitle,
                E2ETestPersonNestedUi.WorkDutiesNewTitleAlt,
            },
            () => IsDetailFormReady("WorkDuty_DetailView", E2ETestWorkDutyFieldCaptions.Description),
            "work-duty");
    }

    protected void FillWorkDutyRequiredFields(
        string description = E2ETestWorkDutyCreateValues.Description)
    {
        WaitForDetailReady("WorkDuty_DetailView", E2ETestWorkDutyFieldCaptions.Description, "WorkDuty");
        FillDetailFormWithRetry(
            new EasyTestParameter(E2ETestWorkDutyFieldCaptions.Description, description));
    }

    protected void SaveWorkDutyDetail() => ExecuteActionWithRetry("Save");

    protected void AssertWorkDutyShowsDescription(string description) =>
        AssertDetailPropertyEquals(E2ETestWorkDutyFieldCaptions.Description, description);

    // --- Salary ---

    protected void ExecutePersonSalariesNestedNew()
    {
        ExecutePersonNestedNew(
            new[] { E2ETestPersonNestedUi.SalariesTab },
            new[] { E2ETestPersonNestedUi.SalariesNewTitle, "New Salary" },
            () => IsDetailFormReady("EmployeeSalary_DetailView", E2ETestSalaryFieldCaptions.Amount),
            "salary");
    }

    protected void FillSalaryRequiredFields()
    {
        WaitForDetailReady("EmployeeSalary_DetailView", E2ETestSalaryFieldCaptions.Amount, "EmployeeSalary");
        FillDetailFormWithRetry(
            new EasyTestParameter(E2ETestSalaryFieldCaptions.Amount, E2ETestSalaryCreateValues.Amount),
            new EasyTestParameter(E2ETestSalaryFieldCaptions.Currency, E2ETestSalaryCreateValues.CurrencyDisplay));
    }

    protected void SaveSalaryDetail() => ExecuteActionWithRetry("Save");

    protected void AssertSalaryShowsAmount(string amount) =>
        AssertDetailPropertyEquals(E2ETestSalaryFieldCaptions.Amount, amount);

    // --- Travel (External Arrival) ---

    protected void ExecutePersonTravelExternalArrivalNestedNew()
    {
        ExecutePersonNestedNew(
            new[] { E2ETestPersonNestedUi.TravelHistoriesTab, E2ETestPersonNestedUi.TravelHistoriesTabAlt, E2ETestPersonNestedUi.TravelHistoriesTabAlt2 },
            new[] { E2ETestPersonNestedUi.TravelExternalArrivalNewTitle, "External Arrival" },
            () => IsDetailFormReady("ExternalArrival_DetailView", "Travel Date")
                  || IsDetailFormReady("TravelHistory_DetailView", "Travel Date"),
            "travel-external-arrival");
    }

    protected void SaveTravelHistoryDetail() => ExecuteActionWithRetry("Save");
}
