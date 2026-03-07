using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services
{
    internal static class PdfMappingHelper
    {
        // -----------------------------------------------------------------------
        // choiceList raw-value lookup tables (confirmed from PDF XFA template).
        // XFA choiceList fields store a short code internally; the display label
        // is separate. Spire's XfaChoiceListField.SelectedItem must receive the
        // RAW code, not the display label, otherwise the field stays blank.
        // -----------------------------------------------------------------------

        // 3.TIZLIGI — L02[0]
        // Display : 'ADATY '  | 'TIZ'  | 'ORAN TIZ' | 'XX'
        // Raw     :  '1'      |  '2'   |  '3'       | (other)
        private static readonly Dictionary<string, string> UrgencyRawValues = new(StringComparer.OrdinalIgnoreCase)
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
        private static readonly HashSet<string> GenderRawValues = new(StringComparer.OrdinalIgnoreCase)
        {
            "M", "F", "X"
        };

        // 25.MASGALA YAGDAY — _18[0]
        // Display : 'Sallah/Durmuşa çykmadyk' | 'Öýlenen/Durmuşa çykan' | 'Aýrylyşan' | 'Dul'
        // Raw     :  '1'                       |  '2'                    |  '3'        | '4'
        private static readonly Dictionary<string, string> MaritalStatusRawValues = new(StringComparer.OrdinalIgnoreCase)
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

        // -----------------------------------------------------------------------

        /// <summary>
        /// Resolves a display label to the raw XFA choiceList code using the
        /// provided lookup table. Logs a warning when the value is unrecognised
        /// so the calling code can catch mismatches early.
        /// </summary>
        private static string ResolveRawValue(
            Dictionary<string, string> lookup,
            string displayValue,
            string fieldLabel,
            string fieldKey,
            ILogger logger)
        {
            if (displayValue == null) return null;
            if (lookup.TryGetValue(displayValue, out string raw))
            {
                if (raw != displayValue)
                    logger?.LogDebug(
                        "PDF mapping: [{FieldLabel}] key='{FieldKey}' — resolved display '{Display}' → raw '{Raw}'.",
                        fieldLabel, fieldKey, displayValue, raw);
                return raw;
            }
            logger?.LogWarning(
                "PDF mapping: [{FieldLabel}] key='{FieldKey}' — display value '{Display}' has no known raw mapping. " +
                "Passing value as-is; the field may render blank in the PDF.",
                fieldLabel, fieldKey, displayValue);
            return displayValue; // best-effort pass-through
        }

        // This method is extracted from ApplicationItemPdfController to be reused.
        public static void MapApplicationData(
            Dictionary<string, object> data,
            Application application,
            ApplicationItem item,
            ILogger logger = null)
        {
            void Log(string fieldKey, string fieldLabel, object value)
            {
                if (logger == null) return;
                if (value == null)
                    logger.LogWarning("PDF mapping: [{FieldLabel}] key='{FieldKey}' → NULL (field will be skipped).", fieldLabel, fieldKey);
                else
                    logger.LogDebug("PDF mapping: [{FieldLabel}] key='{FieldKey}' → '{Value}' ({ValueType}).",
                        fieldLabel, fieldKey, value, value.GetType().Name);
            }

            // --- 1. Application Level Data ---

            // Urgency (3.TIZLIGI) — choiceList, raw values: '1'/'2'/'3'
            if (application.Urgency != null)
            {
                const string key = "topmostSubform[0].Page1[0].L02[0]";
                string raw = ResolveRawValue(UrgencyRawValues, application.Urgency.Name,
                    "3.TIZLIGI (Urgency)", key, logger);
                data[key] = raw;
                Log(key, "3.TIZLIGI (Urgency)", raw);
            }
            else
            {
                logger?.LogWarning("PDF mapping: [3.TIZLIGI (Urgency)] — application.Urgency is null, field skipped.");
            }

            // Company / Inviting Party Info
            if (application.Company != null)
            {
                // 4.CAGYRYAN TARAP YURIDIKI SAHS (Legal Entity Checkbox)
                const string checkboxKey = "topmostSubform[0].Page1[0].IP[1].#field[0]";
                data[checkboxKey] = true;
                Log(checkboxKey, "4. Legal Entity Checkbox", true);

                // 5.KARHANANYN ADY (Company Name)
                const string nameKey = "topmostSubform[0].Page1[0].L10[0]";
                data[nameKey] = application.Company.Name;
                Log(nameKey, "5.KARHANANYN ADY (Company Name)", application.Company.Name);

                // 6.HUKUK SALGYSY (Address)
                const string addrKey = "topmostSubform[0].Page1[0].L11[0]";
                data[addrKey] = application.Company.Address;
                Log(addrKey, "6.HUKUK SALGYSY (Company Address)", application.Company.Address);

                // 8.TELEFON (Phone)
                const string phoneKey = "topmostSubform[0].Page1[0].L13[0]";
                data[phoneKey] = application.Company.PhoneNumber;
                Log(phoneKey, "8.TELEFON (Company Phone)", application.Company.PhoneNumber);
            }
            else
            {
                logger?.LogWarning("PDF mapping: [Company] — application.Company is null, all company fields skipped.");
            }

            // --- 2. Person Level Data ---
            var person = item.Person;
            if (person != null)
            {
                // 9.FAMILIYASY (Last Name)
                const string lastNameKey = "topmostSubform[0].Page1[0]._01[0]";
                data[lastNameKey] = person.LastName;
                Log(lastNameKey, "9.FAMILIYASY (Last Name)", person.LastName);

                // 11.ADY (First Name)
                const string firstNameKey = "topmostSubform[0].Page1[0]._03[0]";
                data[firstNameKey] = person.FirstName;
                Log(firstNameKey, "11.ADY (First Name)", person.FirstName);

                // 12.DOGLAN SENESI (Date of Birth)
                const string dobKey = "topmostSubform[0].Page1[0]._04[0]";
                if (person.DateOfBirth != DateTime.MinValue)
                {
                    data[dobKey] = person.DateOfBirth;
                    Log(dobKey, "12.DOGLAN SENESI (Date of Birth)", person.DateOfBirth);
                }
                else
                {
                    logger?.LogWarning("PDF mapping: [12.DOGLAN SENESI (Date of Birth)] key='{Key}' → DateTime.MinValue, field skipped.", dobKey);
                }

                // 13.GYNSY (Gender) — choiceList, raw values: 'M'/'F'/'X'
                const string genderKey = "topmostSubform[0].Page1[0]._05[0]";
                if (person.Gender != null)
                {
                    string genderRaw = person.Gender.Name;
                    if (!GenderRawValues.Contains(genderRaw))
                    {
                        logger?.LogWarning(
                            "PDF mapping: [13.GYNSY (Gender)] key='{Key}' — value '{Value}' is not a valid raw code " +
                            "(expected 'M', 'F', or 'X'). Field may render blank.",
                            genderKey, genderRaw);
                    }
                    data[genderKey] = genderRaw;
                    Log(genderKey, "13.GYNSY (Gender)", genderRaw);
                }
                else
                {
                    logger?.LogWarning("PDF mapping: [13.GYNSY (Gender)] key='{Key}' → person.Gender is null, field skipped.", genderKey);
                }

                // 16.DOGLAN YERI (Birth Place)
                const string birthPlaceKey = "topmostSubform[0].Page1[0]._08[0]";
                data[birthPlaceKey] = person.BirthPlace;
                Log(birthPlaceKey, "16.DOGLAN YERI (Birth Place)", person.BirthPlace);

                // 25.MASGALA YAGDAY (Marital Status) — choiceList, raw values: '1'/'2'/'3'/'4'
                const string maritalKey = "topmostSubform[0].Page1[0]._18[0]";
                if (person.MaritalStatus != null)
                {
                    string raw = ResolveRawValue(MaritalStatusRawValues, person.MaritalStatus.Name,
                        "25.MASGALA YAGDAY (Marital Status)", maritalKey, logger);
                    data[maritalKey] = raw;
                    Log(maritalKey, "25.MASGALA YAGDAY (Marital Status)", raw);
                }
                else
                {
                    logger?.LogWarning("PDF mapping: [25.MASGALA YAGDAY (Marital Status)] key='{Key}' → person.MaritalStatus is null, field skipped.", maritalKey);
                }

                // 1.PHOTO
                const string photoKey = "topmostSubform[0].Page1[0].ImageField1[0]";
                if (person.Photo != null)
                {
                    data[photoKey] = person.Photo;
                    logger?.LogDebug("PDF mapping: [1.PHOTO] key='{Key}' → byte[] length={Length}.",
                        photoKey, person.Photo is byte[] b ? b.Length : -1);
                }
                else
                {
                    logger?.LogWarning("PDF mapping: [1.PHOTO] key='{Key}' → person.Photo is null, field skipped.", photoKey);
                }
            }
            else
            {
                logger?.LogWarning("PDF mapping: [Person] — item.Person is null, all person fields skipped.");
            }

            // --- 3. Passport Data ---
            var passport = item.CurrentPassport;
            if (passport != null)
            {
                // 17.SAHSY BELGISI (Personal Number)
                const string personalNumKey = "topmostSubform[0].Page1[0]._09[0]";
                data[personalNumKey] = passport.PersonalNumber;
                Log(personalNumKey, "17.SAHSY BELGISI (Personal Number)", passport.PersonalNumber);

                // 19.PASPORTYNYN BELGISI (Passport Number)
                const string passportNumKey = "topmostSubform[0].Page1[0]._11[0]";
                data[passportNumKey] = passport.PassportNumber;
                Log(passportNumKey, "19.PASPORTYNYN BELGISI (Passport Number)", passport.PassportNumber);

                // 20.BERLEN SENESI (Issue Date)
                const string issueDateKey = "topmostSubform[0].Page1[0]._12[0]";
                if (passport.IssueDate.HasValue && passport.IssueDate.Value != DateTime.MinValue)
                {
                    data[issueDateKey] = passport.IssueDate.Value;
                    Log(issueDateKey, "20.BERLEN SENESI (Issue Date)", passport.IssueDate.Value);
                }
                else
                {
                    logger?.LogWarning("PDF mapping: [20.BERLEN SENESI (Issue Date)] key='{Key}' → IssueDate is null or MinValue, field skipped.", issueDateKey);
                }

                // 21.PASPORT MOHLETI (Expiration Date)
                const string expiryKey = "topmostSubform[0].Page1[0]._13[0]";
                if (passport.ExpirationDate.HasValue)
                {
                    data[expiryKey] = passport.ExpirationDate.Value;
                    Log(expiryKey, "21.PASPORT MOHLETI (Expiration Date)", passport.ExpirationDate.Value);
                }
                else
                {
                    logger?.LogWarning("PDF mapping: [21.PASPORT MOHLETI (Expiration Date)] key='{Key}' → ExpirationDate is null, field skipped.", expiryKey);
                }
            }
            else
            {
                logger?.LogWarning("PDF mapping: [Passport] — item.CurrentPassport is null, all passport fields skipped.");
            }

            // --- 4. Address Data ---
            const string addressKey = "topmostSubform[0].Page1[0]._15[0]";
            if (item.CurrentAddressOfResidence != null)
            {
                data[addressKey] = item.CurrentAddressOfResidence.FullAddress;
                Log(addressKey, "Address of Residence", item.CurrentAddressOfResidence.FullAddress);
            }
            else
            {
                logger?.LogWarning("PDF mapping: [Address of Residence] key='{Key}' → item.CurrentAddressOfResidence is null, field skipped.", addressKey);
            }

            logger?.LogDebug("PDF mapping complete. Total keys added to data dictionary: {Count}.", data.Count);
        }
    }
}