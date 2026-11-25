using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Module.BusinessObjects;

public partial class InvitationItem
{
    private string? listViewLifeKey;
    private string? listViewLinkedDetail;
    private string? listViewProcessNumber;
    private string? listViewApplicationNumber;
    private string? listViewInvitationNumber;
    private string? listViewPersonName;
    private string? listViewPassportNumber;
    private string? listViewDepartmentName;
    private string? listViewWorkPermitNumber;
    private string? listViewVisaTypeName;
    private string? listViewVisaCategoryName;
    private string? listViewVisaPeriodName;
    private Guid? listViewVisaId;
    private Guid? listViewApplicationProfileInstanceId;
    private Guid? listViewWorkPermitId;
    private Guid? listViewWorkPermitItemId;

    /// <summary>Stamped <see cref="InvitationLaterLife"/> key for Çakylyklar tabs/rows.</summary>
    public void SetListViewLifeKey(string? lifeKey) => listViewLifeKey = lifeKey;

    public void SetListViewLinkedDetail(string? detail) => listViewLinkedDetail = detail;

    public void SetListViewProcessNumber(string? processNumber) => listViewProcessNumber = processNumber;

    public void SetListViewApplicationNumber(string? applicationNumber) =>
        listViewApplicationNumber = applicationNumber;

    public void SetListViewInvitationNumber(string? invitationNumber) =>
        listViewInvitationNumber = invitationNumber;

    public void SetListViewPersonName(string? name) => listViewPersonName = name;

    public void SetListViewPassportNumber(string? passportNumber) =>
        listViewPassportNumber = passportNumber;

    public void SetListViewDepartmentName(string? departmentName) =>
        listViewDepartmentName = departmentName;

    public void SetListViewWorkPermit(
        string? workPermitNumber,
        Guid? workPermitId,
        Guid? workPermitItemId)
    {
        listViewWorkPermitNumber = workPermitNumber;
        listViewWorkPermitId = workPermitId;
        listViewWorkPermitItemId = workPermitItemId;
    }

    public void SetListViewVisaLookups(
        string? visaTypeName,
        string? visaCategoryName,
        string? visaPeriodName)
    {
        listViewVisaTypeName = visaTypeName;
        listViewVisaCategoryName = visaCategoryName;
        listViewVisaPeriodName = visaPeriodName;
    }

    public void SetListViewVisaId(Guid? visaId) => listViewVisaId = visaId;

    public void SetListViewApplicationProfileInstanceId(Guid? applicationId) =>
        listViewApplicationProfileInstanceId = applicationId;

    [Browsable(false)]
    [NotMapped]
    public string? ListViewLifeKey => listViewLifeKey;

    /// <summary>Issued visa id when life is Used (Çakylyklar Linked detail preview link).</summary>
    [Browsable(false)]
    [NotMapped]
    public Guid? ListViewVisaId => listViewVisaId;

    /// <summary>Owning Application Profile Instance for the invitation (Çakylyklar CO link).</summary>
    [Browsable(false)]
    [NotMapped]
    public Guid? ListViewApplicationProfileInstanceId => listViewApplicationProfileInstanceId;

    /// <summary>Parent WorkPermit id for Çakylyklar work-permit copy preview.</summary>
    [Browsable(false)]
    [NotMapped]
    public Guid? ListViewWorkPermitId => listViewWorkPermitId;

    /// <summary>WorkPermitItem id used as header-copies context on Çakylyklar.</summary>
    [Browsable(false)]
    [NotMapped]
    public Guid? ListViewWorkPermitItemId => listViewWorkPermitItemId;

    /// <summary>Color-tab filter key for Çakylyklar ListView.</summary>
    [Browsable(false)]
    [NotMapped]
    public string InvitationColorTabKey => InvitationItemColorTab.ResolveBucketKey(this);

    [Browsable(false)]
    [NotMapped]
    public string ListRowCssClass =>
        InvitationItemColorTab.GetRowCssClass(InvitationColorTabKey);

    [NotMapped]
    [ModelDefault("AllowEdit", "False")]
    [VisibleInDetailView(false)]
    [VisibleInLookupListView(false)]
    [VisibleInListView(true)]
    [XafDisplayName("Person")]
    public string CakylyklarPersonDisplay =>
        listViewPersonName
        ?? Person?.FullName
        ?? string.Empty;

    [NotMapped]
    [ModelDefault("AllowEdit", "False")]
    [VisibleInDetailView(false)]
    [VisibleInLookupListView(false)]
    [VisibleInListView(true)]
    [XafDisplayName("Passport")]
    public string CakylyklarPassportDisplay =>
        listViewPassportNumber
        ?? Passport?.PassportNumber
        ?? string.Empty;

    [NotMapped]
    [ModelDefault("AllowEdit", "False")]
    [VisibleInDetailView(false)]
    [VisibleInLookupListView(false)]
    [VisibleInListView(true)]
    [XafDisplayName("Department")]
    public string CakylyklarDepartmentDisplay =>
        listViewDepartmentName
        ?? (Person != null ? PersonCurrentItems.FormatEmployeeListDepartmentName(Person) : string.Empty);

    /// <summary>
    /// WorkPermitItem number from the same issuing Application Profile Instance (and person).
    /// </summary>
    [NotMapped]
    [ModelDefault("AllowEdit", "False")]
    [VisibleInDetailView(false)]
    [VisibleInLookupListView(false)]
    [VisibleInListView(true)]
    [XafDisplayName("Work permit")]
    public string CakylyklarWorkPermitDisplay =>
        listViewWorkPermitNumber ?? string.Empty;

