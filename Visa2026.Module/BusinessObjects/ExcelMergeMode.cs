namespace Visa2026.Module.BusinessObjects;

/// <summary>How an Excel user report merges data (see docs/EXCEL_TEMPLATE_REPORTING_PLAN.md).</summary>
public enum ExcelMergeMode
{
    /// <summary>One workbook per <see cref="ApplicationRosterMergeLine"/> (v1.1).</summary>
    SingleItem = 0,

    /// <summary>Header from <see cref="Application"/>; table rows from <see cref="ApplicationRosterMergeLine"/> list.</summary>
    ItemList = 1,
}
