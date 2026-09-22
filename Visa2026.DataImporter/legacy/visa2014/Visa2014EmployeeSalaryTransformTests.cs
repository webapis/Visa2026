using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014EmployeeSalaryTransformTests
{
    private static readonly Guid PersonOid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid SalaryOid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static Visa2014EmployeeSalaryRawRow Raw(
        Guid? salaryOid = null,
        string? detail = "1500",
        DateTime? start = null,
        Guid? personOid = null) =>
        new(
            LegacyPersonOid: personOid ?? PersonOid,
            LegacySalaryOid: salaryOid,
            SalaryDetail: detail,
            CurrentPositionStart: start ?? new DateTime(2023, 5, 1));

    [Fact]
    public void TryParseRawRow_ValidRow_ParsesOptionalSalaryAndStart()
    {
        var row = new Dictionary<string, string?>
        {
            ["LegacyPersonOid"] = PersonOid.ToString("D"),
            ["LegacySalaryOid"] = SalaryOid.ToString("D"),
            ["SalaryDetail"] = "2.500,00 USD",
            ["CurrentPositionStart"] = "2022-04-15",
        };

        Assert.True(Visa2014EmployeeSalaryTransform.TryParseRawRow(row, out var parsed));
        Assert.Equal(PersonOid, parsed.LegacyPersonOid);
        Assert.Equal(SalaryOid, parsed.LegacySalaryOid);
        Assert.Equal("2.500,00 USD", parsed.SalaryDetail);
        Assert.Equal(new DateTime(2022, 4, 15), parsed.CurrentPositionStart);
    }

    [Fact]
    public void TryParseRawRow_MissingPerson_ReturnsFalse()
    {
        Assert.False(Visa2014EmployeeSalaryTransform.TryParseRawRow(
            new Dictionary<string, string?> { ["SalaryDetail"] = "100" },
            out _));
    }

    [Fact]
    public void BuildExportRow_MissingSalaryFk_Skips()
    {
        var raw = Raw(salaryOid: null, detail: null);
        var audit = Visa2014EmployeeSalaryTransform.BuildAmountParseRow(raw);
        var export = Visa2014EmployeeSalaryTransform.BuildExportRow(raw, audit, out var skipReason);

        Assert.Equal("missing_salary_fk", skipReason);
        Assert.Null(export["Amount"]);
        Assert.Equal("Employee", export["_legacyTable"]);
    }

    [Fact]
    public void BuildExportRow_EmptySalaryDetail_Skips()
    {
        var raw = Raw(salaryOid: SalaryOid, detail: "  ");
        var audit = Visa2014EmployeeSalaryTransform.BuildAmountParseRow(raw);
        var export = Visa2014EmployeeSalaryTransform.BuildExportRow(raw, audit, out var skipReason);

        Assert.Equal("empty_salary_detail", skipReason);
        Assert.Null(export["Amount"]);
    }

    [Fact]
    public void BuildExportRow_UnparseableAmount_Skips()
    {
        var raw = Raw(salaryOid: SalaryOid, detail: "not-an-amount");
        var audit = Visa2014EmployeeSalaryTransform.BuildAmountParseRow(raw);
        var export = Visa2014EmployeeSalaryTransform.BuildExportRow(raw, audit, out var skipReason);

        Assert.Equal("unparseable_amount", skipReason);
        Assert.Equal("no_amount_token", audit["_parseNote"]);
        Assert.Null(export["Amount"]);
    }

    [Fact]
    public void BuildExportRow_MissingStartDate_SkipsAfterAmountParsed()
    {
        var raw = Raw(salaryOid: SalaryOid, detail: "1200", start: null) with
        {
            CurrentPositionStart = null,
        };
        var audit = Visa2014EmployeeSalaryTransform.BuildAmountParseRow(raw);
        var export = Visa2014EmployeeSalaryTransform.BuildExportRow(raw, audit, out var skipReason);

        Assert.Equal("required_null:StartDate", skipReason);
        Assert.Equal("1200", export["Amount"]);
        Assert.Equal("USD", export["Currency"]);
        Assert.Null(export["StartDate"]);
    }

    [Fact]
    public void TransformRows_ImportsParsableAmount_AndAuditsSkipped()
    {
        var batch = Visa2014EmployeeSalaryTransform.TransformRows(
            [
                Raw(salaryOid: SalaryOid, detail: "1.250,50"),
                Raw(
                    salaryOid: null,
                    detail: null,
                    personOid: Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")),
            ],
            out var skipped,
            out var amountParseRows);

        Assert.Single(batch.ImportRows);
        Assert.Equal("1.250.50", batch.ImportRows[0]["Amount"]);
        Assert.Equal("USD", batch.ImportRows[0]["Currency"]);
        Assert.Equal("2023-05-01", batch.ImportRows[0]["StartDate"]);
        Assert.Null(batch.ImportRows[0]["EndDate"]);

        Assert.Single(skipped);
        Assert.Equal("missing_salary_fk", skipped[0]["_skipReason"]);
        Assert.Equal(2, amountParseRows.Count);
        Assert.Equal(2, batch.LegacyRowCount);
    }
}
