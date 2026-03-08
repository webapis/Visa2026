using System;
using System.Collections.Generic;

namespace Visa2026.Module.Services
{
    public static class PdfFormConstants
    {
        // 3.TIZLIGI — L02[0]
        // Display : 'ADATY '  | 'TIZ'  | 'ORAN TIZ' | 'XX'
        // Raw     :  '1'      |  '2'   |  '3'       | (other)
        public static readonly Dictionary<string, string> UrgencyRawValues = new(StringComparer.OrdinalIgnoreCase)
        {
            { "ADATY",    "1" },
            { "ADATY ",   "1" },   // trailing space variant in PDF
            { "TIZ",      "2" },
            { "ORAN TIZ", "3" },
            { "XX",       "XX" },
            // Pass-through: if the value is already a raw code just use it
            { "1", "1" }, { "2", "2" }, { "3", "3" },
        };

        // 13.GYNSY — _05[0]
        // Display = Raw: 'M' | 'F' | 'X'  (display and raw happen to be identical)
        public static readonly HashSet<string> GenderRawValues = new(StringComparer.OrdinalIgnoreCase)
        {
            "M", "F", "X"
        };

        // 25.MASGALA YAGDAY — _18[0]
        // Display : 'Sallah/Durmuşa çykmadyk' | 'Öýlenen/Durmuşa çykan' | 'Aýrylyşan' | 'Dul'
        // Raw     :  '1'                       |  '2'                    |  '3'        | '4'
        public static readonly Dictionary<string, string> MaritalStatusRawValues = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Sallah/Durmuşa çykmadyk", "1" },
            { "Sallah",                  "1" },
            { "Öýlenen/Durmuşa çykan",   "2" },
            { "Öýlenen",                 "2" },
            { "Durmuşa çykan",           "2" },
            { "Aýrylyşan",               "3" },
            { "Dul",                     "4" },
            // Pass-through: already raw
            { "1", "1" }, { "2", "2" }, { "3", "3" }, { "4", "4" },
        };

        // 18. RESMINAMASY GORUJI (Document type) — _10[0]
        // Valid raw: 'P','APD','AGL','AML','AUN','YG','BS','PD','SP','UN','US','YD','SH','DZ','PG','LBG','PT','EU'
        public static readonly Dictionary<string, string> PassportTypeRawValues = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Ordinary Passport",   "P" },
            { "Passport",            "P" },
            { "Diplomatic Passport", "PD" },
            { "Diplomatic",          "PD" },
            { "Service Passport",    "BS" },
            { "Service",             "BS" },
            { "Official Passport",   "BS" },
            { "Stateless Person",    "LBG" }
        };
    }
}