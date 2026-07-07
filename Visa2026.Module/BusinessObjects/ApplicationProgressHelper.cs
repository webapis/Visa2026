using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Resolves the latest <see cref="ApplicationProgress"/> row from history (order, then date/ID).
/// </summary>
public static class ApplicationProgressHelper
{
    public static ApplicationProgress? GetLatest(IEnumerable<ApplicationProgress>? history, IObjectSpace? objectSpace = null)
    {
        if (history == null)
            return null;

        IEnumerable<ApplicationProgress> query = history;
        if (objectSpace != null)
            query = query.Where(p => !objectSpace.IsObjectToDelete(p));

        ApplicationProgress? latest = null;
        foreach (var progress in query)
        {
            if (latest == null || ApplicationProgressOrderHelper.CompareSiblingOrder(progress, latest) > 0)
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
