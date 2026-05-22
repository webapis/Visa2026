using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Localization;

/// <summary>Resolves UI culture for <c>PACKAGING_NOTES.txt</c> from the user who queued the PDF batch.</summary>
public static class PdfPackagingNotesCultureResolver
{
    public static string Resolve(IObjectSpace objectSpace, string? requestedByUserName, string? requestedCulture = null)
    {
        if (!string.IsNullOrWhiteSpace(requestedCulture))
        {
            return VisaUiMessages.NormalizeCultureName(requestedCulture);
        }

        if (string.IsNullOrWhiteSpace(requestedByUserName))
        {
            return VisaUiMessages.DefaultCultureName;
        }

        var user = objectSpace.GetObjectsQuery<ApplicationUser>()
            .AsEnumerable()
            .FirstOrDefault(u => u.UserName != null
                && string.Equals(u.UserName, requestedByUserName, StringComparison.OrdinalIgnoreCase));

        if (user != null && !string.IsNullOrWhiteSpace(user.PreferredCulture))
        {
            return VisaUiMessages.NormalizeCultureName(user.PreferredCulture);
        }

        return VisaUiMessages.DefaultCultureName;
    }
}
