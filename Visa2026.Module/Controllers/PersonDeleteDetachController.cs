using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Validation;
using DevExpress.Persistent.Validation;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Employees list delete removes the person record. Case roster links are not aggregated on
/// <see cref="Person"/>, so they must be removed first or required-field rules fire
/// (<c>Person</c> / case link fields must not be empty).
/// Required-field rules stay on save. Rows already marked for deletion are left out of that
/// check so an incomplete person, and the records removed with them, can still be deleted.
/// </summary>
public sealed class PersonDeleteDetachController : ObjectViewController<ObjectView, Person>
{
    private DeleteObjectsViewController? _deleteController;
    private PersistenceValidationController? _validationController;

    protected override void OnActivated()
    {
        base.OnActivated();
        _deleteController = Frame.GetController<DeleteObjectsViewController>();
        if (_deleteController != null)
            _deleteController.Deleting += OnDeleting;

        _validationController = Frame.GetController<PersistenceValidationController>();
        if (_validationController != null)
            _validationController.ContextValidating += OnContextValidating;
    }

    protected override void OnDeactivated()
    {
        if (_deleteController != null)
            _deleteController.Deleting -= OnDeleting;
        if (_validationController != null)
            _validationController.ContextValidating -= OnContextValidating;
        base.OnDeactivated();
    }

    private void OnDeleting(object sender, DeletingEventArgs e)
    {
        foreach (var person in e.Objects.OfType<Person>().ToList())
            ApplicationProfileInstancePersonService.DetachForPersonDelete(ObjectSpace, person);
    }

    private void OnContextValidating(object sender, ContextValidatingEventArgs e)
    {
        if (e.Context?.Id != DefaultContexts.Save && e.Context?.Id != DefaultContexts.Delete)
            return;

        for (var i = e.TargetObjects.Count - 1; i >= 0; i--)
        {
            if (ObjectSpace.IsObjectToDelete(e.TargetObjects[i]))
                e.TargetObjects.RemoveAt(i);
        }
    }
}
