using Visa2026.DataImporter.Legacy.Visa2014;
using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014.Tests;

/// <summary>
/// ApplicationProgress raw parse + TransformRows skip partitions (not covered by SynthesizeSteps facts).
/// </summary>
public class Visa2014ApplicationProgressTransformParseAndSkipTests
{
    [Fact]
    public void TryParseRawRow_MissingOrInvalidOid_ReturnsFalse()
    {
        Assert.False(Visa2014ApplicationProgressTransform.TryParseRawRow(
            new Dictionary<string, string?>(StringComparer.Ordinal),
            out _));

        Assert.False(Visa2014ApplicationProgressTransform.TryParseRawRow(
            new Dictionary<string, string?>(StringComparer.Ordinal) { ["Oid"] = "not-a-guid" },
            out _));
    }

    [Fact]
    public void TryParseRawRow_MapsFlagsDatesAndNumbers()
    {
        var oid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var ok = Visa2014ApplicationProgressTransform.TryParseRawRow(
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["Oid"] = oid.ToString("D"),
                ["ManualApplicationNumber"] = "3717",
                ["ManualApplicationDate"] = "2014-06-10",
                ["IsLongProcess"] = "1",
                ["ForEmployee"] = "1",
                ["ForFamilyMember"] = "0",
                ["EmployeeSubtypeId"] = "1",
                ["FamilySubtypeId"] = null,
                ["HasInvitationWpFk"] = "0",
                ["InvitationAndWorkPermitRequired"] = null,
                ["HasWizaWpFk"] = "0",
                ["WizaAndWorkPermitRequired"] = null,
                ["ChangeInformation"] = null,
                ["DateForwardedToMonistery"] = "2014-06-10",
                ["MinisteriesDocumentDate"] = "2014-06-20",
                ["MinisteriesDocumentNumber"] = "Z/11078",
                ["DateForwardedToMinConstruction"] = "2014-06-23",
                ["DocNumberForwardedToMinConstruction"] = null,
                ["ProcessDate"] = "2014-06-27",
                ["ProcessNumber"] = "AS455977",
                ["Cancelled"] = "0",
                ["Rejected"] = "1",
            },
            out var parsed);

