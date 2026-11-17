#nullable enable
using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Security;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.BusinessObjects.PersonIntake;
using Visa2026.Module.Editors;
using Visa2026.Module.Services.PersonIntake;

namespace Visa2026.Blazor.Server.Editors;

[PropertyEditor(typeof(string), PersonIntakeWizardEditorAliases.Wizard, false)]
public sealed class PersonIntakeWizardPropertyEditor : BlazorPropertyEditorBase, IComplexViewItem
{
    private IObjectSpace? _objectSpace;

    public PersonIntakeWizardPropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model) { }

    public override PersonIntakeWizardModel ComponentModel => (PersonIntakeWizardModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application) =>
        _objectSpace = objectSpace;

    protected override IComponentModel CreateComponentModel() => new PersonIntakeWizardModel
    {
        State = new PersonIntakeWizardState(),
        SaveRequested = EventCallback.Factory.Create(this, SaveAsync)
    };

    protected override void OnCurrentObjectChanged()
    {
        base.OnCurrentObjectChanged();
        BindPerson();
    }

    protected override void ReadValueCore()
    {
        base.ReadValueCore();
        BindPerson();
    }

    protected override void RefreshReadOnly()
    {
        base.RefreshReadOnly();
        BindPerson();
    }

    protected override object GetControlValueCore() => string.Empty;

    protected override void WriteValueCore() { }

    private void BindPerson()
    {
        var model = ComponentModel;
        if (model == null)
            return;

        Person? person;
        IObjectSpace? personSpace;
        if (CurrentObject is PersonIntakeWizardHost host)
        {
            person = host.Person;
            personSpace = host.PersonObjectSpace;
        }
        else
        {
            person = CurrentObject as Person;
            personSpace = _objectSpace ?? View?.ObjectSpace;
        }

        model.Person = person;
        model.ObjectSpace = personSpace;
        if (person != null)
            model.State.LoadFromPerson(person);
    }

    private Task SaveAsync()
    {
        var model = ComponentModel;
        if (model == null)
            return Task.CompletedTask;

        var person = model.Person;
        var objectSpace = model?.ObjectSpace
            ?? (CurrentObject as PersonIntakeWizardHost)?.PersonObjectSpace
            ?? _objectSpace;
        if (person == null || objectSpace == null)
            return Task.CompletedTask;

        if (PersonIntakeCompleteness.IsLegacyOnly(person))
            PersonIntakeCompleteness.MaterializeLegacy(person);

        var result = PersonIntakeWizardPersistHelper.TrySave(
            objectSpace,
            person,
            model.State,
            SecuritySystem.CurrentUserName ?? string.Empty);

        model.StatusMessage = result.Message;
        model.IsStatusError = !result.Succeeded;
        if (result.Succeeded)
            model.State.LoadFromPerson(person);

        ReadValueCore();
        return Task.CompletedTask;
    }
}