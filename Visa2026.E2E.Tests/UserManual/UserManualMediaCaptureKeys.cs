namespace Visa2026.E2E.Tests.UserManual;

/// <summary>
/// Screenshot capture keys for officer manual assets. Each key matches the PNG file stem
/// referenced in guide markdown (<c>assets/screenshots/v*/{locale}/{key}.png</c>) and the
/// <c>&lt;!-- media-capture: {key} --&gt;</c> anchor above the image.
/// </summary>
internal static class UserManualMediaCaptureKeys
{
    internal const string LoginStep01Logon = "login-step-01-logon";
    internal const string LoginStep02ReportDashboard = "login-step-02-report-dashboard";

    internal const string NavigationStep01Shell = "navigation-step-01-shell";
    internal const string NavigationStep02LeftMenu = "navigation-step-02-left-menu";
    internal const string NavigationStep03EmployeesList = "navigation-step-03-employees-list";
    internal const string NavigationStep04DetailForm = "navigation-step-04-detail-form";

    internal const string PersonRegisterStep01EmployeesList = "person-register-step-01-employees-list";
    internal const string PersonRegisterStep02SavedDetail = "person-register-step-02-saved-detail";
    internal const string PersonRegisterStep03OpenFromList = "person-register-step-03-open-from-list";

    internal const string PersonAddPassportStep01EmployeeDetail = "person-add-passport-step-01-employee-detail";
    internal const string PersonAddPassportStep02PassportFormNew = "person-add-passport-step-02-passport-form-new";
    internal const string PersonAddPassportStep03PassportFieldsFilled = "person-add-passport-step-03-passport-fields-filled";
    internal const string PersonAddPassportStep04PassportSaved = "person-add-passport-step-04-passport-saved";

    internal const string PersonAddVisaStep01PassportDetail = "person-add-visa-step-01-passport-detail";
    internal const string PersonAddVisaStep02VisaFormNew = "person-add-visa-step-02-visa-form-new";
    internal const string PersonAddVisaStep03VisaFieldsFilled = "person-add-visa-step-03-visa-fields-filled";
    internal const string PersonAddVisaStep04VisaSaved = "person-add-visa-step-04-visa-saved";

    internal const string EmployeeVisaFamilyManualStep01Field = "employee-visa-family-manual-step-01-field";
    internal const string EmployeeVisaFamilyManualStep02PopupOpen = "employee-visa-family-manual-step-02-popup-open";
    internal const string EmployeeVisaFamilyManualStep03AddMemberForm = "employee-visa-family-manual-step-03-add-member-form";
    internal const string EmployeeVisaFamilyManualStep04PopupWithMember = "employee-visa-family-manual-step-04-popup-with-member";
    internal const string EmployeeVisaFamilyManualStep05SavedSummary = "employee-visa-family-manual-step-05-saved-summary";

    internal const string PersonRegisterFamilyMemberStep01FamilyMembersList = "person-register-family-member-step-01-family-members-list";
    internal const string PersonRegisterFamilyMemberStep02SavedDetail = "person-register-family-member-step-02-saved-detail";
    internal const string PersonRegisterFamilyMemberStep03OpenFromList = "person-register-family-member-step-03-open-from-list";

    // Legacy milestone labels — still captured for guides not yet migrated to doc keys.
    internal const string Legacy00LogonPage = "00-logon-page";
    internal const string Legacy01AfterLogin = "01-after-login";
    internal const string Legacy02EmployeesList = "02-employees-list";
    internal const string Legacy03EmployeeCreated = "03-employee-created";
    internal const string Legacy04EmployeeDetail = "04-employee-detail";
    internal const string Legacy05PassportDetailNew = "05-passport-detail-new";
    internal const string Legacy06PassportFieldsFilled = "06-passport-fields-filled";
    internal const string Legacy07PassportSaved = "07-passport-saved";
}
