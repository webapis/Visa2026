using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Validation;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Case skip-nav links are not aggregated on the child record. Delete removes those
/// links first so the required join foreign key is not left behind.
/// </summary>
public sealed class SkipNavChildDeleteDetachController : ObjectViewController<ObjectView, BaseObject>
{
    private DeleteObjectsViewController? _deleteController;
    private PersistenceValidationController? _validationController;

    protected override void OnActivated()
    {
        base.OnActivated();
        if (!IsSkipNavChild(View.ObjectTypeInfo?.Type))
            return;

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
        foreach (var child in e.Objects.OfType<BaseObject>().ToList())
        {
            if (TryKind(child, out var kind))
                ApplicationProfileInstanceChildMembership.DetachForDelete(ObjectSpace, kind, child.ID);
        }
    }

    private void OnContextValidating(object sender, ContextValidatingEventArgs e)
    {
        var contextId = e.Context.Id;
        if (contextId != nameof(DefaultContexts.Save) && contextId != nameof(DefaultContexts.Delete))
            return;

        for (var i = e.TargetObjects.Count - 1; i >= 0; i--)
        {
            if (ObjectSpace.IsObjectToDelete(e.TargetObjects[i]))
                e.TargetObjects.RemoveAt(i);
        }
    }

    private static bool IsSkipNavChild(System.Type? type) =>
        type != null && (
            type == typeof(Passport)
            || type == typeof(Visa)
            || type == typeof(Education)
            || type == typeof(AddressOfResidence)
            || type == typeof(EmployeePositionHistory)
            || type == typeof(EmployeeSalary)
            || type == typeof(MedicalRecord)
            || type == typeof(WorkDuty)
            || type == typeof(InvitationItem)
            || type == typeof(WorkPermitItem)
            || type == typeof(BorderZoneItem)
            || typeof(TravelHistory).IsAssignableFrom(type));

    private static bool TryKind(object obj, out ApplicationProfileInstancePersonLinkKind kind)
    {
        switch (obj)
        {
            case Passport:
                kind = ApplicationProfileInstancePersonLinkKind.Passport;
                return true;
            case Visa:
                kind = ApplicationProfileInstancePersonLinkKind.Visa;
                return true;
            case Education:
                kind = ApplicationProfileInstancePersonLinkKind.Education;
                return true;
            case AddressOfResidence:
                kind = ApplicationProfileInstancePersonLinkKind.AddressOfResidence;
                return true;
            case EmployeePositionHistory:
                kind = ApplicationProfileInstancePersonLinkKind.Position;
                return true;
            case EmployeeSalary:
                kind = ApplicationProfileInstancePersonLinkKind.Salary;
                return true;
            case MedicalRecord:
                kind = ApplicationProfileInstancePersonLinkKind.MedicalRecord;
                return true;
            case WorkDuty:
                kind = ApplicationProfileInstancePersonLinkKind.WorkDuty;
                return true;
            case InvitationItem:
                kind = ApplicationProfileInstancePersonLinkKind.InvitationItem;
                return true;
            case WorkPermitItem:
                kind = ApplicationProfileInstancePersonLinkKind.WorkPermitItem;
                return true;
            case BorderZoneItem:
                kind = ApplicationProfileInstancePersonLinkKind.BorderZoneItem;
                return true;
            case TravelHistory:
                kind = ApplicationProfileInstancePersonLinkKind.TravelHistory;
                return true;
            default:
                kind = default;
                return false;
        }
    }
}