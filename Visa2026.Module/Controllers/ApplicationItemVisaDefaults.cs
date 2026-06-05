using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Shared defaults when a new <see cref="Visa"/> is created in the context of an <see cref="ApplicationItem"/>.
/// </summary>
public static class ApplicationItemVisaDefaults
{
    internal const string PassportLockedByApplicationItemKey = ApplicationItemPersonLinkedDefaults.LockedByApplicationItemKey;

    public static bool TryApply(
        Visa visa,
        ApplicationItem appItem,
        IObjectSpace visaObjectSpace,
        XafApplication? application)
    {
        if (visa.Passport != null)
            return true;

        var parentObjectSpace = ObjectSpaceHelper.Get(appItem) ?? visaObjectSpace;
        var passport = ResolvePassport(appItem, parentObjectSpace);
        if (passport == null)
        {
            application?.ShowViewStrategy.ShowMessage(
                "Please create or select Current Passport first, then create Visa.",
                InformationType.Warning);
            return false;
        }

        visa.Passport = visaObjectSpace.GetObject(passport);

        if (visa.IssuingApplicationItem == null
            && appItem.Application?.ApplicationType != null
            && VisaIssuingApplicationTypes.IsAllowed(appItem.Application.ApplicationType)
            && passport.Person != null
            && appItem.Person != null
            && appItem.Person.ID == passport.Person.ID)
        {
            visa.IssuingApplicationItem = parentObjectSpace.IsNewObject(appItem)
                ? visaObjectSpace.GetObject(appItem)
                : visaObjectSpace.GetObjectByKey<ApplicationItem>(appItem.ID);
        }

        return true;
    }

    public static void LockPassportEditor(DetailView visaDetailView)
    {
        if (visaDetailView.FindItem(nameof(Visa.Passport)) is PropertyEditor passportEditor)
            passportEditor.AllowEdit[PassportLockedByApplicationItemKey] = false;
    }

    public static Passport? ResolvePassport(ApplicationItem appItem, IObjectSpace objectSpace)
    {
        var item = objectSpace.GetObject(appItem) ?? appItem;

        if (item.CurrentPassport != null)
            return objectSpace.GetObject(item.CurrentPassport);

        if (item.Person == null)
            return null;

        var person = objectSpace.GetObject(item.Person);
        var currentPassport = PersonCurrentItems.GetCurrentPassport(person);
        return currentPassport == null ? null : objectSpace.GetObject(currentPassport);
    }
}
