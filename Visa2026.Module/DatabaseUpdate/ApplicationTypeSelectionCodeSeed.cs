using System;
using System.Collections.Generic;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ministry 3-digit <c>SelectionCode</c> → <see cref="BusinessObjects.ApplicationType.Name"/> seed map.
/// Source: ministry reference table (Application type codes) and the ApplicationType configuration seed.
/// Does not overwrite existing <c>SelectionCode</c> values — only fills empty fields on update.
/// </summary>
internal static class ApplicationTypeSelectionCodeSeed
{
    /// <summary>Keys are <see cref="BusinessObjects.ApplicationType.Name"/> (case-insensitive).</summary>
    private static readonly Dictionary<string, string> ByName =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Çakylyk (100)
            { "App_Inv", "101" },
            { "App_Inv_FM", "102" },
            { "App_Inv_According_to_WP", "103" },
            { "App_Change_Inv", "104" },
            { "App_Inv_And_WP", "105" },

            // Gulluk Pasport (200)
            { "App_Sevice_Passport", "201" }, // spelling matches LOOKUPS / DB Name

            // Hasaba Alyş (300)
            { "App_Reg_Check_In", "301" },
            { "App_Reg_Check_In_Internal", "302" },
            { "App_Reg_Info_Change_Passport", "303" },
            { "App_Reg_Info_Change_Visa", "304" },
            { "App_Reg_Info_Change_Address", "305" },
            { "App_Reg_ext", "306" },
            { "App_Reg_Check_Out", "307" },
            { "App_Reg_Check_Out_Internal", "308" },

            // Iş Rugsatnama (400)
            { "App_WP_Ext", "401" },
            { "App_Additional_WP_location", "402" },

            // Iş Sapary (500)
            { "App_Business_Trip_Departure", "501" },
            { "App_Business_Trip_Arrival", "502" },

            // Serhet ýaka (600)
            { "App_Border_Zone_Permission", "601" },

            // Wiza (700)
            { "App_Visa_Ext_According_to_WP", "701" },
            { "App_Visa_Ext", "702" },
            { "App_Exit_Visa", "703" },
            { "App_Change_Visa_Category", "704" },
            { "App_Change_Passport", "705" },
            { "App_Visa_Ext_FM", "706" },
            { "App_Visa_For_New_Born_FM", "707" },
            { "App_Visa_and_WP_Ext", "708" },

            // Ýatyrmak (800)
            { "App_Cancel_BZ", "801" },
            { "App_Cancel_App", "802" },
            { "App_Cancel_Visa_and_WP_Ext", "803" },
            { "App_Cancel_Visa_Ext", "804" },
            { "App_Cancel_Inv", "805" },
            { "App_Cancel_Inv_WP", "806" },
            { "App_Cancel_Visa", "807" },
            { "App_Cancel_Visa_and_WP", "808" },
            { "App_Cancell_WP", "809" }, // spelling matches LOOKUPS / DB Name
        };

    public static IReadOnlyDictionary<string, string> All => ByName;

    public static bool TryGetByName(string? name, out string code)
    {
        code = string.Empty;
        if (string.IsNullOrWhiteSpace(name))
            return false;
        return ByName.TryGetValue(name.Trim(), out code!);
    }
}
