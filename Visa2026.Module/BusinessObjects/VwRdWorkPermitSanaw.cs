using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard: Sanaw from vw_rd_work_permit_sanaw.
/// Same people as A. Maglumat. Columns follow the sanaw sheet.
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(LastName))]
[ModelDefault("Caption", "Sanaw")]
[ModelDefault("AllowEdit", "False")]
[ModelDefault("AllowNew", "False")]
[ModelDefault("AllowDelete", "False")]
public class VwRdWorkPermitSanaw
{
    [Key]
    [Browsable(false)]
    public virtual Guid ID { get; set; }

    [Browsable(false)]
    public virtual Guid? PersonOid { get; set; }

    [ForeignKey(nameof(PersonOid))]
    [Browsable(false)]
    public virtual Person Person { get; set; }

    [ModelDefault("Caption", "Familiýasy")]
    public virtual string LastName { get; set; }

    [ModelDefault("Caption", "Ady")]
    public virtual string FirstName { get; set; }

    [Browsable(false)]
    public virtual DateTime? DateOfBirth { get; set; }

    [Browsable(false)]
    public virtual string BirthPlace { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Doglan senesi we ýeri")]
    public string BirthPlaceLabel => JoinLines(FormatDay(DateOfBirth), BirthPlace);

    [ModelDefault("Caption", "Jynsy")]
    public virtual string GenderLabel { get; set; }

    [ModelDefault("Caption", "Raýatlygy")]
    public virtual string CitizenshipCode { get; set; }

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

    [ModelDefault("Caption", "Bilimine görä hünäri")]
    public virtual string SpecialtyLabel { get; set; }

    [ModelDefault("Caption", "Wezipesi")]
    public virtual string PositionLabel { get; set; }

    [Browsable(false)]
    public virtual DateTime? SalaryStart { get; set; }

    [Browsable(false)]
    public virtual DateTime? SalaryEnd { get; set; }

    [Browsable(false)]
    public virtual string SalaryAmount { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Möhleti we gazanji")]
    public string SalaryLabel => JoinLines(FormatDay(SalaryStart), FormatDay(SalaryEnd), SalaryAmount);

    [Browsable(false)]
    public virtual string AddressRegionTm { get; set; }

    [Browsable(false)]
    public virtual string AddressCityTm { get; set; }

    [Browsable(false)]
    public virtual string AddressStreet { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Türkmenistandaky salgysy")]
    public string AddressLabel =>
        AddressOfResidenceReportText.CityAndStreet(AddressRegionTm, AddressCityTm, AddressStreet);

    [ModelDefault("Caption", "Daşary ýurtdaky salgysy")]
    public virtual string ForeignAddressLabel { get; set; }

    [ModelDefault("Caption", "Taslamasy")]
    public virtual string ProjectName { get; set; }

    [Browsable(false)]
    public virtual string ProjectNameRaw { get; set; }

    [Browsable(false)]
    public virtual string ProjectNameTm { get; set; }

    [ModelDefault("Caption", "Maşgala agzalary")]
    public virtual string FamilyMembersLabel { get; set; }

    [Browsable(false)]
    public virtual int FamilyMemberCount { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Adam sany")]
    public string PersonCountLabel =>
        FamilyMemberCount > 0 ? (FamilyMemberCount + 1).ToString() : string.Empty;

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