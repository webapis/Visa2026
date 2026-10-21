using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard: uzatılanlar from vw_rd_visa_uzatilanlar.
/// Same people as Visa On Extension. Projesi is the department.
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(PersonName))]
[ModelDefault("Caption", "uzatılanlar")]
[ModelDefault("AllowEdit", "False")]
[ModelDefault("AllowNew", "False")]
[ModelDefault("AllowDelete", "False")]
public class VwRdVisaUzatilanlar
{
    [Key]
    [Browsable(false)]
    public virtual Guid ID { get; set; }

    [Browsable(false)]
    public virtual Guid? ApplicationProfileInstanceOid { get; set; }

    [ForeignKey(nameof(ApplicationProfileInstanceOid))]
    [Browsable(false)]
    public virtual ApplicationProfileInstance ApplicationProfileInstance { get; set; }

    [Browsable(false)]
    public virtual Guid? PersonOid { get; set; }

    [ForeignKey(nameof(PersonOid))]
    [Browsable(false)]
    public virtual Person Person { get; set; }

    [Browsable(false)]
    public virtual Guid? VisaOid { get; set; }

    [Browsable(false)]
    public virtual string PassportNumber { get; set; }

    [ModelDefault("Caption", "Adı Soyadı")]
    public virtual string PersonName { get; set; }

    [ModelDefault("Caption", "Görevi")]
    public virtual string PositionLabel { get; set; }

    [ModelDefault("Caption", "Projesi")]
    public virtual string DepartmentLabel { get; set; }

    [ModelDefault("Caption", "Firması")]
    public virtual string SubcontractorLabel { get; set; }

    [Browsable(false)]
    public virtual DateTime? CurrentVisaEndDate { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Mevcut Vize bitiş tarihi")]
    public string CurrentVisaEndLabel => FormatDay(CurrentVisaEndDate);

    [ModelDefault("Caption", "Davetiye için yazı No")]
    public virtual string ApplicationNumber { get; set; }

    [Browsable(false)]
    public virtual DateTime? ApplicationDate { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "İlgili Bakanlığın Başvuru tarihi")]
    public string ApplicationDateLabel => FormatDay(ApplicationDate);

    [ModelDefault("Caption", "Başvurulan vize süresi")]
    public virtual string VisaPeriodLabel { get; set; }

    [Browsable(false)]
    public virtual DateTime? LastApprovalDate { get; set; }

    [Browsable(false)]
    public virtual string LastApprovalNumber { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "İlgili Bakanlıktan çıkan tarih ve numarası")]
    public string LastApprovalLabel => FormatDecision(LastApprovalDate, LastApprovalNumber);

    [Browsable(false)]
    public virtual DateTime? MigrationSubmittedDate { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Yabancı Şubeye veriliş tarihi")]
    public string MigrationSubmittedLabel => FormatDay(MigrationSubmittedDate);

    [Browsable(false)]
    public virtual DateTime? MigrationDate { get; set; }

    [Browsable(false)]
    public virtual string MigrationResultNumber { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Yabancı Şube CO No ve tarihi")]
    public string MigrationLabel => FormatDecision(MigrationDate, MigrationResultNumber);

    [Browsable(false)]
    public virtual string ProgressStateCode { get; set; }

    [Browsable(false)]
    public virtual string MinistryShortName { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Şu anki Durumu")]
    public string CurrentStatusLabel => FormatHeldStatus(ProgressStateCode, MinistryShortName);

    [NotMapped]
    [ModelDefault("Caption", "Açıklama")]
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

    public static string FormatHeldStatus(string? stateCode, string? ministryShortName)
    {
        var code = (stateCode ?? string.Empty).Trim();
        if (code.Equals("SUBMITTED_MIGRATION", StringComparison.OrdinalIgnoreCase)
            || code.Equals("PROCESS_STARTED", StringComparison.OrdinalIgnoreCase))
            return "Yabancıların şubesinde";

        if (code.Length >= 16
            && code[0] is >= '1' and <= '5'
            && code[1] == '_'
            && code.EndsWith("_REVIEW_STARTED", StringComparison.Ordinal))
            return TurkishMinistryLocative(ministryShortName);

        return "OFISDE";
    }

    public static string FormatDecision(DateTime? date, string? number)
    {
        var lines = new List<string>();
        var day = FormatDay(date);
        if (day.Length > 0)
            lines.Add(day);
        var value = number?.Trim() ?? string.Empty;
        if (value.Length > 0)
            lines.Add("№ " + value);
        return string.Join("\n", lines);
    }

    private static string TurkishMinistryLocative(string? shortName)
    {
        var name = shortName?.Trim() ?? string.Empty;
        if (name.Length == 0)
            return string.Empty;
        if (MinistryLocative.TryGetValue(name, out var phrase))
            return phrase;
        return name;
    }

    private static readonly Dictionary<string, string> MinistryLocative = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Energetika"] = "Enerji Bakanlığında",
        ["Gurluşyk"] = "İnşaat Bakanlığında",
        ["Türkmenenergo"] = "Türkmenenergo'da",
        ["Türkmengaz"] = "Türkmengaz'da",
        ["Türkmenhimiýa"] = "Türkmenhimiýa'da",
        ["Türkmennebit"] = "Türkmennebit'te",
        ["Aşgabat häkimlik"] = "Aşgabat häkimliginde",
        ["TNGIZ"] = "TNGIZ'de",
    };

    private static string FormatDay(DateTime? date)
    {
        if (date is not { } value || value.Year <= 1900)
            return string.Empty;
        return value.ToString("dd.MM.yyyy");
    }
}
