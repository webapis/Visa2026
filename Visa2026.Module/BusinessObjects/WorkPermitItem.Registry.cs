using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Module.BusinessObjects;

public partial class WorkPermitItem
{
    private string? listViewLifeKey;
    private string? listViewLinkedDetail;
    private string? listViewProcessNumber;
    private string? listViewPersonName;
    private string? listViewPassportNumber;
    private string? listViewDepartmentName;
    private string? listViewWorkPermitNumber;
    private Guid? listViewApplicationProfileInstanceId;
    private Guid? listViewWorkPermitId;

    public void SetListViewLifeKey(string? lifeKey) => listViewLifeKey = lifeKey;

    public void SetListViewLinkedDetail(string? detail) => listViewLinkedDetail = detail;

    public void SetListViewProcessNumber(string? processNumber) => listViewProcessNumber = processNumber;

    public void SetListViewPersonName(string? name) => listViewPersonName = name;

    public void SetListViewPassportNumber(string? passportNumber) =>
        listViewPassportNumber = passportNumber;

    public void SetListViewDepartmentName(string? departmentName) =>
        listViewDepartmentName = departmentName;

    public void SetListViewWorkPermitNumber(string? workPermitNumber) =>
        listViewWorkPermitNumber = workPermitNumber;

    public void SetListViewApplicationProfileInstanceId(Guid? applicationId) =>
        listViewApplicationProfileInstanceId = applicationId;

    public void SetListViewWorkPermitId(Guid? workPermitId) =>
        listViewWorkPermitId = workPermitId;

    [Browsable(false)]
    [NotMapped]
    public string? ListViewLifeKey => listViewLifeKey;

    [Browsable(false)]
    [NotMapped]
    public Guid? ListViewApplicationProfileInstanceId => listViewApplicationProfileInstanceId;

    [Browsable(false)]
    [NotMapped]
    public Guid? ListViewWorkPermitId => listViewWorkPermitId;

    [Browsable(false)]
    [NotMapped]
    public string WorkPermitColorTabKey => WorkPermitItemColorTab.ResolveBucketKey(this);

    [Browsable(false)]
    [NotMapped]
    public string ListRowCssClass =>
        WorkPermitItemColorTab.GetRowCssClass(WorkPermitColorTabKey);

    [NotMapped]
    [ModelDefault("AllowEdit", "False")]
    [VisibleInDetailView(false)]
    [VisibleInLookupListView(false)]
    [VisibleInListView(true)]
    [XafDisplayName("Person")]
    public string RegistryPersonDisplay =>
        listViewPersonName
        ?? Person?.FullName
        ?? string.Empty;

    [NotMapped]
    [ModelDefault("AllowEdit", "False")]
    [VisibleInDetailView(false)]
    [VisibleInLookupListView(false)]
    [VisibleInListView(true)]
    [XafDisplayName("Passport")]
    public string RegistryPassportDisplay =>
        listViewPassportNumber
        ?? Passport?.PassportNumber
        ?? string.Empty;

    [NotMapped]
    [ModelDefault("AllowEdit", "False")]
    [VisibleInDetailView(false)]
    [VisibleInLookupListView(false)]
    [VisibleInListView(true)]
    [XafDisplayName("Department")]
    public string RegistryDepartmentDisplay =>
        listViewDepartmentName
        ?? (Person != null ? PersonCurrentItems.FormatEmployeeListDepartmentName(Person) : string.Empty);

    [NotMapped]
    [ModelDefault("AllowEdit", "False")]
    [VisibleInDetailView(false)]
    [VisibleInLookupListView(false)]
    [VisibleInListView(true)]
    [XafDisplayName("Work permit")]
    public string RegistryWorkPermitNumberDisplay =>
        listViewWorkPermitNumber
        ?? WorkPermitNumber
        ?? string.Empty;

    [NotMapped]
    [ModelDefault("AllowEdit", "False")]
    [VisibleInDetailView(false)]
    [VisibleInLookupListView(false)]
    [VisibleInListView(true)]
    [XafDisplayName("Days remaining")]
    public int RegistryDaysRemaining =>
        ExpirationLogicHelper.CalculateDaysRemaining(ExpirationDate, IsCancelled);

    [NotMapped]
    [ModelDefault("AllowEdit", "False")]
    [VisibleInDetailView(false)]
    [VisibleInLookupListView(false)]
    [VisibleInListView(true)]
    [XafDisplayName("State")]
    public string RegistryStateDisplay =>
        WorkPermitItemColorTab.GetLabel(WorkPermitColorTabKey);

    [NotMapped]
    [ModelDefault("AllowEdit", "False")]
    [VisibleInDetailView(false)]
    [VisibleInLookupListView(false)]
    [VisibleInListView(true)]
    [XafDisplayName("CO")]
    public string RegistryProcessNumberDisplay =>
        listViewProcessNumber
        ?? FormatProcessWithDate(
            WorkPermit?.ApplicationProfileInstance?.ProcessNumber,
            WorkPermit?.ApplicationProfileInstance?.ApplicationDate);

    [NotMapped]
    [ModelDefault("AllowEdit", "False")]
    [VisibleInDetailView(false)]
    [VisibleInLookupListView(false)]
    [VisibleInListView(true)]
    [XafDisplayName("Linked detail")]
    public string RegistryLinkedDetailDisplay =>
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
}