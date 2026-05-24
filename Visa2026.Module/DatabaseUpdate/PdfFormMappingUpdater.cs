using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate
{
    public class PdfFormMappingUpdater : ModuleUpdater
    {
        public PdfFormMappingUpdater(IObjectSpace objectSpace, Version currentDBVersion) :
            base(objectSpace, currentDBVersion)
        {
        }

        public override void UpdateDatabaseAfterUpdateSchema()
        {
            base.UpdateDatabaseAfterUpdateSchema();
            SeedPdfFormMappings();
            ObjectSpace.CommitChanges();
        }

        private void SeedPdfFormMappings()
        {
            // Application Level
            CreateMappingIfNotExists("topmostSubform[0].Page1[0].L01[0]", "Application.ApplicationType.PdfForm_Code", "Visa operation type", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0].L02[0]", "Application.Urgency.PdfForm_Code", "Urgency (Dropdown)", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page2[0]._25[0]", "Application.VisaType.PdfForm_Code", "Visa Type (Application Level)", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page2[0]._27[0]", "Application.VisaPeriod.PdfForm_Count", "Duration of stay (count)", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page2[0]._271[0]", "Application.VisaPeriod.PdfForm__Code", "Duration of stay (unit)", PdfMappingMode.Property);

            // Company Info
            CreateMappingIfNotExists("topmostSubform[0].Page1[0].L10[0]", "Application.Application_Company_Name", "Company Name", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0].L11[0]", "Application.Application_Company_Address", "Company Address", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0].L13[0]", "Application.Application_Company_PhoneNumber", "Company Phone", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0].L12[0]", "Application.Application_Company_Email", "Company Email", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0].IP[1].#field[0]", null, "Legal Entity Checkbox", PdfMappingMode.Constant, "true");

            // Person Level
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._01[0]", "Person.LastName", "Last Name", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._03[0]", "Person.FirstName", "First Name", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._04[0]", "Person.DateOfBirth", "Date of Birth", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._05[0]", "Person.Gender.PdfForm_Code", "Gender", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._18[0]", "Person.MaritalStatus.PdfForm_Code", "Marital Status", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._08[0]", "Person.BirthPlace", "Birth Place", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._06[0]", "Person.CountryOfBirth.Code", "Country of Birth", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._07[0]", "Person.Nationality.Code", "Citizenship", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._15[0]", null, "Foreign Address (Country + Address)", PdfMappingMode.Expression, "Concat(Person.ForeignAddressCountry.Code, ', ', Person.ForeignAddress)");

            // Education
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._19[0]", "CurrentEducation.EducationLevel.PdfForm_Code", "Education Level", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._20[0]", "CurrentEducation.Specialty.Name", "Specialty", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._21[0]", null, "Education Place (Country + Institution)", PdfMappingMode.Expression, "Concat(CurrentEducation.EducationCountry.Name, ', ', CurrentEducation.EducationInstitution.Name)");

            // Work
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._23[0]", "CurrentPositionHistory.Position.Name", "Work Position", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._22[0]", null, "Work Place and Phone", PdfMappingMode.Expression, "Concat(Application.Application_Company_Name, ', ', Application.Application_Company_PhoneNumber)");

            // Photo
            CreateMappingIfNotExists("topmostSubform[0].Page1[0].ImageField1[0]", "Person.Photo", "Photo", PdfMappingMode.Property);

            // Passport
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._10[0]", "CurrentPassport.PassportType.PdfForm_Code", "Passport Type", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._09[0]", "Person.PersonalNumber", "Personal Number", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._11[0]", "CurrentPassport.PassportNumber", "Passport Number", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._12[0]", "CurrentPassport.IssueDate", "Passport Issue Date", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._13[0]", "CurrentPassport.ExpirationDate", "Passport Expiration Date", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._14[0]", "CurrentPassport.IssuedCountry.Code", "Passport Issued Country", PdfMappingMode.Property);

            // Visa
            CreateMappingIfNotExists("topmostSubform[0].Page2[0]._26[0]", "Application.VisaCategory.PdfForm_Code", "Visa Category", PdfMappingMode.Property);

            // Address of Residence
            CreateMappingIfNotExists("topmostSubform[0].Page2[0]._33[0]", "CurrentAddressOfResidence.Region.PdfForm_Code", "Region of stay", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page2[0]._34[0]", "CurrentAddressOfResidence.City.PdfForm_Code", "District of stay", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page2[0]._35[0]", "CurrentAddressOfResidence.FullAddress", "Stay address", PdfMappingMode.Property);

            // Family members (master FamilyMembers or manual text) — mapped to additional text on page 1; remap in DB if your template uses another field.
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._241[0]", "Pdf_FamilyMembersAggregateText", "Family members aggregate (master or manual)", PdfMappingMode.Property);

            // Spouse (employee master FamilyMembers — Relationship marked spouse)
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._181[0]", "Pdf_SpouseLastName", "Spouse last name", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._182[0]", "Pdf_SpouseFirstName", "Spouse first name", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._183[0]", "Pdf_SpouseAdditional", "Spouse additional (middle name, DOB)", PdfMappingMode.Property);

            // Accompanying traveller (co-applicant FM line for employees; sponsoring employee for FM items)
            CreateMappingIfNotExists("topmostSubform[0].Page2[0]._46[0]", "Pdf_AccompanyingFullName", "Accompanying person name", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page2[0]._45[0]", "Pdf_AccompanyingNationalityCode", "Accompanying nationality (ISO alpha-3)", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page2[0]._47[0]", "Pdf_AccompanyingDetail1", "Accompanying detail — relationship (Tm)", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page2[0]._48[0]", "Pdf_AccompanyingDetail2", "Accompanying detail — date of birth", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page2[0]._49[0]", "Pdf_AccompanyingDetail3", "Accompanying detail — passport no.", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page2[0]._50[0]", "Pdf_AccompanyingDetail4", "Accompanying detail — personal ID", PdfMappingMode.Property);
        }

        private void CreateMappingIfNotExists(string pdfKey, string propertyPath, string description, PdfMappingMode mode, string expressionOrConstant = null)
        {
            var existingMapping = ObjectSpace.FirstOrDefault<PdfFormMapping>(m => m.PdfFieldKey == pdfKey);
            if (existingMapping == null)
            {
                var newMapping = ObjectSpace.CreateObject<PdfFormMapping>();
                newMapping.PdfFieldKey = pdfKey;
                newMapping.Description = description;
                newMapping.MappingMode = mode;

                if (mode == PdfMappingMode.Property)
                {
                    newMapping.PropertyPath = propertyPath;
                }
                else if (mode == PdfMappingMode.Expression)
                {
                    newMapping.Expression = expressionOrConstant;
                }
                else if (mode == PdfMappingMode.Constant)
                {
                    newMapping.ConstantValue = expressionOrConstant;
                }
            }
        }
    }
}