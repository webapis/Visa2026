using System;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.OfficerShell;
using Visa2026.Module.Services.WordReports;

namespace Visa2026.Blazor.Server.Services;

/// <summary>Generates a Seretmezlik document on demand and serves it as PDF for <c>#visa-preview-slot</c>.</summary>
public abstract class ApplicationProfileInstanceExclusionDocumentPreviewSourceBase : IFilePreviewSource
{
    private readonly INonSecuredObjectSpaceFactory _objectSpaces;
    private readonly ApplicationWordReportOfficePreviewPdfConverter _converter;

    protected ApplicationProfileInstanceExclusionDocumentPreviewSourceBase(
        INonSecuredObjectSpaceFactory objectSpaces,
        ApplicationWordReportOfficePreviewPdfConverter converter)
    {
        _objectSpaces = objectSpaces;
        _converter = converter;
    }

    public abstract string SourceType { get; }

    protected abstract ApplicationProfileInstanceExclusionTemplateKind Kind { get; }

    public Task<FilePreviewResult?> TryLoadAsync(Guid exclusionId)
    {
        if (exclusionId == Guid.Empty)
            return Task.FromResult<FilePreviewResult?>(null);

        using var objectSpace = _objectSpaces.CreateNonSecuredObjectSpace<ApplicationProfileInstanceExclusion>();
        var exclusion = ApplicationProfileInstanceExclusionTemplateStore.LoadExclusion(objectSpace, exclusionId);
        if (exclusion == null)
            return Task.FromResult<FilePreviewResult?>(null);

        var document = ApplicationProfileInstanceExclusionLetterBuilder.Generate(objectSpace, exclusion, Kind);
        return Task.FromResult(OfficeFilePreviewResultFactory.FromOfficeOrPdf(_converter, document.Content, document.FileName));
    }
}

public sealed class ApplicationProfileInstanceExclusionLetterPreviewSource : ApplicationProfileInstanceExclusionDocumentPreviewSourceBase
{
    public const string Key = "seretmezlik-letter";

    public ApplicationProfileInstanceExclusionLetterPreviewSource(
        INonSecuredObjectSpaceFactory objectSpaces,
        ApplicationWordReportOfficePreviewPdfConverter converter)
        : base(objectSpaces, converter)
    {
    }

    public override string SourceType => Key;

    protected override ApplicationProfileInstanceExclusionTemplateKind Kind => ApplicationProfileInstanceExclusionTemplateKind.Letter;
}

public sealed class ApplicationProfileInstanceExclusionRosterPreviewSource : ApplicationProfileInstanceExclusionDocumentPreviewSourceBase
{
    public const string Key = "seretmezlik-roster";

    public ApplicationProfileInstanceExclusionRosterPreviewSource(
        INonSecuredObjectSpaceFactory objectSpaces,
        ApplicationWordReportOfficePreviewPdfConverter converter)
        : base(objectSpaces, converter)
    {
    }

    public override string SourceType => Key;

    protected override ApplicationProfileInstanceExclusionTemplateKind Kind => ApplicationProfileInstanceExclusionTemplateKind.Roster;
}

/// <summary>Current company Seretmezlik template (uploaded or built-in) merged with sample data.</summary>
public sealed class ApplicationProfileInstanceExclusionTemplatePreviewSource : IFilePreviewSource
{
    public const string Key = "seretmezlik-template";

    private readonly INonSecuredObjectSpaceFactory _objectSpaces;
    private readonly ApplicationWordReportOfficePreviewPdfConverter _converter;

    public ApplicationProfileInstanceExclusionTemplatePreviewSource(
        INonSecuredObjectSpaceFactory objectSpaces,
        ApplicationWordReportOfficePreviewPdfConverter converter)
    {
        _objectSpaces = objectSpaces;
        _converter = converter;
    }

    public string SourceType => Key;

    public Task<FilePreviewResult?> TryLoadAsync(Guid objectId)
    {
        if (ApplicationProfileInstanceExclusionTemplateStore.KindFromPreviewId(objectId) is not { } kind)
            return Task.FromResult<FilePreviewResult?>(null);

        using var objectSpace = _objectSpaces.CreateNonSecuredObjectSpace<ApplicationProfileInstanceExclusionTemplate>();
        var template = ApplicationProfileInstanceExclusionLetterBuilder.GetTemplate(objectSpace, kind);
        var sample = ApplicationProfileInstanceExclusionLetterBuilder.BuildSampleMergeData();
        var content = ApplicationProfileInstanceExclusionLetterBuilder.IsExcel(template.FileName)
            ? ApplicationProfileInstanceExclusionLetterBuilder.MergeExcel(template.Content, sample)
            : ApplicationProfileInstanceExclusionLetterBuilder.Merge(template.Content, sample);
        return Task.FromResult(OfficeFilePreviewResultFactory.FromOfficeOrPdf(_converter, content, "Sample_" + template.FileName));
    }
}
