using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard: A. Maglumat 3 from vw_rd_work_permit_a_maglumat_3.
/// One person. Every valid work permit is listed in <see cref="WorkPermitBundle"/>.
/// Issued additional-location applications are stacked per permit.
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(PersonName))]
[ModelDefault("Caption", "Goşmaça barjak ýerleri")]
[ModelDefault("AllowEdit", "False")]
[ModelDefault("AllowNew", "False")]
[ModelDefault("AllowDelete", "False")]
public class VwRdWorkPermitMaglumat3 : IRdWorkPermitMaglumatRow
{
    [Key]
    [Browsable(false)]
    public virtual Guid ID { get; set; }

    [Browsable(false)]
    public virtual Guid? PersonOid { get; set; }

    [ForeignKey(nameof(PersonOid))]
    [ModelDefault("Caption", "F.A.A")]
    public virtual Person Person { get; set; }

    [Browsable(false)]
    public virtual Guid? WorkPermitOid { get; set; }

    [ForeignKey(nameof(WorkPermitOid))]
    [Browsable(false)]
    public virtual WorkPermit WorkPermit { get; set; }

    [Browsable(false)]
    public virtual Guid? PassportOid { get; set; }

    [ForeignKey(nameof(PassportOid))]
    [Browsable(false)]
    public virtual Passport Passport { get; set; }

    [Browsable(false)]
    public virtual Guid? VisaOid { get; set; }

    [ForeignKey(nameof(VisaOid))]
    [Browsable(false)]
    public virtual Visa Visa { get; set; }

    [Browsable(false)]
    public virtual string PersonName { get; set; }

    [Browsable(false)]
    public virtual DateTime? DateOfBirth { get; set; }

    [Browsable(false)]
    public virtual string CitizenshipCode { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Doglan ýyly we raýatlygy")]
    public string BirthCitizenshipLabel => JoinLines(FormatDay(DateOfBirth), CitizenshipCode);

    [Browsable(false)]
    public virtual string PassportNumber { get; set; }

    [Browsable(false)]
    public virtual DateTime? PassportExpirationDate { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Pasport belgesi we möhleti")]
    public string PassportLabel => JoinLines(PassportNumber, FormatDay(PassportExpirationDate));

    [Browsable(false)]
    public virtual string EducationLevelLabel { get; set; }

    [Browsable(false)]
    public virtual string EducationInstitutionLabel { get; set; }

    [ModelDefault("Caption", "Bilimine görä hünäri")]
    public virtual string SpecialtyLabel { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Bilimi we okan ýeri")]
    public string EducationLabel
    {
        get
        {
            var level = EducationLevelLabel?.Trim() ?? string.Empty;
            var institution = EducationInstitutionLabel?.Trim() ?? string.Empty;
            if (level.Length == 0)
                return institution;
            if (institution.Length == 0)
                return level;
            return level + ",\n" + institution;
        }
    }

    [ModelDefault("Caption", "Wezipesi")]
    public virtual string PositionLabel { get; set; }

    [Browsable(false)]
    public virtual string AddressRegionTm { get; set; }

    [Browsable(false)]
    public virtual string AddressCityTm { get; set; }

    [Browsable(false)]
    public virtual string AddressStreet { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Türkmenistandaky anyk ýaşaýan salgysy")]
    public string AddressLabel =>
        AddressOfResidenceReportText.CityAndStreet(AddressRegionTm, AddressCityTm, AddressStreet);

    [Browsable(false)]
    public virtual string WorkPermitNumber { get; set; }

    [Browsable(false)]
    public virtual DateTime? StartDate { get; set; }

    [Browsable(false)]
    public virtual DateTime? ExpirationDate { get; set; }

    [Browsable(false)]
    public virtual string WorkPermitBundle { get; set; }

    [ModelDefault("Caption", "Rugsat berlen ýerler")]
    public virtual string WorkPermitLocationsLabel { get; set; }

    [ModelDefault("Caption", "Goşmaça barjak ýerleri")]
    public virtual string WorkPermitAdditionalLocationsLabel { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Rugsatnama belgesi we möhleti")]
    public string WorkPermitLabel
    {
        get
        {
            var stacked = WorkPermitBundleText.Display(WorkPermitBundle);
            return stacked.Length > 0
                ? stacked
                : JoinLines(WorkPermitNumber, FormatDay(StartDate), FormatDay(ExpirationDate));
        }
    }

    [Browsable(false)]
    public virtual string VisaNumber { get; set; }

    [Browsable(false)]
    public virtual DateTime? VisaStartDate { get; set; }

    [Browsable(false)]
    public virtual DateTime? VisaEndDate { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Wiza belgesi we möhleti")]
    public string VisaLabel => JoinLines(VisaNumber, FormatDay(VisaStartDate), FormatDay(VisaEndDate));

    [ModelDefault("Caption", "Bellik")]
    public virtual string Notes { get; set; }

    [Browsable(false)]
    public virtual string ProjectName { get; set; }

    [Browsable(false)]
    public virtual string ProjectNameRaw { get; set; }

    [Browsable(false)]
    public virtual string ProjectNameTm { get; set; }

    [Browsable(false)]
    public virtual int PersonRoleCode { get; set; }

    [Browsable(false)]
    public virtual string StatusLabel { get; set; }

    [Browsable(false)]
    public virtual string StatusCssClass { get; set; }

    [Browsable(false)]
    public virtual bool IsArchived { get; set; }

    private static string FormatDay(DateTime? date)
    {
        if (date is not { } value || value.Year <= 1900)
            return string.Empty;
        return value.ToString("dd.MM.yyyy");
    }

    private static string JoinLines(params string?[] lines)
    {
        var kept = new System.Collections.Generic.List<string>();
        foreach (var line in lines)
        {
            var text = line?.Trim() ?? string.Empty;
            if (text.Length > 0)
                kept.Add(text);
        }

        return string.Join("\n", kept);
    }
}