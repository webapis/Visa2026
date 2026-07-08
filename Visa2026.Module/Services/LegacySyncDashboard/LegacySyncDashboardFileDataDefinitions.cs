namespace Visa2026.Module.Services.LegacySyncDashboard;

internal static class LegacySyncDashboardFileDataDefinitions
{
    internal sealed record FileDataRow(
        string BO,
        string? LegacyQuery,
        string TargetQuery,
        string? IdMapEntity = null,
        string Note = "");

    /// <summary>Document-copy / photo waves (same set as OnPremSyncState Get-OnPremFileRowDefinitions).</summary>
    internal static readonly FileDataRow[] Rows =
    [
        new(
            "Person.Photo",
            "SELECT COUNT(*) FROM dbo.Person WHERE GCRecord IS NULL AND Photo IS NOT NULL AND DATALENGTH(Photo) > 0",
            "SELECT COUNT(*) FROM People WHERE Photo IS NOT NULL AND DATALENGTH(Photo) > 0",
            null,
            "person photo blob"),
        new(
            "PassportDocument",
            "SELECT COUNT(*) FROM dbo.PassportCopy pc INNER JOIN dbo.Passport p ON pc.Passport = p.Oid AND p.GCRecord IS NULL WHERE pc.GCRecord IS NULL AND pc.Passport IS NOT NULL",
            "SELECT COUNT(*) FROM PassportDocuments WHERE GCRecord IS NULL OR GCRecord = 0",
            "PassportCopy",
            "legacy PassportCopy with Passport FK"),
        new(
            "EducationDocument",
            "SELECT COUNT(*) FROM dbo.PassportCopy pc INNER JOIN dbo.Education e ON pc.Education = e.Oid AND e.GCRecord IS NULL WHERE pc.GCRecord IS NULL AND pc.Education IS NOT NULL",
            "SELECT COUNT(*) FROM EducationDocument WHERE GCRecord IS NULL OR GCRecord = 0",
            "EducationDocument",
            "legacy PassportCopy with Education FK"),
        new(
            "VisaDocument",
            "SELECT COUNT(*) FROM dbo.Visa v INNER JOIN dbo.Passport p ON v.Passport = p.Oid AND p.GCRecord IS NULL WHERE v.GCRecord IS NULL AND v.[G\u00F6\u00E7\u00FCrmeNusga] IS NOT NULL AND DATALENGTH(v.[G\u00F6\u00E7\u00FCrmeNusga]) > 0",
            "SELECT COUNT(*) FROM VisaDocument WHERE GCRecord IS NULL OR GCRecord = 0",
            "VisaDocument",
            "legacy Visa goceurme blobs"),
        new(
            "WorkPermitDocument",
            null,
            "SELECT COUNT(*) FROM WorkPermitDocuments WHERE GCRecord IS NULL OR GCRecord = 0",
            "WorkPermitDocument",
            "legacy scope = id-map when SQL omitted"),
        new(
            "InvitationDocument",
            null,
            "SELECT COUNT(*) FROM InvitationDocuments WHERE GCRecord IS NULL OR GCRecord = 0",
            "InvitationDocument",
            "legacy scope = id-map when SQL omitted"),
        new(
            "FamilyProofDocument",
            "SELECT COUNT(*) FROM dbo.FamilyProofDocument WHERE GCRecord IS NULL",
            "SELECT COUNT(*) FROM PersonFamilyRelationDocuments WHERE GCRecord IS NULL OR GCRecord = 0",
            "FamilyProofDocument",
            ""),
        new(
            "MedicalRecordDocument",
            "SELECT COUNT(*) FROM dbo.Copy c WHERE c.GCRecord IS NULL AND c.IPersonn_SpidKepilnama IS NOT NULL",
            "SELECT COUNT(*) FROM MedicalRecordDocuments WHERE GCRecord IS NULL OR GCRecord = 0",
            "MedicalRecordDocument",
            "spid kepilnama scans"),
        new(
            "FileData (all)",
            null,
            "SELECT COUNT(*) FROM FileData WHERE GCRecord IS NULL OR GCRecord = 0",
            null,
            "target FileData table total"),
    ];
}