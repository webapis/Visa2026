using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp.Actions;

namespace Visa2026.Blazor.Server.Controllers;

internal static class NavigationE2eSelectorSupport
{
    internal static string? FindTestIdForDataItem(
        object dataItem,
        IList<ChoiceActionItem> actionItems,
        IList<object> rootMenuItems,
        IReadOnlyDictionary<string, string> testIdsByNavigationItemId)
    {
        var map = new Dictionary<object, string>(ReferenceEqualityComparer.Instance);
        PopulateTestIdMap(actionItems, rootMenuItems, map, testIdsByNavigationItemId);
        if (map.TryGetValue(dataItem, out string? testId))
        {
            return testId;
        }

        string? navigateUrl = GetNavigateUrl(dataItem);
        if (string.IsNullOrEmpty(navigateUrl))
        {
            return null;
        }

        return NavigationE2eHooks.TestIdsByNavigateUrl.GetValueOrDefault(NormalizeNavigateUrl(navigateUrl));
    }

    internal static void PopulateTestIdMap(
        IList<ChoiceActionItem> actionItems,
        IList<object> menuItems,
        IDictionary<object, string> testIdByMenuItem,
        IReadOnlyDictionary<string, string> testIdsByNavigationItemId)
    {
        int count = Math.Min(actionItems.Count, menuItems.Count);
        for (int i = 0; i < count; i++)
        {
            ChoiceActionItem actionItem = actionItems[i];
            object menuItem = menuItems[i];

            if (testIdsByNavigationItemId.TryGetValue(actionItem.Id, out string? testId))
            {
                testIdByMenuItem[menuItem] = testId;
            }

            PopulateTestIdMap(
                actionItem.Items?.ToList() ?? [],
                GetChildMenuItems(menuItem),
                testIdByMenuItem,
                testIdsByNavigationItemId);
        }
    }

    internal static IList<object> GetRootMenuItems(object? accordionData)
    {
        if (accordionData is not IEnumerable enumerable)
        {
            return [];
        }

        return enumerable.Cast<object>().ToList();
    }

    private static string NormalizeNavigateUrl(string navigateUrl)
    {
        return navigateUrl.Trim().TrimStart('/');
    }

    private static string? GetNavigateUrl(object menuItem)
    {
        PropertyInfo? navigateUrlProperty = menuItem.GetType().GetProperty(
            "NavigateUrl",
            BindingFlags.Instance | BindingFlags.Public);
        return navigateUrlProperty?.GetValue(menuItem) as string;
    }

    private static IList<object> GetChildMenuItems(object menuItem)
    {
        PropertyInfo? itemsProperty = menuItem.GetType().GetProperty("Items", BindingFlags.Instance | BindingFlags.Public);
        if (itemsProperty?.GetValue(menuItem) is not IEnumerable items)
        {
            return [];
        }

        return items.Cast<object>().ToList();
    }
}
