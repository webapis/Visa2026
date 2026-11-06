using System;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Extension-application cancellations replaced by Seretmezlik.
/// Rows stay in the database so an already saved case still opens.
/// They are inactive and omitted from pickers.
/// </summary>
public static class ApplicationProfileRetiredCatalog
{
    public const string CancelVisaExtension = "cancel_visa_ext";
    public const string CancelVisaAndWorkPermitExtension = "cancel_visa_wp_ext";

    public static bool IsRetiredProfileCode(string? code) =>
        string.Equals(code, CancelVisaExtension, StringComparison.OrdinalIgnoreCase)
        || string.Equals(code, CancelVisaAndWorkPermitExtension, StringComparison.OrdinalIgnoreCase);
}
