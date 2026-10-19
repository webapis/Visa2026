using System.ComponentModel;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Blazor.Server;

/// <summary>
/// Per-user flag on the person roster lists. When an officer drags a column,
/// the widths are written to <see cref="IModelColumn.Width"/> and this flag,
/// then saved with the user model differences.
/// </summary>
public interface IModelPersonRosterLayout
{
    [DefaultValue(false)]
    bool ColumnWidthsCustomized { get; set; }
}
