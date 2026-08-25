using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014LegacyDocumentCancellationIndexTests
{
    private static readonly Guid VisaOid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid WorkPermitOid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly ApplicationTypeVisibilityCatalog Visibility =
        ApplicationTypeVisibilityCatalog.Load();

    private static Dictionary<string, string?> EvidenceRow(
        string? visaOid = null,
        string? workPermitOid = null,
        bool cancelled = false,
        bool isComplete = false,
        bool forEmployee = true,
        int? employeeSubtypeId = null) =>
        new(StringComparer.Ordinal)
        {
            ["VisaOid"] = visaOid,
            ["WorkPermitOid"] = workPermitOid,
            ["Cancelled"] = cancelled ? "1" : "0",
            ["IsComplete"] = isComplete ? "1" : "0",
            ["ForEmployee"] = forEmployee ? "1" : "0",
            ["ForFamilyMember"] = "0",
            ["EmployeeSubtypeId"] = employeeSubtypeId?.ToString(),
            ["FamilySubtypeId"] = null,
            ["HasInvitationWpFk"] = "0",
            ["InvitationAndWorkPermitRequired"] = null,
            ["HasWizaWpFk"] = "0",
            ["WizaAndWorkPermitRequired"] = null,
            ["ChangeInformation"] = null,
        };

    [Fact]
    public void Empty_ReportsNoCancellations()
    {
        var index = Visa2014LegacyDocumentCancellationIndex.Empty;
        Assert.False(index.IsVisaCancelled(VisaOid));
        Assert.False(index.IsWorkPermitCancelled(WorkPermitOid));
    }

    [Fact]
    public void FromWorkPermitOidsForTests_MarksOnlyWorkPermits()
    {
        var index = Visa2014LegacyDocumentCancellationIndex.FromWorkPermitOidsForTests([WorkPermitOid]);
        Assert.True(index.IsWorkPermitCancelled(WorkPermitOid));
        Assert.False(index.IsVisaCancelled(VisaOid));
    }

    [Fact]
    public void FromVisaOidsForTests_MarksOnlyVisas()
    {
        var index = Visa2014LegacyDocumentCancellationIndex.FromVisaOidsForTests([VisaOid]);
        Assert.True(index.IsVisaCancelled(VisaOid));
        Assert.False(index.IsWorkPermitCancelled(WorkPermitOid));
    }

    [Fact]
    public void ApplyEvidenceRow_CompletedCancelWorkPermitSubtype_IndexesWorkPermit()
    {
        var index = new Visa2014LegacyDocumentCancellationIndex();
        index.ApplyEvidenceRow(
            EvidenceRow(
                workPermitOid: WorkPermitOid.ToString("D"),
                isComplete: true,
                employeeSubtypeId: Visa2014ApplicationItemCancelledFlagsMapper.LegacySubtypeCancelWorkPermit),
            catalogs: new Dictionary<string, Visa2014LookupCatalog>(),
            Visibility);

        Assert.True(index.IsWorkPermitCancelled(WorkPermitOid));
        Assert.False(index.IsVisaCancelled(VisaOid));
    }

    [Fact]
    public void ApplyEvidenceRow_CompletedCancelVisaSubtype_IndexesVisa()
    {
        var index = new Visa2014LegacyDocumentCancellationIndex();
        index.ApplyEvidenceRow(
            EvidenceRow(
                visaOid: VisaOid.ToString("D"),
                isComplete: true,
                employeeSubtypeId: Visa2014ApplicationItemCancelledFlagsMapper.LegacySubtypeCancelVisa),
            catalogs: new Dictionary<string, Visa2014LookupCatalog>(),
            Visibility);

        Assert.True(index.IsVisaCancelled(VisaOid));
        Assert.False(index.IsWorkPermitCancelled(WorkPermitOid));
    }

    [Fact]
    public void ApplyEvidenceRow_CompletedCancelBothSubtype_IndexesBothDocuments()
    {
        var index = new Visa2014LegacyDocumentCancellationIndex();
        index.ApplyEvidenceRow(
            EvidenceRow(
                visaOid: VisaOid.ToString("D"),
                workPermitOid: WorkPermitOid.ToString("D"),
                isComplete: true,
                employeeSubtypeId: Visa2014ApplicationItemCancelledFlagsMapper.LegacySubtypeCancelVisaAndWorkPermit),
            catalogs: new Dictionary<string, Visa2014LookupCatalog>(),
            Visibility);

        Assert.True(index.IsVisaCancelled(VisaOid));
        Assert.True(index.IsWorkPermitCancelled(WorkPermitOid));
    }

    [Fact]
    public void ApplyEvidenceRow_NonCancelCompleteSubtype_DoesNotIndex()
    {
        var index = new Visa2014LegacyDocumentCancellationIndex();
        index.ApplyEvidenceRow(
            EvidenceRow(
                visaOid: VisaOid.ToString("D"),
                workPermitOid: WorkPermitOid.ToString("D"),
                isComplete: true,
                employeeSubtypeId: 1),
            catalogs: new Dictionary<string, Visa2014LookupCatalog>(),
            Visibility);

        Assert.False(index.IsVisaCancelled(VisaOid));
        Assert.False(index.IsWorkPermitCancelled(WorkPermitOid));
    }

    [Fact]
    public void ApplyEvidenceRow_CancelledWithoutMappedType_DefaultsWorkPermitFlag()
    {
        // ResolveDocumentCancellation with unknown type defaults work-permit flag when no heuristics hit.
        var index = new Visa2014LegacyDocumentCancellationIndex();
        index.ApplyEvidenceRow(
            EvidenceRow(
                workPermitOid: WorkPermitOid.ToString("D"),
                cancelled: true),
            catalogs: new Dictionary<string, Visa2014LookupCatalog>(),
            Visibility);

        Assert.True(index.IsWorkPermitCancelled(WorkPermitOid));
        Assert.False(index.IsVisaCancelled(VisaOid));
    }
}
