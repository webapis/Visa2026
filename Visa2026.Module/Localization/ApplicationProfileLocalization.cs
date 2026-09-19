using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Localization;

/// <summary>
/// Officer-facing labels for Application Profile templates and instances (Layer A).
/// Keys live in <c>UiStrings.application-profile-messages.json</c>.
/// </summary>
public static class ApplicationProfileLocalization
{
    public static string Msg(string key) => VisaUiMessages.Get(key);

    public static string Format(string key, params object[] args) => VisaUiMessages.Format(key, args);

    public static string Field(string fieldKey) => VisaUiMessages.Get("ApplicationProfile.Field." + fieldKey);

    public static string ActionFamily(ApplicationProfileActionFamily family) => family switch
    {
        ApplicationProfileActionFamily.Cancellation => Msg("ApplicationProfile.ActionFamily.Cancellation"),
        ApplicationProfileActionFamily.Change => Msg("ApplicationProfile.ActionFamily.Change"),
        ApplicationProfileActionFamily.Registration => Msg("ApplicationProfile.ActionFamily.Registration"),
        ApplicationProfileActionFamily.BusinessTrip => Msg("ApplicationProfile.ActionFamily.BusinessTrip"),
        _ => Msg("ApplicationProfile.ActionFamily.Issuance"),
    };

    public static string RegistrationKind(ApplicationProfileRegistrationKind kind) => kind switch
    {
        ApplicationProfileRegistrationKind.CheckIn => Msg("ApplicationProfile.RegistrationKind.CheckIn"),
        ApplicationProfileRegistrationKind.CheckOut => Msg("ApplicationProfile.RegistrationKind.CheckOut"),
        ApplicationProfileRegistrationKind.InfoChange => Msg("ApplicationProfile.RegistrationKind.InfoChange"),
        ApplicationProfileRegistrationKind.Extension => Msg("ApplicationProfile.RegistrationKind.Extension"),
        _ => string.Empty,
    };

    public static string ProgressRoute(ApplicationProfileInstanceProgressRouteKind route) =>
        route == ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService
            ? Msg("ApplicationProfile.Route.DirectMigration")
            : Msg("ApplicationProfile.Route.ViaMinistry");

    /// <summary>Officer progress outcome captions (Submitted / Approved / Issued / …).</summary>
    public static string ProgressOutcome(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return label ?? string.Empty;

        return label.Trim() switch
        {
            "Submitted" => Msg("ApplicationProfileInstance.Workspace.Submitted"),
            "Approved" => Msg("ApplicationProfileInstance.Workspace.Approved"),
            "Issued" => Msg("ApplicationProfileInstance.Workspace.Issued"),
            "Rejected" => Msg("ApplicationProfileInstance.Workspace.Rejected"),
            "Cancelled" => Msg("ApplicationProfileInstance.Workspace.Cancelled"),
            "Disapproved" => Msg("ApplicationProfileInstance.Workspace.Disapproved"),
            "Postponed" => Msg("ApplicationProfileInstance.Workspace.Postponed"),
            "On process" => Msg("ApplicationProfileInstance.Workspace.OnProcess"),
            "Process complete" => Msg("ApplicationProfileInstance.Workspace.ProcessComplete"),
            _ => label,
        };
    }

    /// <summary>
    /// Display-only step / badge / history labels. Does not change
    /// <c>Office preparation</c> logic keys used by CompletenessGate.
    /// </summary>
    public static string ProgressStepLabel(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return label ?? string.Empty;

        var trimmed = label.Trim();
        var sep = trimmed.IndexOf(" · ", StringComparison.Ordinal);
        if (sep > 0)
            return ProgressStepLabel(trimmed[..sep]) + " · " + ProgressStepLabel(trimmed[(sep + 3)..]);

        return trimmed switch
        {
            "Office preparation" => Msg("ApplicationProfileInstance.Workspace.OfficePreparation"),
            "Migration service" => Msg("ApplicationProfileInstance.Workspace.MigrationService"),
            "Completed" => Msg("ApplicationProfileInstance.Workspace.Completed"),
            "Pending" => Msg("ApplicationProfileInstance.Workspace.Pending"),
            "In progress" => Msg("ApplicationProfileInstance.Workspace.InProgress"),
            "Selected" => Msg("ApplicationProfileInstance.Picker.Selected"),
            _ => ProgressOutcome(trimmed),
        };
    }

    public static string ProgressReason(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text ?? string.Empty;

        return text.Trim() switch
        {
            "This application has reached a terminal progress state." =>
                Msg("ApplicationProfileInstance.Workspace.TerminalBlocked"),
            "No further progress steps are available for this route." =>
                Msg("ApplicationProfileInstance.Workspace.NoFurtherSteps"),
            _ => text,
        };
    }

    /// <summary>Display-only Activity rail titles. Stored English strings stay for tests.</summary>
    public static string ActivityTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return title ?? string.Empty;

