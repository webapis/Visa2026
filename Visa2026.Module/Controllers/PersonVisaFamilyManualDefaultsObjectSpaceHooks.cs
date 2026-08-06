using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Applies <see cref="VisaFamilyMemberLinesHelper.ApplyEmployeeDefaultIfEmpty"/> on
/// <see cref="IObjectSpace.ObjectSaving"/> so <c>Ýok</c> is set before validation rules run.
/// </summary>
internal static class PersonVisaFamilyManualDefaultsObjectSpaceHooks
{
    internal static void Subscribe(IObjectSpace objectSpace)
    {
        objectSpace.ObjectSaving += OnObjectSaving;
        objectSpace.Disposed += OnDisposed;
    }

    private static void Unsubscribe(IObjectSpace objectSpace)
    {
        objectSpace.ObjectSaving -= OnObjectSaving;
        objectSpace.Disposed -= OnDisposed;
    }

    private static void OnDisposed(object? sender, EventArgs e)
    {
        if (sender is IObjectSpace objectSpace)
        {
            Unsubscribe(objectSpace);
        }
    }

    private static void OnObjectSaving(object? sender, ObjectManipulatingEventArgs e)
    {
        if (e.Object is not Person person)
        {
            return;
        }

        if (!person.IsEmployee && person.PersonRole != PersonRecordRole.Employee)
        {
            return;
        }

        VisaFamilyMemberLinesHelper.ApplyEmployeeDefaultIfEmpty(person);
    }
}