        Assert.True(ok);
        Assert.Equal(oid, parsed.LegacyApplicationOid);
        Assert.Equal("3717", parsed.ManualApplicationNumber);
        Assert.Equal(new DateTime(2014, 6, 10), parsed.ManualApplicationDate);
        Assert.True(parsed.IsLongProcess);
        Assert.True(parsed.ForEmployee);
        Assert.False(parsed.ForFamilyMember);
        Assert.Equal(1, parsed.EmployeeSubtypeId);
        Assert.Equal(new DateTime(2014, 6, 27), parsed.ProcessDate);
        Assert.Equal("AS455977", parsed.ProcessNumber);
        Assert.False(parsed.Cancelled);
        Assert.True(parsed.Rejected);
    }

    [Fact]
    public void TransformRows_SkippedApplicationTypeComposite_E33()
    {
        var raw = new Visa2014ApplicationProgressRawRow(
            LegacyApplicationOid: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ManualApplicationNumber: "1",
            ManualApplicationDate: new DateTime(2015, 1, 1),
            IsLongProcess: false,
            ForEmployee: true,
            ForFamilyMember: false,
            EmployeeSubtypeId: 33,
            FamilySubtypeId: null,
            HasInvitationWpFk: false,
            InvitationAndWorkPermitRequired: null,
            HasWizaWpFk: false,
            WizaAndWorkPermitRequired: null,
            ChangeInformation: null,
            DateForwardedToMonistery: null,
            MinisteriesDocumentDate: null,
            MinisteriesDocumentNumber: null,
            DateForwardedToMinConstruction: null,
            DocNumberForwardedToMinConstruction: null,
            ProcessDate: new DateTime(2015, 2, 1),
            ProcessNumber: "P-1",
            Cancelled: false,
            Rejected: false);

        var batch = Visa2014ApplicationProgressTransform.TransformRows(
            [raw],
            ministryLegCountByLegacyApplicationOid: null,
            completionByLegacyApplicationOid: null,
            rejectionByLegacyApplicationOid: null,
            out var skipped,
            out _);

        Assert.Empty(batch.ImportRows);
        Assert.Single(skipped);
        Assert.Equal("parent_application_type_skipped", skipped[0]["_reason"]);
        Assert.Equal("E:33:na:na:na", skipped[0]["_legacy_ApplicationTypeComposite"]);
    }

    [Fact]
    public void TransformRows_MissingApplicationDate_RequiredNull()
    {
        var raw = new Visa2014ApplicationProgressRawRow(
            LegacyApplicationOid: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ManualApplicationNumber: "2",
            ManualApplicationDate: null,
            IsLongProcess: false,
            ForEmployee: true,
            ForFamilyMember: false,
            EmployeeSubtypeId: 1,
            FamilySubtypeId: null,
            HasInvitationWpFk: false,
            InvitationAndWorkPermitRequired: null,
            HasWizaWpFk: false,
            WizaAndWorkPermitRequired: null,
            ChangeInformation: null,
            DateForwardedToMonistery: null,
            MinisteriesDocumentDate: null,
            MinisteriesDocumentNumber: null,
            DateForwardedToMinConstruction: null,
            DocNumberForwardedToMinConstruction: null,
            ProcessDate: null,
            ProcessNumber: null,
            Cancelled: false,
            Rejected: false);

        var batch = Visa2014ApplicationProgressTransform.TransformRows(
            [raw],
            null,
            null,
            null,
            out var skipped,
            out _);

        Assert.Empty(batch.ImportRows);
        Assert.Single(skipped);
        Assert.Equal("required_null:ApplicationDate", skipped[0]["_reason"]);
    }

    [Fact]
    public void TransformRows_NoEvidenceDirectMigration_NoSynthesizedSteps()
    {
        // Direct migration with an application date but no process/completion/cancel/reject → empty timeline.
        var raw = new Visa2014ApplicationProgressRawRow(
            LegacyApplicationOid: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            ManualApplicationNumber: "3",
            ManualApplicationDate: new DateTime(2016, 3, 1),
            IsLongProcess: false,
            ForEmployee: true,
            ForFamilyMember: false,
            EmployeeSubtypeId: 1,
            FamilySubtypeId: null,
            HasInvitationWpFk: false,
            InvitationAndWorkPermitRequired: null,
            HasWizaWpFk: false,
            WizaAndWorkPermitRequired: null,
            ChangeInformation: null,
            DateForwardedToMonistery: null,
            MinisteriesDocumentDate: null,
            MinisteriesDocumentNumber: null,
            DateForwardedToMinConstruction: null,
            DocNumberForwardedToMinConstruction: null,
            ProcessDate: null,
            ProcessNumber: null,
            Cancelled: false,
            Rejected: false);

        var batch = Visa2014ApplicationProgressTransform.TransformRows(
            [raw],
            null,
            null,
            null,
            out var skipped,
            out _);

        Assert.Empty(batch.ImportRows);
        Assert.Single(skipped);
        Assert.Equal("no_synthesized_steps", skipped[0]["_reason"]);
    }

    [Fact]
    public void TransformRows_HappyPath_EmitsOrderedImportRows()
    {
        var oid = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var raw = new Visa2014ApplicationProgressRawRow(
            LegacyApplicationOid: oid,
            ManualApplicationNumber: "4",
            ManualApplicationDate: new DateTime(2014, 6, 1),
            IsLongProcess: false,
            ForEmployee: true,
            ForFamilyMember: false,
            EmployeeSubtypeId: 1,
            FamilySubtypeId: null,
            HasInvitationWpFk: false,
            InvitationAndWorkPermitRequired: null,
            HasWizaWpFk: false,
            WizaAndWorkPermitRequired: null,
            ChangeInformation: null,
            DateForwardedToMonistery: null,
            MinisteriesDocumentDate: null,
            MinisteriesDocumentNumber: null,
            DateForwardedToMinConstruction: null,
            DocNumberForwardedToMinConstruction: null,
            ProcessDate: new DateTime(2014, 6, 15),
            ProcessNumber: "P-4",
            Cancelled: false,
            Rejected: false);

        var batch = Visa2014ApplicationProgressTransform.TransformRows(
            [raw],
            null,
            null,
            null,
            out var skipped,
            out var dedupe);

        Assert.Empty(skipped);
        Assert.Single(dedupe);
        Assert.Equal(2, batch.ImportRows.Count);
        Assert.Equal("PROCESS_STARTED", batch.ImportRows[0]["State"]);
        Assert.Equal("PROCESS_ISSUED", batch.ImportRows[1]["State"]);
        Assert.Equal(1, batch.ImportRows[0]["Order"]);
        Assert.Equal(2, batch.ImportRows[1]["Order"]);
        Assert.Equal(oid.ToString("D"), batch.ImportRows[0]["Application"]);
    }
}