        var trimmed = title.Trim();
        const string mergedPrefix = "Merged ";
        const string mergedSuffix = " profiles";
        if (trimmed.StartsWith(mergedPrefix, StringComparison.Ordinal)
            && trimmed.EndsWith(mergedSuffix, StringComparison.Ordinal))
        {
            var mid = trimmed[mergedPrefix.Length..^mergedSuffix.Length].Trim();
            if (int.TryParse(mid, out var count))
                return Format("ApplicationProfileInstance.Workspace.MergedProfiles", count);
        }

        const string progressPrefix = "Progress: ";
        if (trimmed.StartsWith(progressPrefix, StringComparison.Ordinal))
            return Format(
                "ApplicationProfileInstance.Workspace.ProgressColon",
                ProgressStepLabel(trimmed[progressPrefix.Length..]));

        return trimmed switch
        {
            "Number assigned" => Msg("ApplicationProfileInstance.Workspace.NumberAssigned"),
            "Latest progress" => Msg("ApplicationProfileInstance.Workspace.LatestProgress"),
            _ => ProgressStepLabel(trimmed),
        };
    }

    public static string ActivitySubtitle(string? subtitle)
    {
        if (string.Equals(subtitle, "Latest progress", StringComparison.Ordinal))
            return Msg("ApplicationProfileInstance.Workspace.LatestProgress");
        return subtitle ?? string.Empty;
    }

    /// <summary>Display-only linked-record tile labels. Catalog <c>Definition.Label</c> stays English.</summary>
    public static string LinkedRecordLabel(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return label ?? string.Empty;

        return label.Trim() switch
        {
            "Passport" => Msg("ApplicationProfile.Person.Passport"),
            "Education" => Msg("ApplicationProfile.Person.Education"),
            "Position" => Msg("ApplicationProfile.Person.Position"),
            "Address" => Msg("ApplicationProfileInstance.Org.Address"),
            "Visa" => Msg("ApplicationProfile.Person.Visa"),
            "Invitation" => Msg("ApplicationProfile.Doc.Invitation"),
            "Work permit" => Msg("ApplicationProfile.Doc.WorkPermit"),
            "Border zone" => Msg("ApplicationProfile.Doc.BorderZone"),
            "Salary" => Msg("ApplicationProfile.Person.Salary"),
            "Medical" => Msg("ApplicationProfile.Person.Medical"),
            "Rejection" => Msg("ApplicationProfile.Doc.Rejection"),
            "Travel history" => Msg("ApplicationProfile.Person.TravelHistory"),
            _ => label,
        };
    }

    /// <summary>Display-only issued-record tile labels. Catalog <c>Definition.Label</c> stays English.</summary>
    public static string IssuedRecordLabel(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return label ?? string.Empty;

        return label.Trim() switch
        {
            "Invitation" => Msg("ApplicationProfile.Doc.Invitation"),
            "Work permit" => Msg("ApplicationProfile.Doc.WorkPermit"),
            "Border zone" => Msg("ApplicationProfile.Doc.BorderZone"),
            "Rejection" => Msg("ApplicationProfile.Doc.Rejection"),
            "Issued visa" => Msg("ApplicationProfileInstance.Issued.Visa"),
            _ => LinkedRecordLabel(label),
        };
    }

    /// <summary>Compact ListView chip caption. Visa uses <c>Wiza</c>, not "Issued visa".</summary>
    public static string IssuedResultChipLabel(string? label)
    {
        if (string.Equals(label?.Trim(), "Issued visa", StringComparison.Ordinal))
            return Msg("ApplicationProfile.Doc.Visa");

        return IssuedRecordLabel(label);
    }

    public static string IssuedAddCaption(string? caption) => caption?.Trim() switch
    {
        "+ Add invitation" => Format("ApplicationProfileInstance.Issued.Add", Msg("ApplicationProfile.Doc.Invitation")),
        "+ Add work permit" => Format("ApplicationProfileInstance.Issued.Add", Msg("ApplicationProfile.Doc.WorkPermit")),
        "+ Add border zone" => Format("ApplicationProfileInstance.Issued.Add", Msg("ApplicationProfile.Doc.BorderZone")),
        "+ Add rejection" => Format("ApplicationProfileInstance.Issued.Add", Msg("ApplicationProfile.Doc.Rejection")),
        "+ Add issued visa" => Format("ApplicationProfileInstance.Issued.Add", Msg("ApplicationProfileInstance.Issued.Visa")),
        _ => IssuedRecordLabel(caption),
    };

    public static string IssuedNewCaption(string? caption) => IssuedComposeTitle(caption);

    public static string IssuedPanelTitle(string? title) => title?.Trim() switch
    {
        "Invitations produced by this case" =>
            Format("ApplicationProfileInstance.Issued.Panel", Msg("ApplicationProfile.Doc.Invitations")),
        "Work permits produced by this case" =>
            Format("ApplicationProfileInstance.Issued.Panel", Msg("ApplicationProfile.Doc.WorkPermits")),
        "Border-zone permits produced by this case" =>
            Format("ApplicationProfileInstance.Issued.Panel", Msg("ApplicationProfile.Doc.BorderZonePermits")),
        "Rejections produced by this case" =>
            Format("ApplicationProfileInstance.Issued.Panel", Msg("ApplicationProfile.Doc.Rejections")),
        "Visas issued by this case" =>
            Format("ApplicationProfileInstance.Issued.Panel", Msg("ApplicationProfile.Doc.Visas")),
        _ => title ?? string.Empty,
    };

    public static string IssuedEmptyHint(string? englishLabel, string? applicationNumber = null)
    {
        var kind = IssuedRecordLabel(englishLabel);
        return string.IsNullOrWhiteSpace(applicationNumber)
            ? Format("ApplicationProfileInstance.Issued.Empty", kind)
            : Format("ApplicationProfileInstance.Issued.EmptyNumber", kind, applicationNumber);
    }

    /// <summary>Display-only compose drawer titles. Service <c>Title</c> stays English.</summary>
    public static string IssuedComposeTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return title ?? string.Empty;

        var trimmed = title.Trim();
        const string visaPrefix = "Visa ";
        if (trimmed.StartsWith(visaPrefix, StringComparison.Ordinal)
            && !string.Equals(trimmed, "Visa issued", StringComparison.Ordinal))
        {
            return Format("ApplicationProfileInstance.Issued.VisaNumberValue", trimmed[visaPrefix.Length..]);
        }

        return trimmed switch
        {
            "New invitation" => Format("ApplicationProfileInstance.Issued.New", Msg("ApplicationProfile.Doc.Invitation")),
            "New work permit" => Format("ApplicationProfileInstance.Issued.New", Msg("ApplicationProfile.Doc.WorkPermit")),
            "New rejection" => Format("ApplicationProfileInstance.Issued.New", Msg("ApplicationProfile.Doc.Rejection")),
            "New border zone" => Format("ApplicationProfileInstance.Issued.New", Msg("ApplicationProfile.Doc.BorderZone")),
            "New issued record" => Format("ApplicationProfileInstance.Issued.New", Msg("ApplicationProfileInstance.Issued.Record")),
            "New issued visa" => Format("ApplicationProfileInstance.Issued.New", Msg("ApplicationProfileInstance.Issued.Visa")),
            "Edit invitation" => Format("ApplicationProfileInstance.Issued.TitleEdit", Msg("ApplicationProfile.Doc.Invitation")),
            "Edit work permit" => Format("ApplicationProfileInstance.Issued.TitleEdit", Msg("ApplicationProfile.Doc.WorkPermit")),
            "Edit rejection" => Format("ApplicationProfileInstance.Issued.TitleEdit", Msg("ApplicationProfile.Doc.Rejection")),
            "Edit border zone" => Format("ApplicationProfileInstance.Issued.TitleEdit", Msg("ApplicationProfile.Doc.BorderZone")),
            "Issued visa" => Msg("ApplicationProfileInstance.Issued.Visa"),
            "Issued visas" => Msg("ApplicationProfile.Doc.Visas"),
            "Issued record" => Msg("ApplicationProfileInstance.Issued.Record"),
            _ => trimmed,
        };
    }

    public static string IssuedStatus(string? caption) => caption?.Trim() switch
    {
        "Ready" => Msg("ApplicationProfileInstance.Issued.StatusReady"),
        "Missing passport" => Msg("ApplicationProfileInstance.Issued.StatusMissingPassport"),
        "No passport" => Msg("ApplicationProfileInstance.Issued.StatusNoPassport"),
        "No position" => Msg("ApplicationProfileInstance.Issued.StatusNoPosition"),
        "On this work permit" => Msg("ApplicationProfileInstance.Issued.StatusOnWorkPermit"),
        "On this rejection" => Msg("ApplicationProfileInstance.Issued.StatusOnRejection"),
        "On this invitation" => Msg("ApplicationProfileInstance.Issued.StatusOnInvitation"),
        "On this invitation (visa issued/closed)" => Msg("ApplicationProfileInstance.Issued.StatusOnInvitationClosed"),
        "On this border zone" => Msg("ApplicationProfileInstance.Issued.StatusOnBorderZone"),
        "Visa issued" => Msg("ApplicationProfileInstance.Issued.StatusVisaIssued"),
        "Issued" => Msg("ApplicationProfileInstance.Workspace.Issued"),
        _ => caption ?? string.Empty,
    };

    public static string RelatedTo(ApplicationProfile profile) =>
        RelatedTo(profile.ActionFamily, profile.RegistrationKind);

    public static string RelatedTo(
        ApplicationProfileActionFamily family,
        ApplicationProfileRegistrationKind kind)
    {
        var familyLabel = ActionFamily(family);
        if (family != ApplicationProfileActionFamily.Registration)
            return familyLabel;

        var kindLabel = RegistrationKind(kind);
        return string.IsNullOrEmpty(kindLabel) ? familyLabel : familyLabel + " · " + kindLabel;
    }
}
