using System;
using System.Globalization;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.BusinessObjects.PersonDossier;

namespace Visa2026.Module.Services.PersonDossier;

/// <summary>
/// Builds the dossier detail view for a person. Callers assign the result to
/// <c>ShowViewParameters.CreatedView</c> so XAF owns navigation.
/// </summary>
public static class PersonDossierOpenHelper
{
    public static DetailView? CreateDossierView(XafApplication application, IObjectSpace sourceObjectSpace, Person person)
    {
        if (application == null || sourceObjectSpace == null || person == null)
            return null;

        var personId = ResolveId(sourceObjectSpace, person);
        return personId == null ? null : CreateDossierView(application, personId.Value);
    }

    public static DetailView? CreateDossierView(XafApplication application, Guid personId)
    {
        if (application == null || personId == Guid.Empty)
            return null;

        PersonDossierPendingOpenGate.Set(application, personId);

        var objectSpace = application.CreateObjectSpace(typeof(PersonDossierHost));
        var host = objectSpace.CreateObject<PersonDossierHost>();
        host.PersonId = personId;

        var detailView = application.CreateDetailView(objectSpace, host);
        detailView.ViewEditMode = ViewEditMode.View;
        return detailView;
    }

    private static Guid? ResolveId(IObjectSpace objectSpace, Person person)
    {
        var key = objectSpace.GetKeyValue(person);
        return key switch
        {
            Guid guid => guid,
            null => null,
            _ => Guid.TryParse(Convert.ToString(key, CultureInfo.InvariantCulture), out var parsed)
                ? parsed
                : null,
        };
    }
}
