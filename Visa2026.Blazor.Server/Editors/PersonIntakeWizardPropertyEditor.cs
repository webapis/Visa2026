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
    private XafApplication? _application;
    private Guid? _boundPersonId;

    public PersonIntakeWizardPropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model) { }

    public override PersonIntakeWizardModel ComponentModel => (PersonIntakeWizardModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application)
    {
        _objectSpace = objectSpace;
        _application = application;
    }

    protected override IComponentModel CreateComponentModel()
    {
        var model = new PersonIntakeWizardModel
        {
            State = new PersonIntakeWizardState(),
            SaveRequested = EventCallback.Factory.Create(this, SaveAsync)
        };
        model.Application = _application;
        return model;
    }

    protected override void OnCurrentObjectChanged()
    {
        base.OnCurrentObjectChanged();
        _boundPersonId = null;
        BindPerson(forceReloadChildren: true);
    }

    protected override void ReadValueCore()
    {
        base.ReadValueCore();
        BindPerson(forceReloadChildren: false);
    }

    protected override void RefreshReadOnly()
    {
        base.RefreshReadOnly();
        BindPerson(forceReloadChildren: false);
    }

    protected override object GetControlValueCore() => string.Empty;

    protected override void WriteValueCore() { }

    private void BindPerson(bool forceReloadChildren)
    {
        var model = ComponentModel;
        if (model == null)
            return;

        Person? person;
        IObjectSpace? personSpace;
        var openedFromIncompleteList = false;
        if (CurrentObject is PersonIntakeWizardHost host)
        {
            person = host.Person;
            personSpace = host.PersonObjectSpace;
            openedFromIncompleteList = host.OpenedFromIncompleteList;
        }
        else
        {
            person = CurrentObject as Person;
            personSpace = _objectSpace ?? View?.ObjectSpace;
        }

        model.Person = person;
        model.OwnerViewId = View?.Id;
        model.ObjectSpace = personSpace;
        model.Application = _application;
        model.ShowIncompleteOverview = openedFromIncompleteList && person?.IsDataIncomplete == true;
        if (person == null)
            return;

        var personId = person.ID;
        var reloadChildren = forceReloadChildren || _boundPersonId != personId;
        if (reloadChildren || model.Access == PersonIntakeWizardAccess.Unrestricted)
            model.Access = PersonIntakeWizardAccess.ForCurrentUser(personSpace, person);
        model.State.LoadFromPerson(person, reloadChildren: reloadChildren);
        _boundPersonId = personId;
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
            SecuritySystem.CurrentUserName ?? string.Empty,
            model.Access);

        model.StatusMessage = result.Message;
        model.IsStatusError = !result.Succeeded;
        if (result.Succeeded)
        {
            // A created person now has object-level rules (criteria) to evaluate.
            model.Access = PersonIntakeWizardAccess.ForCurrentUser(objectSpace, person);
            model.State.LoadFromPerson(person, reloadChildren: true, reloadPending: true);
        }

        ReadValueCore();
        return Task.CompletedTask;
    }
}