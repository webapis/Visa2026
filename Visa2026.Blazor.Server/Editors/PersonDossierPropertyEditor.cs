#nullable enable
using System;
using System.Globalization;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.BusinessObjects.PersonDossier;
using Visa2026.Module.Editors;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.PersonDossier;
using Visa2026.Module.Services.PreviewSlot;

namespace Visa2026.Blazor.Server.Editors;

[PropertyEditor(typeof(string), PersonDossierEditorAliases.Dossier, false)]
public class PersonDossierPropertyEditor : BlazorPropertyEditorBase, IComplexViewItem
{
    private XafApplication? _application;

    public PersonDossierPropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model) { }

    public override PersonDossierModel ComponentModel => (PersonDossierModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application) =>
        _application = application;

    protected override IComponentModel CreateComponentModel() => new PersonDossierModel
    {
        IsLoading = true,
        InitialLoadRequested = EventCallback.Factory.Create(this, LoadAsync),
        OpenCopiesRequested = EventCallback.Factory.Create(this, OpenCopies),
        ExportRequested = EventCallback.Factory.Create(this, QueueExport),
    };

    private Task LoadAsync()
    {
        var model = ComponentModel;

        try
        {
            // The snapshot holds only scalars, so the object space can be released immediately.
            using var objectSpace = _application?.CreateObjectSpace(typeof(Person));
            var person = objectSpace == null || CurrentPersonId == Guid.Empty
                ? null
                : objectSpace.GetObjectByKey<Person>(CurrentPersonId);

            model.Snapshot = objectSpace == null
                ? new PersonDossierSnapshot()
                : PersonDossierResolver.Resolve(objectSpace, person);
        }
        finally
        {
            model.IsLoading = false;
        }

        return Task.CompletedTask;
    }

    private void OpenCopies()
    {
        var personId = CurrentPersonId;
        if (personId == Guid.Empty)
            return;

        var slotService = _application?.ServiceProvider?.GetService<IVisaPreviewSlotService>();
        if (slotService == null)
            return;

        // Owner is the dossier view, otherwise the slot closes as soon as the officer got here.
        slotService.OpenPersonDocumentCopiesAsync(
            new PersonDocumentCopiesSlotRequest { PersonIds = new[] { personId } },
            PersonDossierViewIds.DetailView);
    }

    private void QueueExport()
    {
        var model = ComponentModel;
        model.ExportMessage = null;

        var personId = CurrentPersonId;
        var services = _application?.ServiceProvider;
        if (personId == Guid.Empty || services == null)
        {
            model.ExportMessage = VisaUiMessages.Get("PersonDossier.Export.ErrorNoPerson");
            return;
        }

        var enqueueService = services.GetService<PersonExportBatchEnqueueService>();
        if (enqueueService == null)
        {
            model.ExportMessage = VisaUiMessages.Get("PersonDossier.Export.ErrorFailed");
            return;
        }

        using var objectSpace = _application!.CreateObjectSpace(typeof(Person));
        var person = objectSpace.GetObjectByKey<Person>(personId);
        string requestedBy = SecuritySystem.CurrentUserName ?? string.Empty;

        if (!enqueueService.TryEnqueuePerson(
                objectSpace,
                person,
                requestedBy,
                CultureInfo.CurrentUICulture.Name,
                out var result,
                out var errorMessageKey)
            || result == null)
        {
            model.ExportMessage = VisaUiMessages.Get(errorMessageKey ?? "PersonDossier.Export.ErrorFailed");
            return;
        }

        // Hand the id to the global toast, which lives outside this component tree.
        services.GetService<IPersonExportBatchTrackNotifier>()?.TrackQueuedBatch(result.BatchId, requestedBy);

        model.IsExportQueued = true;
        model.ExportMessage = VisaUiMessages.Get("PersonDossier.Export.Queued");
    }

    private Guid CurrentPersonId =>
        CurrentObject is PersonDossierHost host ? host.PersonId : Guid.Empty;
}