    [NotMapped]
    [ModelDefault("AllowEdit", "False")]
    [VisibleInDetailView(false)]
    [VisibleInLookupListView(false)]
    [VisibleInListView(true)]
    [XafDisplayName("Visa type")]
    public string CakylyklarVisaTypeDisplay =>
        listViewVisaTypeName
        ?? LookupLocalization.GetDisplayName(
            Invitation?.ApplicationProfileInstance?.VisaType
            ?? IssuedVisa?.VisaType);

    [NotMapped]
    [ModelDefault("AllowEdit", "False")]
    [VisibleInDetailView(false)]
    [VisibleInLookupListView(false)]
    [VisibleInListView(true)]
    [XafDisplayName("Visa category")]
    public string CakylyklarVisaCategoryDisplay =>
        listViewVisaCategoryName
        ?? LookupLocalization.GetDisplayName(
            Invitation?.VisaCategory
            ?? Invitation?.ApplicationProfileInstance?.VisaCategory
            ?? IssuedVisa?.VisaCategory);

    [NotMapped]
    [ModelDefault("AllowEdit", "False")]
    [VisibleInDetailView(false)]
    [VisibleInLookupListView(false)]
    [VisibleInListView(true)]
    [XafDisplayName("Visa period")]
    public string CakylyklarVisaPeriodDisplay =>
        listViewVisaPeriodName
        ?? LookupLocalization.GetDisplayName(
            Invitation?.VisaPeriod
            ?? Invitation?.ApplicationProfileInstance?.VisaPeriod);

    /// <summary>
    /// Calendar days until invitation letter expiry (0 when expired or missing).
    /// Especially relevant for Taýýar (Ready) rows.
    /// </summary>
    [NotMapped]
    [ModelDefault("AllowEdit", "False")]
    [VisibleInDetailView(false)]
    [VisibleInLookupListView(false)]
    [VisibleInListView(true)]
    [XafDisplayName("Days remaining")]
    public int CakylyklarDaysRemaining =>
        ExpirationLogicHelper.CalculateDaysRemaining(ExpirationDate);

    /// <summary>Localized lifecycle label (same text as the color tab for this row's state).</summary>
    [NotMapped]
    [ModelDefault("AllowEdit", "False")]
    [VisibleInDetailView(false)]
    [VisibleInLookupListView(false)]
    [VisibleInListView(true)]
    [XafDisplayName("State")]
    public string CakylyklarStateDisplay =>
        InvitationItemColorTab.GetLabel(InvitationColorTabKey);

    [NotMapped]
    [ModelDefault("AllowEdit", "False")]
    [VisibleInDetailView(false)]
    [VisibleInLookupListView(false)]
    [VisibleInListView(true)]
    [XafDisplayName("Invitation №")]
    public string CakylyklarInvitationNumberDisplay =>
        listViewInvitationNumber
        ?? Invitation?.InvitationNumber
        ?? string.Empty;

    /// <summary>Process number · application date for the invitation's Application Profile Instance.</summary>
    [NotMapped]
    [ModelDefault("AllowEdit", "False")]
    [VisibleInDetailView(false)]
    [VisibleInLookupListView(false)]
    [VisibleInListView(true)]
    [XafDisplayName("CO")]
    public string CakylyklarProcessNumberDisplay =>
        listViewProcessNumber
        ?? FormatProcessFallback(
            Invitation?.ApplicationProfileInstance?.ProcessNumber,
            Invitation?.ApplicationProfileInstance?.ApplicationDate);

    [NotMapped]
    [ModelDefault("AllowEdit", "False")]
    [VisibleInDetailView(false)]
    [VisibleInLookupListView(false)]
    [VisibleInListView(true)]
    [XafDisplayName("Linked detail")]
    public string CakylyklarLinkedDetailDisplay =>
        listViewLinkedDetail ?? string.Empty;

    /// <summary>Full application number · date for the issuing Application Profile Instance.</summary>
    [NotMapped]
    [ModelDefault("AllowEdit", "False")]
    [VisibleInDetailView(false)]
    [VisibleInLookupListView(false)]
    [VisibleInListView(true)]
    [XafDisplayName("Application")]
    public string CakylyklarApplicationDisplay =>
        listViewApplicationNumber
        ?? FormatProcessFallback(
            PreferApplicationNumber(Invitation?.ApplicationProfileInstance),
            Invitation?.ApplicationProfileInstance?.ApplicationDate);

    private static string? PreferApplicationNumber(ApplicationProfileInstance? app)
    {
        if (app == null)
            return null;
        if (!string.IsNullOrWhiteSpace(app.FullApplicationNumber))
            return app.FullApplicationNumber.Trim();
        return string.IsNullOrWhiteSpace(app.ApplicationNumber) ? null : app.ApplicationNumber.Trim();
    }

    internal static string FormatProcessWithDate(string? processNumber, DateTime? applicationDate)
    {
        var number = string.IsNullOrWhiteSpace(processNumber) ? string.Empty : processNumber.Trim();
        var date = applicationDate?.ToString("dd.MM.yyyy") ?? string.Empty;
        if (number.Length == 0)
            return date;
        if (date.Length == 0)
            return number;
        return number + " · " + date;
    }

    private static string FormatProcessFallback(string? processNumber, DateTime? applicationDate) =>
        FormatProcessWithDate(processNumber, applicationDate);
}