using System;
using System.Collections.Generic;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.Services;

namespace Visa2026.Blazor.Server.Editors;

public class VisaFamilyMembersTextModel : ComponentModelBase
{
    public string DisplayText
    {
        get => GetPropertyValue<string>() ?? string.Empty;
        set => SetPropertyValue(value);
    }

    public IReadOnlyList<VisaFamilyMemberLineDto> Lines
    {
        get => GetPropertyValue<IReadOnlyList<VisaFamilyMemberLineDto>>() ?? Array.Empty<VisaFamilyMemberLineDto>();
        set => SetPropertyValue(value);
    }

    public IReadOnlyList<VisaFamilyMemberLineDto> DraftLines
    {
        get => GetPropertyValue<IReadOnlyList<VisaFamilyMemberLineDto>>() ?? Array.Empty<VisaFamilyMemberLineDto>();
        set => SetPropertyValue(value);
    }

    public IReadOnlyList<RelationshipLookupItem> RelationshipOptions
    {
        get => GetPropertyValue<IReadOnlyList<RelationshipLookupItem>>() ?? Array.Empty<RelationshipLookupItem>();
        set => SetPropertyValue(value);
    }

    public IReadOnlyList<CountryLookupItem> CountryOptions
    {
        get => GetPropertyValue<IReadOnlyList<CountryLookupItem>>() ?? Array.Empty<CountryLookupItem>();
        set => SetPropertyValue(value);
    }

    public EventCallback<IReadOnlyList<VisaFamilyMemberLineDto>> LinesCommitted
    {
        get => GetPropertyValue<EventCallback<IReadOnlyList<VisaFamilyMemberLineDto>>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<IReadOnlyList<VisaFamilyMemberLineDto>> DraftLinesChanged
    {
        get => GetPropertyValue<EventCallback<IReadOnlyList<VisaFamilyMemberLineDto>>>();
        set => SetPropertyValue(value);
    }

    public bool PopupVisible
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public EventCallback<bool> PopupVisibleChanged
    {
        get => GetPropertyValue<EventCallback<bool>>();
        set => SetPropertyValue(value);
    }

    public bool ReadOnly
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public string StatusMessage
    {
        get => GetPropertyValue<string>() ?? string.Empty;
        set => SetPropertyValue(value);
    }

    public bool StatusIsError
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public string PopupTitle { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string PopupButtonTitle { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string OkText { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string CancelText { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string AddButtonText { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string EditButtonText { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string DeleteButtonText { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string EditPopupTitle { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string SaveEditText { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string DeleteConfirmTitle { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string DeleteConfirmFormat { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string MemberCountFormat { get => GetPropertyValue<string>() ?? "Members: {0}"; set => SetPropertyValue(value); }
    public string EmptyListMessage { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string FullNameLabel { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string BirthDateLabel { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string RelationshipLabel { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string CountryLabel { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string EditFormIncompleteHint { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }

    public IObjectSpace ObjectSpace
    {
        get => GetPropertyValue<IObjectSpace>();
        set => SetPropertyValue(value);
    }

    public override Type ComponentType => typeof(VisaFamilyMembersTextComponent);
}
