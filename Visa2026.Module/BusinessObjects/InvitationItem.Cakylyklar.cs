using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Module.BusinessObjects;

public partial class InvitationItem
{
    private string? listViewLifeKey;
    private string? listViewLinkedDetail;
    private string? listViewProcessNumber;
    private string? listViewInvitationNumber;
    private string? listViewPersonName;
    private string? listViewPassportNumber;
    private Guid? listViewVisaId;
    private Guid? listViewApplicationProfileInstanceId;

    /// <summary>Stamped <see cref="InvitationLaterLife"/> key for Çakylyklar tabs/rows.</summary>
    public void SetListViewLifeKey(string? lifeKey) => listViewLifeKey = lifeKey;

    public void SetListViewLinkedDetail(string? detail) => listViewLinkedDetail = detail;

    public void SetListViewProcessNumber(string? processNumber) => listViewProcessNumber = processNumber;

    public void SetListViewInvitationNumber(string? invitationNumber) =>
        listViewInvitationNumber = invitationNumber;

    public void SetListViewPersonName(string? name) => listViewPersonName = name;

    public void SetListViewPassportNumber(string? passportNumber) =>
        listViewPassportNumber = passportNumber;

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