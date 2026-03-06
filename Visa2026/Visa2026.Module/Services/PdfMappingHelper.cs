using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services
{
    internal static class PdfMappingHelper
    {
        // This method is extracted from ApplicationItemPdfController to be reused.
        public static void MapApplicationData(Dictionary<string, object> data, Application application, ApplicationItem item)
        {
            // --- 1. Application Level Data ---

            // Urgency (3.TIZLIGI)
            if (application.Urgency != null)
            {
                data["topmostSubform[0].Page1[0].L02[0]"] = application.Urgency.Name; // Assuming Name property exists
            }

            // Company / Inviting Party Info
            if (application.Company != null)
            {
                // 4.CAGYRYAN TARAP YURIDIKI SAHS (Legal Entity Checkbox)
                data["topmostSubform[0].Page1[0].IP[1].#field[0]"] = true;

                // 5.KARHANANYN ADY (Company Name)
                data["topmostSubform[0].Page1[0].L10[0]"] = application.Company.Name; // Assuming Name property exists

                // 6.HUKUK SALGYSY (Address)
                data["topmostSubform[0].Page1[0].L11[0]"] = application.Company.Address; // Assuming Address property exists

                // 8.TELEFON (Phone)
                data["topmostSubform[0].Page1[0].L13[0]"] = application.Company.PhoneNumber; // Assuming PhoneNumber property exists
            }

            // --- 2. Person Level Data ---
            var person = item.Person;
            if (person != null)
            {
                // 9.FAMILIYASY (Last Name)
                data["topmostSubform[0].Page1[0]._01[0]"] = person.LastName;

                // 11.ADY (First Name)
                data["topmostSubform[0].Page1[0]._03[0]"] = person.FirstName;

                // 12.DOGLAN SENESI (Date of Birth)
                if (person.DateOfBirth != DateTime.MinValue)
                {
                    data["topmostSubform[0].Page1[0]._04[0]"] = person.DateOfBirth;
                }

                // 13.GYNSY (Gender)
                if (person.Gender != null)
                {
                    data["topmostSubform[0].Page1[0]._05[0]"] = person.Gender.Name; // Assuming Name property exists
                }

                // 16.DOGLAN YERI (Birth Place)
                data["topmostSubform[0].Page1[0]._08[0]"] = person.BirthPlace;

                // 25.MASGALA YAGDAY (Marital Status)
                if (person.MaritalStatus != null)
                {
                    data["topmostSubform[0].Page1[0]._18[0]"] = person.MaritalStatus.Name; // Assuming Name property exists
                }

                // 1.PHOTO
                if (person.Photo != null)
                {
                    // Note: You might need to verify the exact field name for the image in your PDF
                    data["ImageField1"] = person.Photo;
                }
            }

            // --- 3. Passport Data ---
            var passport = item.CurrentPassport;
            if (passport != null)
            {
                // 17.SAHSY BELGISI (Personal Number)
                data["topmostSubform[0].Page1[0]._09[0]"] = passport.PersonalNumber;

                // 19.PASPORTYNYN BELGISI (Passport Number)
                data["topmostSubform[0].Page1[0]._11[0]"] = passport.PassportNumber;

                // 20.BERLEN SENESI (Issue Date)
                if (passport.IssueDate != DateTime.MinValue)
                {
                    data["topmostSubform[0].Page1[0]._12[0]"] = passport.IssueDate;
                }

                // 21.PASPORT MOHLETI (Expiration Date)
                if (passport.ExpirationDate.HasValue)
                {
                    data["topmostSubform[0].Page1[0]._13[0]"] = passport.ExpirationDate.Value;
                }
            }

            // --- 4. Address Data ---
            if (item.CurrentAddressOfResidence != null)
            {
                // Using FullAddress as it is the available property in AddressOfResidence
                data["topmostSubform[0].Page1[0]._15[0]"] = item.CurrentAddressOfResidence.FullAddress;
            }
        }
    }
}