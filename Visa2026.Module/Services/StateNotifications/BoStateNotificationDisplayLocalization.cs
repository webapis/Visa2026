using System;
using Visa2026.Module.BusinessObjects.StateNotifications;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Services.StateNotifications;

/// <summary>Resolves inbox row display text from <see cref="BoStateNotificationItem.SampleKey"/> (prototype and future evaluators).</summary>
public static class BoStateNotificationDisplayLocalization
{
    public static string BoType(BoStateNotificationItem item) =>
        Resolve($"StateNotification.BoType.{item.BoType}", item.BoType);

    public static string StateLabel(BoStateNotificationItem item) =>
        string.IsNullOrEmpty(item.SampleKey)
            ? Resolve($"StateNotification.State.{item.StateCode}", item.StateLabel)
            : Resolve($"StateNotification.Sample.{item.SampleKey}.StateLabel", item.StateLabel);

    public static string Message(BoStateNotificationItem item) =>
        string.IsNullOrEmpty(item.SampleKey)
            ? item.Message
            : Resolve($"StateNotification.Sample.{item.SampleKey}.Message", item.Message);

    public static string DisplayKey(BoStateNotificationItem item) =>
        string.IsNullOrEmpty(item.SampleKey)
            ? item.DisplayKey
            : Resolve($"StateNotification.Sample.{item.SampleKey}.DisplayKey", item.DisplayKey);

    public static string MissingItemLabel(BoStateNotificationItem item)
    {
        if (string.IsNullOrWhiteSpace(item.MissingItemLabel))
            return StateLabel(item);

        return string.IsNullOrEmpty(item.SampleKey)
            ? Resolve($"StateNotification.MissingItem.{NormalizeKey(item.MissingItemLabel)}", item.MissingItemLabel)
            : Resolve($"StateNotification.Sample.{item.SampleKey}.MissingItem", item.MissingItemLabel);
    }

    public static string HandledBy(BoStateNotificationItem item)
    {
        if (string.IsNullOrEmpty(item.HandledBy))
            return string.Empty;

        return item.HandledBy switch
        {
            "State sync" => VisaUiMessages.Get("StateNotification.HandledBy.StateSync"),
            "you" => VisaUiMessages.Get("StateNotification.HandledBy.You"),
            "demo.officer" => VisaUiMessages.Get("StateNotification.HandledBy.DemoOfficer"),
            _ => item.HandledBy,
        };
    }

    private static string Resolve(string key, string fallback)
    {
        string value = VisaUiMessages.Get(key);
        return string.Equals(value, key, StringComparison.Ordinal) ? fallback : value;
    }

    private static string NormalizeKey(string label) =>
        label.Replace(" ", "", StringComparison.Ordinal);
}
