using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ExcelReports;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateConvert;

public class EphemeralTemplateValidationServiceTests
{
    private readonly RecordingValidator _validator = new();
    private readonly IApplicationProfilePlaceholderSetService _sets =
        new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService());

    private readonly IEphemeralTemplateValidationService _service;

    public EphemeralTemplateValidationServiceTests()
    {
        _service = new EphemeralTemplateValidationService(
            new UserReportPlaceholderExtractor(),
            new ExcelTemplatePlaceholderExtractor(),
            _validator,
            _validator);
    }

    private static ApplicationProfile Profile(bool passport = true) =>
        new()
        {
            RequirePersonPassport = passport,
            RequirePersonVisa = true,
            RequirePersonEducation = true,
            RequirePersonAddressOfResidence = true,
            RequirePersonPosition = true,
            RequirePersonSalary = true,
            RequirePersonMedical = true,
            RequirePersonInvitationItem = true,
            RequirePersonWorkPermitItem = true,
            RequirePersonBorderZoneItem = true,
            RequirePersonRejectionItem = true,
            RequirePersonTravelHistory = true,
        };

    private ApplicationProfilePlaceholderSet Set(
        ApplicationProfile? profile = null,
        ApplicationProfileTemplateDataScope scope = ApplicationProfileTemplateDataScope.Both,
        ApplicationProfileTemplateKind kind = ApplicationProfileTemplateKind.Word) =>
        _sets.GetSet(new ApplicationProfilePlaceholderSetQuery
        {
            Profile = profile ?? Profile(),
            DataScope = scope,
            TemplateKind = kind,
        });

    private Task<TemplateValidationReport> Validate(
        byte[] content,
        ApplicationProfilePlaceholderSet set,
        TemplateSourceFormat format = TemplateSourceFormat.Docx) =>
        _service.ExtractAndValidateAsync(content, format, set);

    private static byte[] Letter(params string[] paragraphs) =>
        TemplateConvertFixtures.CreateWordDocument(paragraphs.Select(p => new[] { p }).ToArray());

    private static TemplateValidationIssue Issue(TemplateValidationReport report, TemplateValidationIssueCode code) =>
        Assert.Single(report.Issues.Where(i => i.Code == code));

    [Fact]
    public async Task Empty_content_fails_before_any_extraction()
    {
        var report = await Validate([], Set());

        Assert.True(report.HasHardFailure);
        Assert.Equal(TemplateValidationIssueCode.UnreadableDocument, Assert.Single(report.Issues).Code);
        Assert.Empty(report.Tokens);
    }

    [Fact]
    public async Task Bytes_that_are_not_a_package_fail_as_unreadable()
    {
        var report = await Validate([0x01, 0x02, 0x03, 0x04], Set());

        Assert.True(report.HasHardFailure);
        Assert.Equal(TemplateValidationIssueCode.UnreadableDocument, Assert.Single(report.Issues).Code);
    }

    [Fact]
    public async Task A_document_with_no_tokens_is_a_hard_failure()
    {
        var report = await Validate(Letter("Hormatly ministr,", "Arza hödürleýäris."), Set());

        Assert.True(report.HasHardFailure);
        Assert.Equal(TemplateValidationIssueCode.NoTokensFound, Assert.Single(report.Issues).Code);
    }

    [Fact]
    public async Task Allowed_tokens_that_resolve_produce_no_issues()
    {
        var report = await Validate(Letter("Arza {{ds.AFNUM}}", "{{ds.ACNAM}} tarapyndan"), Set());

        Assert.False(report.HasHardFailure);
        Assert.False(report.HasWarnings);
        Assert.Empty(report.Issues);
        Assert.Equal(["ds.ACNAM", "ds.AFNUM"], report.Tokens);
        Assert.Equal(2, report.Results.Count);
    }

    [Fact]
    public async Task A_token_outside_the_catalog_is_unknown()
    {
        var report = await Validate(Letter("Arza {{ds.NOPE}}"), Set());

        Assert.True(report.HasHardFailure);
        Assert.Equal("ds.NOPE", Issue(report, TemplateValidationIssueCode.UnknownToken).Token);
    }

    [Fact]
    public async Task A_pack_the_profile_does_not_collect_warns_instead_of_blocking()
    {
        var report = await Validate(Letter("Pasport {{.PPN}}"), Set(Profile(passport: false)));

        Assert.False(report.HasHardFailure);
        Assert.True(report.HasWarnings);

        var issue = Issue(report, TemplateValidationIssueCode.PackDisabledToken);
        Assert.Equal(TemplateValidationSeverity.Warning, issue.Severity);

        // Still bound: it resolves on the merge root, it just merges empty.
        Assert.Contains(".PPN", _validator.WordTokens);
    }

    [Fact]
    public async Task A_row_token_in_a_header_only_template_is_out_of_scope()
    {
        var report = await Validate(
            Letter("{{ds.AFNUM}} — {{.PFN}}"),
            Set(scope: ApplicationProfileTemplateDataScope.ApplicationHeader));

        Assert.True(report.HasHardFailure);
        Assert.Equal(".PFN", Issue(report, TemplateValidationIssueCode.OutOfDataScopeToken).Token);
    }

    [Fact]
    public async Task An_image_token_in_an_excel_template_is_rejected()
    {
        var content = TemplateConvertFixtures.CreateExcelSheet("Sanaw", ("A1", "{{IMAGE:PPH}}"));

        var report = await Validate(
            content,
            Set(kind: ApplicationProfileTemplateKind.Excel),
            TemplateSourceFormat.Xlsx);

        Assert.True(report.HasHardFailure);
        Assert.Equal("IMAGE:PPH", Issue(report, TemplateValidationIssueCode.UnsupportedImageToken).Token);
    }

    [Fact]
    public async Task A_balanced_loop_passes_and_is_not_sent_to_the_property_validator()
    {
        var content = TemplateConvertFixtures.CreateExcelSheet(
            "Sanaw",
            ("A1", "{{ds.AFNUM}}"),
            ("A2", "{{#ds.rows}}"),
            ("B2", "{{.PFN}}"),
            ("A3", "{{/ds.rows}}"));

        var report = await Validate(
            content,
            Set(kind: ApplicationProfileTemplateKind.Excel),
            TemplateSourceFormat.Xlsx);

        Assert.False(report.HasHardFailure);
        Assert.Empty(report.Issues);
        Assert.Equal(["#ds.rows", ".PFN", "/ds.rows", "ds.AFNUM"], report.Tokens);
        Assert.Equal([".PFN", "ds.AFNUM"], _validator.ExcelTokens);
    }

    [Fact]
    public async Task A_loop_that_is_never_closed_is_a_broken_loop()
    {
        var report = await Validate(Letter("{{#ds.rows}}", "{{.PFN}}"), Set());

        Assert.True(report.HasHardFailure);
        Assert.Equal("#ds.rows", Issue(report, TemplateValidationIssueCode.BrokenLoop).Token);
    }

    [Fact]
    public async Task A_close_marker_without_an_open_is_a_broken_loop()
    {
        var report = await Validate(Letter("{{.PFN}}", "{{/ds.rows}}"), Set());

        Assert.True(report.HasHardFailure);
        Assert.Equal("/ds.rows", Issue(report, TemplateValidationIssueCode.BrokenLoop).Token);
    }

    [Fact]
    public async Task A_token_that_does_not_resolve_on_the_merge_root_blocks_approve()
    {
        _validator.Invalid.Add("ds.AFNUM");

        var report = await Validate(Letter("Arza {{ds.AFNUM}}"), Set());

        Assert.True(report.HasHardFailure);
        var issue = Issue(report, TemplateValidationIssueCode.UnresolvedOnBoType);
        Assert.Equal("ds.AFNUM", issue.Token);
        Assert.Contains("Property not found", issue.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task People_scope_validates_against_the_roster_merge_line()
    {
        await Validate(Letter("{{.PFN}}"), Set(scope: ApplicationProfileTemplateDataScope.PeopleM2M));

        Assert.Equal(UserReportBoType.ApplicationItem, _validator.WordBoType);
    }

    [Fact]
    public async Task Header_scope_validates_against_the_instance()
    {
        await Validate(Letter("{{ds.AFNUM}}"), Set(scope: ApplicationProfileTemplateDataScope.ApplicationHeader));

        Assert.Equal(UserReportBoType.ApplicationProfileInstance, _validator.WordBoType);
    }

    [Fact]
    public async Task Excel_templates_are_validated_as_header_plus_rows()
    {
        var content = TemplateConvertFixtures.CreateExcelSheet("Sanaw", ("A1", "{{ds.AFNUM}}"));

        await Validate(content, Set(kind: ApplicationProfileTemplateKind.Excel), TemplateSourceFormat.Xlsx);

        Assert.Equal(ExcelMergeMode.ItemList, _validator.MergeMode);
        Assert.Empty(_validator.WordTokens);
    }

    private sealed class RecordingValidator : IUserReportValidationService, IExcelReportValidationService
    {
        public List<string> WordTokens { get; } = [];

        public List<string> ExcelTokens { get; } = [];

        public UserReportBoType? WordBoType { get; private set; }

        public UserReportBoType? ExcelBoType { get; private set; }

        public ExcelMergeMode? MergeMode { get; private set; }

        public HashSet<string> Invalid { get; } = new(StringComparer.Ordinal);

        public Task<IList<PlaceholderValidationResult>> ValidatePlaceholdersAsync(
            IList<string> placeholders,
            UserReportBoType boType)
        {
            WordBoType = boType;
            WordTokens.AddRange(placeholders);
            return Task.FromResult(Build(placeholders));
        }

        public Task<IList<PlaceholderValidationResult>> ValidatePlaceholdersAsync(
            IList<string> placeholders,
            UserReportBoType boType,
            ExcelMergeMode mergeMode)
        {
            ExcelBoType = boType;
            MergeMode = mergeMode;
            ExcelTokens.AddRange(placeholders);
            return Task.FromResult(Build(placeholders));
        }

        private IList<PlaceholderValidationResult> Build(IList<string> placeholders) =>
            placeholders
                .Select(p => new PlaceholderValidationResult
                {
                    PlaceholderKey = p,
                    IsValid = !Invalid.Contains(p),
                    ErrorMessage = Invalid.Contains(p) ? "Property not found" : string.Empty,
                })
                .ToList();
    }
}
