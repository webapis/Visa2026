using System;

namespace Visa2026.Module.BusinessObjects;

/// <summary>Shared shape for ApplicationProfileInstance (via ministry) Report Dashboard SQL view rows (ApplicationRosterMergeLine grain).</summary>
public interface IVwRdApplicationViaMinistryRow
{
    Guid ID { get; set; }
    Guid? ApplicationItemOid { get; set; }
    string PersonName { get; set; }
    string ProjectName { get; set; }
    string ProjectNameRaw { get; set; }
    string ProjectNameTm { get; set; }
    int PersonRoleCode { get; set; }
    string PositionLabel { get; set; }
    string ApplicationTypeLabel { get; set; }
    string VisaPeriodLabel { get; set; }
    string VisaTypeLabel { get; set; }
    string ApplicationNumber { get; set; }
    DateTime? ApplicationDate { get; set; }
    string ProgressStateCode { get; set; }
    string StatusLabel { get; set; }
    bool IsArchived { get; set; }
    string PeriodLabel { get; set; }
    string CategoryLabel { get; set; }
    string TypeLabel { get; set; }
}
