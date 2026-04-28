using System;
using System.Collections.Generic;

namespace Visa2026.Module.BusinessObjects
{
    /// <summary>
    /// Application types (<see cref="ApplicationType.Name"/>) whose workflows may produce a new <see cref="Visa"/>.
    /// Single source: <see cref="Names"/> drives validation (<see cref="AllowedApplicationTypeNames"/>)
    /// and the issuing-application lookup on <see cref="Visa.AvailableIssuingApplicationItems"/>.
    /// </summary>
    public static class VisaIssuingApplicationTypes
    {
        /// <summary>Canonical allowlist — edit only here.</summary>
        private static readonly string[] Names =
        {
            "App_Inv",
            "App_Inv_FM",
            "App_Inv_According_to_WP",
            "App_Change_Inv",
            "App_Inv_And_WP",
            "App_Visa_Ext_According_to_WP",
            "App_Visa_Ext",
            "App_Exit_Visa",
            "App_Change_Visa_Category",
            "App_Change_Passport",
            "App_Visa_Ext_FM",
            "App_Visa_For_New_Born_FM",
            "App_Visa_and_WP_Ext",
        };

        public static readonly HashSet<string> AllowedApplicationTypeNames;

        static VisaIssuingApplicationTypes()
        {
            AllowedApplicationTypeNames = new HashSet<string>(Names, StringComparer.Ordinal);
        }

        public static bool IsAllowed(ApplicationType applicationType)
        {
            if (applicationType == null) return false;
            var n = applicationType.Name?.Trim();
            return !string.IsNullOrEmpty(n) && AllowedApplicationTypeNames.Contains(n);
        }
    }
}
