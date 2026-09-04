using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Resolves the latest <see cref="ApplicationProfileInstanceProgress"/> row from history (order, then date/ID).
/// </summary>
public static class ApplicationProfileInstanceProgressHelper
{
    public static ApplicationProfileInstanceProgress? GetLatest(IEnumerable<ApplicationProfileInstanceProgress>? history, IObjectSpace? objectSpace = null)
    {
        if (history == null)
            return null;

        IEnumerable<ApplicationProfileInstanceProgress> query = history;
        if (objectSpace != null)
            query = query.Where(p => !objectSpace.IsObjectToDelete(p));

        ApplicationProfileInstanceProgress? latest = null;
        foreach (var progress in query)
        {
            if (latest == null || ApplicationProfileInstanceProgressOrderHelper.CompareSiblingOrder(progress, latest) > 0)
                latest = progress;
        }

        return latest;
    }

    /// <summary>
    /// DevExpress criteria comparing the latest progress state's code on a collection path.
    /// </summary>
    public static string BuildLatestStateCodeCriteria(string progressHistoryPath, string stateCode, bool equals = true)
    {
        var op = equals ? "=" : "<>";
        return $"{progressHistoryPath}[Date = ^.{progressHistoryPath}.Max(Date)].Single(State.Code) {op} '{stateCode}'";
    }
}
