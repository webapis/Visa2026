using System;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Education is not required on visa-cancellation templates
/// (<c>cancel_visa</c> / Wizany Yatyrmak, <c>cancel_visa_wp</c>).
/// Extension-request cancels (<c>cancel_visa_ext</c>) keep the catalog flag.
/// </summary>
public static class ApplicationProfileEducationPolicy
{
    public static bool AllowsPersonEducation(ApplicationProfile? profile)
    {
        if (profile == null)
            return false;
        if (IsVisaDocumentCancellation(profile))
            return false;
        return true;
    }

    public static bool IsVisaDocumentCancellation(ApplicationProfile profile) =>
        IsVisaDocumentCancellation(profile.CancelVisas, profile.Code);

    public static bool IsVisaDocumentCancellation(bool cancelVisas, string? code) =>
        cancelVisas || CodeLooksLikeVisaDocumentCancellation(code);

    public static bool CodeLooksLikeVisaDocumentCancellation(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        return string.Equals(code, "cancel_visa", StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, "cancel_visa_wp", StringComparison.OrdinalIgnoreCase);
    }
}