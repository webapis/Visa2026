using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Supplies the state code used for ListView row background (<see cref="BoStateAppearanceColors"/>).
/// </summary>
public interface IBoListRowState
{
    [Browsable(false)]
    [NotMapped]
    string PrimaryStateCode { get; }
}
