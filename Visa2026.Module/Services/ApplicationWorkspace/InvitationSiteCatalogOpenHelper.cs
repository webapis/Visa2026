using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Opens a lodging, hotel, hospital, or other-site detail so an officer can add a missing
/// invitation place for the city already chosen on the case.
/// </summary>
public static class InvitationSiteCatalogOpenHelper
{
    public static bool TryOpenNew(
        XafApplication application,
        string? fieldKey,
        Guid cityId,
        Action<Guid>? onSaved = null)
    {
        if (application == null || cityId == Guid.Empty)
            return false;

        var objectType = ResolveType(fieldKey);
        if (objectType == null)
            return false;

        var objectSpace = application.CreateObjectSpace(objectType);
        var target = objectSpace.CreateObject(objectType);
        var city = objectSpace.GetObjectByKey<City>(cityId);
        if (city == null || !InvitationSiteCity.Assign(target, city))
        {
            objectSpace.Dispose();
            return false;
        }

        var detailView = application.CreateDetailView(objectSpace, target, isRoot: true);
        detailView.ViewEditMode = ViewEditMode.Edit;
        detailView.Tag = new InvitationSiteCreateContext(cityId);

        if (onSaved != null && target is BaseObject saved)
        {
            objectSpace.Committed += (_, _) =>
            {
                if (saved.ID != Guid.Empty)
                    onSaved(saved.ID);
            };
        }

        application.ShowViewStrategy.ShowView(
            new ShowViewParameters(detailView) { TargetWindow = TargetWindow.NewModalWindow },
            new ShowViewSource(application.MainWindow, null));
        return true;
    }

    internal static Type? ResolveType(string? fieldKey) =>
        fieldKey switch
        {
            ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationLodging => typeof(Lodging),
            ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationHotel => typeof(Hotel),
            ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationHospital => typeof(Hospital),
            ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationOtherSite => typeof(OtherSite),
            _ => null,
        };
}

public sealed class InvitationSiteCreateContext
{
    public InvitationSiteCreateContext(Guid cityId) => CityId = cityId;

    public Guid CityId { get; }
}

internal static class InvitationSiteCity
{
    public static bool Assign(object? target, City city) =>
        target switch
        {
            Lodging lodging => Assign(city, value => lodging.City = value),
            Hotel hotel => Assign(city, value => hotel.City = value),
            Hospital hospital => Assign(city, value => hospital.City = value),
            OtherSite site => Assign(city, value => site.City = value),
            _ => false,
        };

    private static bool Assign(City city, Action<City> set)
    {
        set(city);
        return true;
    }
}