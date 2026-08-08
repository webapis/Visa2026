#nullable enable
using System;
using System.Collections.Generic;
using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;

namespace Visa2026.Blazor.Server.Editors;

public sealed class ApplicationProfilePickerModel : ComponentModelBase
{
    public override Type ComponentType => typeof(ApplicationProfilePickerComponent);

    public bool IsLoading
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public bool IsPersonStartFlow
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public int Step
    {
        get => GetPropertyValue<int>();
        set => SetPropertyValue(value);
    }

    public string? SeedPersonLabel
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }

    public IReadOnlyList<PickerRowModel> Rows
    {
        get => GetPropertyValue<IReadOnlyList<PickerRowModel>>() ?? Array.Empty<PickerRowModel>();
        set => SetPropertyValue(value);
    }

    public IReadOnlyList<PeopleRowModel> PeopleRows
    {
        get => GetPropertyValue<IReadOnlyList<PeopleRowModel>>() ?? Array.Empty<PeopleRowModel>();
        set => SetPropertyValue(value);
    }

    public HashSet<Guid> SelectedPersonIds
    {
        get => GetPropertyValue<HashSet<Guid>>() ?? new HashSet<Guid>();
        set => SetPropertyValue(value);
    }

    public Guid SelectedProfileId
    {
        get => GetPropertyValue<Guid>();
        set => SetPropertyValue(value);
    }

    public string? StatusMessage
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }

    public bool IsStatusError
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public bool IsStatusWarning
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public bool DuplicateWarningAcknowledged
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public bool HasDuplicateWarning
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public bool CanCreateFromPeople
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public string? RouteHint
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }

    public EventCallback InitialLoadRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback UseProfileRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback NextStepRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback BackStepRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback<Guid> SelectProfileRequested
    {
        get => GetPropertyValue<EventCallback<Guid>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<bool> DuplicateWarningAcknowledgedChanged
    {
        get => GetPropertyValue<EventCallback<bool>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<Guid> TogglePersonRequested
    {
        get => GetPropertyValue<EventCallback<Guid>>();
        set => SetPropertyValue(value);
    }

    public sealed class PickerRowModel
    {
        public Guid ProfileId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string MetaLine { get; init; } = string.Empty;

        public string SeedUsageLine { get; init; } = string.Empty;

        public bool IsConfigLocked { get; init; }

        public bool HasOpenApplicationForSeedPerson { get; init; }
    }

    public sealed record PeopleRowModel
    {
        public Guid PersonId { get; init; }

        public string FullName { get; init; } = string.Empty;

        public string RoleLabel { get; init; } = string.Empty;

        public string PersonalNumber { get; init; } = string.Empty;

        public bool IsSeedPerson { get; init; }

        public bool IsSuggestedFamily { get; init; }

        public bool IsSelected { get; init; }
    }
}
