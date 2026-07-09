using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Canonical header parent + item ListView columns (data fields + document-copies link).
/// Applied after all xafml and per-user model layers merge (see <see cref="HeaderParentListViewConfigurator"/>).
/// </summary>
internal static class HeaderParentListViewColumns
{
    private static readonly Dictionary<string, Type> ViewBusinessTypes = new(StringComparer.Ordinal)
    {
        ["Invitation_ListView"] = typeof(BusinessObjects.Invitation),
        ["WorkPermit_ListView"] = typeof(BusinessObjects.WorkPermit),
        ["Rejection_ListView"] = typeof(BusinessObjects.Rejection),
        ["BorderZone_ListView"] = typeof(BusinessObjects.BorderZone),
        ["InvitationItem_ListView"] = typeof(BusinessObjects.InvitationItem),
        ["WorkPermitItem_ListView"] = typeof(BusinessObjects.WorkPermitItem),
        ["RejectionItem_ListView"] = typeof(BusinessObjects.RejectionItem),
        ["BorderZoneItem_ListView"] = typeof(BusinessObjects.BorderZoneItem),
    };

    private static readonly (string ViewId, string[] Columns, int[] Widths)[] ParentListViews =
    [
        (
            "Invitation_ListView",
            [
                nameof(BusinessObjects.Invitation.InvitationNumber),
                nameof(BusinessObjects.Invitation.DocumentCopiesListLink),
                nameof(BusinessObjects.Invitation.StartDate),
                nameof(BusinessObjects.Invitation.ExpirationDate),
                nameof(BusinessObjects.Invitation.DaysRemaining),
                nameof(BusinessObjects.Invitation.ValidityDuration),
            ],
            [110, 56, 100, 100, 72, 120]
        ),
        (
            "WorkPermit_ListView",
            [
                nameof(BusinessObjects.WorkPermit.WorkPermitNumber),
                nameof(BusinessObjects.WorkPermit.DocumentCopiesListLink),
                nameof(BusinessObjects.WorkPermit.IssuedDate),
            ],
            [120, 56, 100]
        ),
        (
            "Rejection_ListView",
            [
                nameof(BusinessObjects.Rejection.RejectionTitle),
                nameof(BusinessObjects.Rejection.DocumentCopiesListLink),
                nameof(BusinessObjects.Rejection.Date),
                nameof(BusinessObjects.Rejection.RejectedDocNumber),
                nameof(BusinessObjects.Rejection.Application),
                nameof(BusinessObjects.Rejection.Reason),
            ],
            [200, 56, 100, 120, 140, 240]
        ),
        (
            "BorderZone_ListView",
            [
                nameof(BusinessObjects.BorderZone.BorderZoneNumber),
                nameof(BusinessObjects.BorderZone.DocumentCopiesListLink),
                nameof(BusinessObjects.BorderZone.StartDate),
                nameof(BusinessObjects.BorderZone.ExpirationDate),
                nameof(BusinessObjects.BorderZone.DaysRemaining),
                nameof(BusinessObjects.BorderZone.ValidityDuration),
                nameof(BusinessObjects.BorderZone.Application),
            ],
            [110, 56, 100, 100, 72, 120, 140]
        ),
    ];

