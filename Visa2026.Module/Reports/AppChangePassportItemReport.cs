using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// ApplicationItem-level dual-section personnel list for App_Change_Passport.
    ///
    /// Strategy: BeforePrint doubles the ApplicationItem collection into a
    /// List&lt;PassportChangeRow&gt; — first half with IsKone=true (old passport),
    /// second half with IsKone=false (new passport). A GroupHeaderBand grouped on
    /// IsKone (descending) produces the two section headers automatically.
    ///
    /// Reference image: Resources/FormTemplates/App_Change_Passport_item.jpg
    /// </summary>
    public partial class AppChangePassportItemReport : AppItemBaseReport
    {
        public AppChangePassportItemReport()
        {
            InitializeComponent();
            this.BeforePrint += (s, e) => PrepareDataSource();
        }

        private void PrepareDataSource()
        {
            if (this.DataSource is not IEnumerable raw) return;

            var items = raw.Cast<Visa2026.Module.BusinessObjects.ApplicationItem>().ToList();
            if (items.Count == 0) return;

            var rows = items.Select(i => BuildRow(i, isKone: true))
                            .Concat(items.Select(i => BuildRow(i, isKone: false)))
                            .ToList();

            this.DataSource = rows;
            this.DataMember = null;
        }

        private static PassportChangeRow BuildRow(
            Visa2026.Module.BusinessObjects.ApplicationItem item, bool isKone)
        {
            return new PassportChangeRow
            {
                IsKone               = isKone,
                PersonLastName       = item.Person_LastName,
                PersonFirstName      = item.Person_FirstName,
                PersonBirthInfo      = $"{item.Person_DateOfBirthText}\n{item.Person_CountryOfBirthTm}/{item.Person_BirthPlace}",
                PersonGenderTm       = item.Person_GenderTm,
                PersonNationalityCode = item.Person_NationalityCode,
                PassportBelgisi      = isKone
                    ? $"{item.PreviousPassport_Number}\n{item.PreviousPassport_ExpirationDateText}"
                    : $"{item.Passport_Number}\n{item.Passport_ExpirationDateText}",
                BilimiWeOkanYeri     = $"{item.Education_LevelTm}\n{item.Education_InstitutionName}",
                BilimineGoreHunari   = item.Education_SpecialtyTm,
                Wezipesi             = item.Position_PositionTm,
                MohletiWeGezekligi   = $"{item.Visa_StartDateText}\n{item.Visa_ExpirationDateText}\n{item.Visa_Number}",
                TmSalgysy            = item.Address_FullAddress,
                DasarySalgysy        = item.Person_ForeignAddress,
                BarjakSerhetYakasy   = item.WorkPermit_WorkPermittedLocations
            };
        }

        // ---- inner DTO -------------------------------------------------------
        // Properties named to match ExpressionBinding [PropertyName] syntax.
        private class PassportChangeRow
        {
            public bool   IsKone               { get; set; }
            public string PersonLastName        { get; set; }
            public string PersonFirstName       { get; set; }
            public string PersonBirthInfo       { get; set; }
            public string PersonGenderTm        { get; set; }
            public string PersonNationalityCode { get; set; }
            public string PassportBelgisi       { get; set; }
            public string BilimiWeOkanYeri      { get; set; }
            public string BilimineGoreHunari    { get; set; }
            public string Wezipesi              { get; set; }
            public string MohletiWeGezekligi    { get; set; }
            public string TmSalgysy             { get; set; }
            public string DasarySalgysy         { get; set; }
            public string BarjakSerhetYakasy    { get; set; }
        }
    }
}
