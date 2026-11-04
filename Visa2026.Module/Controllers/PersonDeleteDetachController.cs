using System;
using System.Collections.Generic;
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
/// Required-field rules stay on save. While a person delete is in flight, Save validation is
/// skipped for related rows (Person FK can be nulled before those rows are marked deleted).
/// </summary>
public sealed class PersonDeleteDetachController : ObjectViewController<ObjectView, Person>
{
    private DeleteObjectsViewController? _deleteController;
    private PersistenceValidationController? _validationController;
    private HashSet<Guid>? _deletingPersonIds;

    protected override void OnActivated()
    {
        base.OnActivated();
        _deleteController = Frame.GetController<DeleteObjectsViewController>();
        if (_deleteController != null)
            _deleteController.Deleting += OnDeleting;

        _validationController = Frame.GetController<PersistenceValidationController>();
        if (_validationController != null)
            _validationController.ContextValidating += OnContextValidating;

        ObjectSpace.Committed += OnCommittedOrReloaded;
        ObjectSpace.Reloaded += OnCommittedOrReloaded;
    }

    protected override void OnDeactivated()
    {
        if (_deleteController != null)
            _deleteController.Deleting -= OnDeleting;
        if (_validationController != null)
            _validationController.ContextValidating -= OnContextValidating;
        ObjectSpace.Committed -= OnCommittedOrReloaded;
        ObjectSpace.Reloaded -= OnCommittedOrReloaded;
        _deletingPersonIds = null;
        base.OnDeactivated();
    }

    private void OnCommittedOrReloaded(object? sender, EventArgs e) =>
        _deletingPersonIds = null;

    private void OnDeleting(object sender, DeletingEventArgs e)
    {
        _deletingPersonIds ??= new HashSet<Guid>();
        foreach (var person in e.Objects.OfType<Person>())
        {
            if (person.ID != Guid.Empty)
                _deletingPersonIds.Add(person.ID);
            ApplicationProfileInstancePersonService.DetachForPersonDelete(ObjectSpace, person);
        }
    }

    private void OnContextValidating(object sender, ContextValidatingEventArgs e)
    {
        var contextId = e.Context.Id;
        if (contextId != nameof(DefaultContexts.Save) && contextId != nameof(DefaultContexts.Delete))
            return;

        // Person delete commit: related rows can have Person nulled before delete marking.
        // Skip Save validation for the whole batch; Delete context still drops deleted targets.
        if (contextId == nameof(DefaultContexts.Save)
            && _deletingPersonIds is { Count: > 0 })
        {
            e.TargetObjects.Clear();
            return;
        }

        for (var i = e.TargetObjects.Count - 1; i >= 0; i--)
        {
            var target = e.TargetObjects[i];
            if (ObjectSpace.IsObjectToDelete(target) || IsOrphanedByTrackedPersonDelete(target))
                e.TargetObjects.RemoveAt(i);
        }
    }

    private bool IsOrphanedByTrackedPersonDelete(object target)
    {
        if (_deletingPersonIds is not { Count: > 0 })
            return false;

        return target switch
        {
            ApplicationProfileInstancePersonResolvedLink link =>
                link.Person == null || _deletingPersonIds.Contains(link.PersonId),
            WorkPermitItem wpi => wpi.Person == null || _deletingPersonIds.Contains(wpi.Person.ID),
            InvitationItem ii => ii.Person == null || (ii.Person != null && _deletingPersonIds.Contains(ii.Person.ID)),
            RejectionItem ri => ri.Person == null || (ri.Person != null && _deletingPersonIds.Contains(ri.Person.ID)),
            BorderZoneItem bzi => bzi.Person == null || (bzi.Person != null && _deletingPersonIds.Contains(bzi.Person.ID)),
            Passport passport => passport.Person == null || (passport.Person != null && _deletingPersonIds.Contains(passport.Person.ID)),
            Education education => education.Person == null || (education.Person != null && _deletingPersonIds.Contains(education.Person.ID)),
            AddressOfResidence address => address.Person == null || (address.Person != null && _deletingPersonIds.Contains(address.Person.ID)),
            EmployeePositionHistory position => position.Person == null || (position.Person != null && _deletingPersonIds.Contains(position.Person.ID)),
            EmployeeSalary salary => salary.Person == null || (salary.Person != null && _deletingPersonIds.Contains(salary.Person.ID)),
            MedicalRecord medical => medical.Person == null || (medical.Person != null && _deletingPersonIds.Contains(medical.Person.ID)),
            WorkDuty duty => duty.Person == null || (duty.Person != null && _deletingPersonIds.Contains(duty.Person.ID)),
            TravelHistory travel => travel.Person == null || (travel.Person != null && _deletingPersonIds.Contains(travel.Person.ID)),
            _ => false,
        };
    }
}