using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.PreviewSlot;

namespace Visa2026.Module.Services.PersonLinkedDocuments;

public static class PersonDocumentCopiesOpenHelper
{
    public static bool TryOpenForPerson(XafApplication application, View view, Person person)
    {
        if (application == null || view == null || person == null)
            return false;

        var key = view.ObjectSpace.GetKeyValue(person);
        if (key == null)
            return false;

        var personId = key is Guid guid
            ? guid
            : Guid.Parse(Convert.ToString(key, CultureInfo.InvariantCulture)!);

        return TryOpenForPersonIds(application, view, new[] { personId });
    }

    public static bool TryOpenForPersonIds(XafApplication application, View view, IReadOnlyList<Guid> personIds)
    {
        if (application == null || view == null || personIds == null)
            return false;

        var ids = personIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
            return false;

        var slotService = application.ServiceProvider?.GetService<IVisaPreviewSlotService>();
        if (slotService == null)
        {
            application.ShowViewStrategy.ShowMessage(
                VisaUiMessages.Get("PersonDocumentCopies.Preview.Error"),
                InformationType.Error);
            return false;
        }

        slotService.OpenPersonDocumentCopiesAsync(new PersonDocumentCopiesSlotRequest
        {
            PersonIds = ids,
        }, VisaPreviewSlotViewHelper.ResolveOwnerViewId(view)).GetAwaiter().GetResult();

        return true;
    }
}
