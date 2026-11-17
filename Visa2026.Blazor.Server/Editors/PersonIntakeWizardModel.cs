#nullable enable
using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.PersonIntake;

namespace Visa2026.Blazor.Server.Editors;

public sealed class PersonIntakeWizardModel : ComponentModelBase
{
    public override Type ComponentType => typeof(PersonIntakeWizardComponent);

    public Person? Person
    {
        get => GetPropertyValue<Person>();
        set => SetPropertyValue(value);
    }

    public IObjectSpace? ObjectSpace
    {
        get => GetPropertyValue<IObjectSpace>();
        set => SetPropertyValue(value);
    }

    public PersonIntakeWizardState State
    {
        get => GetPropertyValue<PersonIntakeWizardState>() ?? new PersonIntakeWizardState();
        set => SetPropertyValue(value);
    }

    public string? StatusMessage
    {
        get => GetPropertyValue<string>();
        set => SetPropertyValue(value);
    }

    public bool IsStatusError
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public EventCallback SaveRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback RefreshRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }
}