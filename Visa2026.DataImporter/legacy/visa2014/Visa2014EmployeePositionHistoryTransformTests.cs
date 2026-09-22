using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014EmployeePositionHistoryTransformTests
{
    private static readonly Guid PersonOid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OlderOid = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid NewerOid = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static IReadOnlyDictionary<string, Visa2014LookupCatalog> Catalogs() =>
        new Dictionary<string, Visa2014LookupCatalog>(StringComparer.OrdinalIgnoreCase)
        {
            ["Position"] = new Visa2014LookupCatalog
            {
                TargetCatalog = "Position",
                TargetMatchProperty = "Name",
                UnmappedPolicy = "skip_row",
                LegacyToTarget = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Engineer"] = "Engineer",
                    ["Manager"] = "Manager",
                },
            },
            ["Department"] = new Visa2014LookupCatalog
            {
                TargetCatalog = "Department",
                TargetMatchProperty = "Name",
                UnmappedPolicy = "skip_row",
                LegacyToTarget = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["IT"] = "IT",
                },
            },
        };

    private static Visa2014EmployeePositionHistoryRawRow Raw(
        Guid oid,
        DateTime? start,
        string? title = "Engineer",
        string? department = "IT",
        string? positionCode = "617-",
        string? middleName = null) =>
        new(
            LegacyOid: oid,
            LegacyPersonOid: PersonOid,
            TitleOfPosition: title,
            PositionCode: positionCode,
            TitleOfDepartment: department,
            StartDateOnThisPosition: start,
            PersonMiddleName: middleName);

    [Fact]
    public void TryParseRawRow_ValidRow_ParsesCoreFields()
    {
        var row = new Dictionary<string, string?>
        {
            ["Oid"] = OlderOid.ToString("D"),
            ["LegacyPersonOid"] = PersonOid.ToString("D"),
            ["TitleOfPosition"] = "Engineer",
            ["PositionCode"] = "123",
            ["TitleOfDepartment"] = "IT",
            ["StartDateOnThisPosition"] = "2020-01-10",
            ["PersonMiddleName"] = "Lead Engineer",
        };

        Assert.True(Visa2014EmployeePositionHistoryTransform.TryParseRawRow(row, out var parsed));
        Assert.Equal(OlderOid, parsed.LegacyOid);
        Assert.Equal(PersonOid, parsed.LegacyPersonOid);
        Assert.Equal("Engineer", parsed.TitleOfPosition);
        Assert.Equal("Lead Engineer", parsed.PersonMiddleName);
        Assert.Equal(new DateTime(2020, 1, 10), parsed.StartDateOnThisPosition);
    }

    [Fact]
    public void TransformRows_DerivesEndDateForPriorPosition_AndNullForCurrent()
    {
        var batch = Visa2014EmployeePositionHistoryTransform.TransformRows(
            [
                Raw(OlderOid, new DateTime(2019, 1, 1), title: "Engineer"),
                Raw(NewerOid, new DateTime(2021, 6, 1), title: "Manager"),
            ],
            Catalogs(),
            forPermitSupplement: false,
            out var skipped,
            out _,
            out _);

        Assert.Empty(skipped);
        Assert.Equal(2, batch.ImportRows.Count);

        var older = batch.ImportRows.Single(r => Equals(r["_legacyRowId"], OlderOid));
        var newer = batch.ImportRows.Single(r => Equals(r["_legacyRowId"], NewerOid));

        Assert.Equal("2019-01-01", older["StartDate"]);
        Assert.Equal("2021-06-01", older["EndDate"]);
        Assert.Equal("2021-06-01", newer["StartDate"]);
        Assert.Null(newer["EndDate"]);
        Assert.Equal("WorkHistoryOfEmployee", older["_legacyTable"]);
    }

    [Fact]
    public void TransformRows_CurrentRowUsesMiddleNameAsActualPosition_OlderUsesCodeNormalized()
    {
        var batch = Visa2014EmployeePositionHistoryTransform.TransformRows(
            [
                Raw(OlderOid, new DateTime(2019, 1, 1), positionCode: "617-", middleName: "Ignored Older"),
                Raw(NewerOid, new DateTime(2021, 6, 1), positionCode: "999", middleName: "Site Lead"),
            ],
            Catalogs(),
            forPermitSupplement: false,
            out _,
            out _,
            out _);

        var older = batch.ImportRows.Single(r => Equals(r["_legacyRowId"], OlderOid));
        var newer = batch.ImportRows.Single(r => Equals(r["_legacyRowId"], NewerOid));

        Assert.Equal("-", older["ActualPosition"]);
        Assert.Equal("Site Lead", newer["ActualPosition"]);
    }

    [Fact]
    public void TransformRows_PermitSupplement_NeverAppliesMiddleName_AndClearsEndDate()
    {
        var batch = Visa2014EmployeePositionHistoryTransform.TransformRows(
            [
                Raw(NewerOid, new DateTime(2021, 6, 1), positionCode: "999", middleName: "Site Lead"),
            ],
            Catalogs(),
            forPermitSupplement: true,
            out _,
            out _,
            out _);

        Assert.Single(batch.ImportRows);
        Assert.Equal("WorkHistoryOfEmployee(permit-supplement)", batch.ImportRows[0]["_legacyTable"]);
        Assert.Equal("-", batch.ImportRows[0]["ActualPosition"]);
        Assert.Null(batch.ImportRows[0]["EndDate"]);
    }

    [Fact]
    public void TransformRows_MissingStartDate_Skips()
    {
        var batch = Visa2014EmployeePositionHistoryTransform.TransformRows(
            [Raw(OlderOid, start: null)],
            Catalogs(),
            forPermitSupplement: false,
            out var skipped,
            out _,
            out _);

        Assert.Empty(batch.ImportRows);
        Assert.Single(skipped);
        Assert.Equal("required_null:StartDate", skipped[0]["_skipReason"]);
    }

    [Fact]
    public void TransformRows_UnmappedPositionWithSkipPolicy_ImportsNullPosition()
    {
        // skip_row unmapped → TryTranslate succeeds with null target; row is not skipped.
        var batch = Visa2014EmployeePositionHistoryTransform.TransformRows(
            [Raw(OlderOid, new DateTime(2020, 1, 1), title: "UnknownRole")],
            Catalogs(),
            forPermitSupplement: false,
            out var skipped,
            out var unmappedDistinct,
            out _);

        Assert.Empty(skipped);
        Assert.Single(batch.ImportRows);
        Assert.Null(batch.ImportRows[0]["Position"]);
        Assert.NotEmpty(unmappedDistinct);
    }

    [Fact]
    public void TransformRows_BlankPosition_SkipsAsRequiredNull()
    {
        var batch = Visa2014EmployeePositionHistoryTransform.TransformRows(
            [Raw(OlderOid, new DateTime(2020, 1, 1), title: "  ")],
            Catalogs(),
            forPermitSupplement: false,
            out var skipped,
            out _,
            out _);

        Assert.Empty(batch.ImportRows);
        Assert.Single(skipped);
        Assert.Equal("required_null:Position", skipped[0]["_skipReason"]);
    }
}