    private static readonly (string ViewId, string[] Columns, int[] Widths)[] ItemListViews =
    [
        (
            "InvitationItem_ListView",
            [
                nameof(BusinessObjects.InvitationItem.InvitationItemName),
                nameof(BusinessObjects.InvitationItem.DocumentCopiesListLink),
                nameof(BusinessObjects.InvitationItem.Person),
                nameof(BusinessObjects.InvitationItem.Passport),
                nameof(BusinessObjects.InvitationItem.Invitation),
                nameof(BusinessObjects.InvitationItem.IsCancelled),
                nameof(BusinessObjects.InvitationItem.IsUsed),
            ],
            [180, 56, 120, 100, 120, 80, 72]
        ),
        (
            "WorkPermitItem_ListView",
            [
                nameof(BusinessObjects.WorkPermitItem.WorkPermitItemName),
                nameof(BusinessObjects.WorkPermitItem.DocumentCopiesListLink),
                nameof(BusinessObjects.WorkPermitItem.Person),
                nameof(BusinessObjects.WorkPermitItem.WorkPermit),
                nameof(BusinessObjects.WorkPermitItem.WorkPermitNumber),
                nameof(BusinessObjects.WorkPermitItem.StartDate),
                nameof(BusinessObjects.WorkPermitItem.ExpirationDate),
                nameof(BusinessObjects.WorkPermitItem.DaysRemaining),
                nameof(BusinessObjects.WorkPermitItem.WorkPermittedLocations),
                nameof(BusinessObjects.WorkPermitItem.ASNumber),
            ],
            [180, 56, 120, 120, 90, 100, 100, 72, 240, 90]
        ),
        (
            "RejectionItem_ListView",
            [
                nameof(BusinessObjects.RejectionItem.RejectionItemName),
                nameof(BusinessObjects.RejectionItem.DocumentCopiesListLink),
                nameof(BusinessObjects.RejectionItem.Person),
                nameof(BusinessObjects.RejectionItem.Passport),
                nameof(BusinessObjects.RejectionItem.Reason),
            ],
            [200, 56, 120, 100, 200]
        ),
        (
            "BorderZoneItem_ListView",
            [
                nameof(BusinessObjects.BorderZoneItem.Person),
                nameof(BusinessObjects.BorderZoneItem.DocumentCopiesListLink),
                nameof(BusinessObjects.BorderZoneItem.Passport),
                nameof(BusinessObjects.BorderZoneItem.BorderZone),
            ],
            [140, 56, 100, 140]
        ),
    ];

    internal static void ApplyToViews(IModelViews views)
    {
        ApplyLayouts(views, ParentListViews);
        ApplyLayouts(views, ItemListViews);
    }

    private static void ApplyLayouts(
        IModelViews views,
        IEnumerable<(string ViewId, string[] Columns, int[] Widths)> layouts)
    {
        foreach (var (viewId, columns, widths) in layouts)
        {
            if (views[viewId] is not IModelListView listView)
                continue;

            ApplyToListView(listView, viewId, columns, widths);
        }
    }

    private static void ApplyToListView(IModelListView listView, string viewId, string[] columns, int[] widths)
    {
        ITypeInfo? typeInfo = ResolveTypeInfo(listView, viewId);
        if (typeInfo == null)
            return;

        PruneInvalidColumns(listView, typeInfo);

        var allowed = new HashSet<string>(columns, StringComparer.Ordinal);
        for (int i = 0; i < columns.Length; i++)
            EnsureColumn(listView, columns[i], i, widths[i]);

        foreach (IModelColumn column in listView.Columns)
        {
            string propertyName = column.PropertyName ?? column.Id;
            if (!allowed.Contains(propertyName))
                column.Index = -1;
        }
    }

    private static ITypeInfo? ResolveTypeInfo(IModelListView listView, string viewId)
    {
        if (listView.ModelClass?.TypeInfo != null)
            return listView.ModelClass.TypeInfo;

        if (!ViewBusinessTypes.TryGetValue(viewId, out Type? businessType))
            return null;

        return XafTypesInfo.Instance.FindTypeInfo(businessType);
    }

    private static void PruneInvalidColumns(IModelListView listView, ITypeInfo typeInfo)
    {
        foreach (IModelColumn column in listView.Columns.ToList())
        {
            string propertyName = column.PropertyName ?? column.Id;
            if (string.IsNullOrEmpty(propertyName))
                continue;

            if (typeInfo.FindMember(propertyName) == null)
                column.Index = -1;
        }
    }

    private static void EnsureColumn(IModelListView listView, string propertyName, int index, int width)
    {
        var column = listView.Columns[propertyName]
            ?? listView.Columns.AddNode<IModelColumn>(propertyName);
        column.PropertyName = propertyName;
        column.Index = index;
        column.Width = width;
        if (propertyName.EndsWith("DocumentCopiesListLink", StringComparison.Ordinal))
            column.Caption = " ";
    }
}
