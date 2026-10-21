using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard: aktiv personeller from vw_rd_visa_aktiv_personeller.
/// One person row from Active Visa. PROJECT is the department.
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(PersonName))]
[ModelDefault("Caption", "aktiv personeller")]
[ModelDefault("AllowEdit", "False")]
[ModelDefault("AllowNew", "False")]
[ModelDefault("AllowDelete", "False")]
public class VwRdVisaAktivPersoneller
{
    [Key]
    [Browsable(false)]
    public virtual Guid ID { get; set; }

    [Browsable(false)]
    public virtual Guid? PersonOid { get; set; }

    [ForeignKey(nameof(PersonOid))]
    [Browsable(false)]
    public virtual Person Person { get; set; }

    [Browsable(false)]
    public virtual Guid? VisaOid { get; set; }

    [ModelDefault("Caption", "PROJECT")]
    public virtual string DepartmentLabel { get; set; }

    [ModelDefault("Caption", "Firması")]
    public virtual string SubcontractorLabel { get; set; }

    [ModelDefault("Caption", "Adı Soyadı")]
    public virtual string PersonName { get; set; }

    [Browsable(false)]
    public virtual DateTime? DateOfBirth { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Doğum Tarihi")]
    public string BirthDateLabel => FormatDay(DateOfBirth);

    [ModelDefault("Caption", "Mevcut Pasaport No")]
    public virtual string PassportNumber { get; set; }

    [Browsable(false)]
    public virtual DateTime? PassportExpirationDate { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Pasaport döneminin bitiş tarihi")]
    public string PassportEndLabel => FormatDay(PassportExpirationDate);

    [ModelDefault("Caption", "Mesleki Görevleri")]
    public virtual string PositionLabel { get; set; }

    [Browsable(false)]
    public virtual DateTime? WorkPermitEndDate { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Çalışma İzni bittiği tarih")]
    public string WorkPermitEndLabel => FormatDay(WorkPermitEndDate);

    [ModelDefault("Caption", "Vize Numarası")]
    public virtual string VisaNumber { get; set; }

    [Browsable(false)]
    public virtual DateTime? VisaEndDate { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Vizenin bitiş tarihi")]
    public string VisaEndLabel => FormatDay(VisaEndDate);

    [NotMapped]
    [ModelDefault("Caption", "Vize bitmesine kalan süre")]
    public string VisaDaysRemainingLabel =>
        VwRdWorkPermitCalismaIzniListesi.FormatVisaDaysRemaining(VisaEndDate);

    [Browsable(false)]
    public virtual string AddressRegionTm { get; set; }

    [Browsable(false)]
    public virtual string AddressCityTm { get; set; }

    [Browsable(false)]
    public virtual string AddressStreet { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Mevcut Adresi")]
    public string AddressLabel =>
        AddressOfResidenceReportText.CityAndStreet(AddressRegionTm, AddressCityTm, AddressStreet);

    [NotMapped]
    [ModelDefault("Caption", "NOT")]
    public string NotesLabel => string.Empty;

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
}
