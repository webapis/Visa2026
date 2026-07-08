namespace Visa2026.Module.Services.LegacySyncDashboard;

internal static class LegacySyncDashboardScalarDefinitions
{
    internal sealed record ScalarRow(string BO, string LegacyQuery, string TargetQuery, string Note = "");

    internal static readonly ScalarRow[] Rows =
    [
        new("Person", "SELECT COUNT(*) FROM dbo.Person WHERE GCRecord IS NULL", "SELECT COUNT(*) FROM People", ""),
        new("Passport", "SELECT COUNT(*) FROM dbo.Passport pp INNER JOIN dbo.Person p ON pp.Person = p.Oid AND p.GCRecord IS NULL WHERE pp.GCRecord IS NULL", "SELECT COUNT(*) FROM Passports WHERE GCRecord IS NULL OR GCRecord = 0", ""),
        new("Visa", "SELECT COUNT(*) FROM dbo.Visa WHERE GCRecord IS NULL", "SELECT COUNT(*) FROM Visas WHERE GCRecord IS NULL OR GCRecord = 0", ""),
        new("Education", "SELECT COUNT(*) FROM dbo.Education WHERE GCRecord IS NULL", "SELECT COUNT(*) FROM Educations WHERE GCRecord IS NULL OR GCRecord = 0", ""),
        new("EmployeePositionHistory", "SELECT COUNT(*) FROM dbo.WorkHistoryOfEmployee WHERE GCRecord IS NULL", "SELECT COUNT(*) FROM EmployeePositionHistories WHERE GCRecord IS NULL OR GCRecord = 0", ""),
        new("EmployeeSalary", "SELECT COUNT(*) FROM dbo.Employee e INNER JOIN dbo.Person p ON p.Oid = e.Oid AND p.GCRecord IS NULL", "SELECT COUNT(*) FROM EmployeeSalaries WHERE GCRecord IS NULL OR GCRecord = 0", "legacy = Employee scope"),
        new("AddressOfResidence", "SELECT COUNT(*) FROM dbo.AddressOfResidence WHERE GCRecord IS NULL", "SELECT COUNT(*) FROM AddressesOfResidence WHERE GCRecord IS NULL OR GCRecord = 0", "prod may exceed legacy (PIA inferred)"),
        new("MedicalRecord", "SELECT COUNT(*) FROM dbo.IPersonn_SpidKepilnama WHERE GCRecord IS NULL", "SELECT COUNT(*) FROM MedicalRecords WHERE GCRecord IS NULL OR GCRecord = 0", ""),
        new("Application", "SELECT COUNT(*) FROM dbo.Application WHERE GCRecord IS NULL", "SELECT COUNT(*) FROM Applications WHERE IsManualEntry = 1 AND (GCRecord IS NULL OR GCRecord = 0)", "manual-entry only"),
        new("WorkPermit", "SELECT COUNT(*) FROM dbo.WorkPermitLetter WHERE GCRecord IS NULL", "SELECT COUNT(*) FROM WorkPermits WHERE GCRecord IS NULL OR GCRecord = 0", ""),
        new("WorkPermitItem", "SELECT COUNT(*) FROM dbo.WorkPermit WHERE GCRecord IS NULL", "SELECT COUNT(*) FROM WorkPermitItems WHERE GCRecord IS NULL OR GCRecord = 0", ""),
        new("Invitation", "SELECT COUNT(*) FROM dbo.ApplicationResult WHERE GCRecord IS NULL", "SELECT COUNT(*) FROM Invitations WHERE GCRecord IS NULL OR GCRecord = 0", ""),
        new("InvitationItem", "SELECT COUNT(*) FROM dbo.PersonInInvitation WHERE GCRecord IS NULL", "SELECT COUNT(*) FROM InvitationItems WHERE GCRecord IS NULL OR GCRecord = 0", ""),
        new("ApplicationItem", "SELECT COUNT(*) FROM dbo.PersonInApplication WHERE GCRecord IS NULL", "SELECT COUNT(*) FROM ApplicationItems ai INNER JOIN Applications a ON ai.ApplicationID = a.ID WHERE a.IsManualEntry = 1 AND (a.GCRecord IS NULL OR a.GCRecord = 0) AND (ai.GCRecord IS NULL OR ai.GCRecord = 0)", "manual-entry items"),
        new("ApplicationProgress", "SELECT COUNT(*) FROM dbo.Application WHERE GCRecord IS NULL", "SELECT COUNT(*) FROM ApplicationProgresses ap INNER JOIN Applications a ON ap.ApplicationID = a.ID WHERE a.IsManualEntry = 1 AND (a.GCRecord IS NULL OR a.GCRecord = 0) AND (ap.GCRecord IS NULL OR ap.GCRecord = 0)", "synthetic multi-step per app"),
    ];
}