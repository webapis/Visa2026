using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Visa2026.Module.Localization;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.PreviewSlot;

namespace Visa2026.Blazor.Server.Services;

public sealed class ApplicationItemDocumentPackageEnqueueService
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ApplicationProfileInstancePersonPdfBatchEnqueueService rosterBatchEnqueueService;

    public ApplicationItemDocumentPackageEnqueueService(
        IHttpContextAccessor httpContextAccessor,
        ApplicationProfileInstancePersonPdfBatchEnqueueService rosterBatchEnqueueService)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.rosterBatchEnqueueService = rosterBatchEnqueueService;
    }

    public Task<ApplicationItemDocumentPackageEnqueueOutcome> EnqueueDefaultPackageAsync(
        IReadOnlyList<Guid> lineIds) =>
        EnqueuePackageAsync(lineIds, ApplicationItemDocumentPackageOptions.CreateDefaults());

    public Task<ApplicationItemDocumentPackageEnqueueOutcome> EnqueuePackageAsync(
        IReadOnlyList<Guid> lineIds,
        ApplicationItemDocumentPackageOptions packageOptions)
    {
        string? userName = httpContextAccessor.HttpContext?.User?.Identity?.Name
            ?? httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrWhiteSpace(userName))
        {
            return Task.FromResult(new ApplicationItemDocumentPackageEnqueueOutcome
            {
                Success = false,
                ErrorMessage = VisaUiMessages.Get("ApplicationItemDocumentCopies.Package.ErrorNotSignedIn")
            });
        }

        string culture = VisaUiMessages.NormalizeCultureName(CultureInfo.CurrentUICulture.Name);
        ApplicationItemPdfBatchEnqueueResult? result = null;
        string? errorMessageKey = null;
        bool enqueued = rosterBatchEnqueueService.TryEnqueuePackage(
            lineIds,
            packageOptions,
            userName,
            culture,
            out result,
            out errorMessageKey);

        if (!enqueued || result == null)
        {
            return Task.FromResult(new ApplicationItemDocumentPackageEnqueueOutcome
            {
                Success = false,
                ErrorMessage = !string.IsNullOrWhiteSpace(errorMessageKey)
                    ? VisaUiMessages.Get(errorMessageKey)
                    : VisaUiMessages.Get("ApplicationItemDocumentCopies.Package.Error")
            });
        }

        string? notice = null;
        if (result.PassportZipWillSkip)
        {
            notice = VisaUiMessages.Format(
                "Pdf.QueuedPassportWarning",
                result.ItemCount,
                result.ItemsMissingCurrentPassport);
        }

        return Task.FromResult(new ApplicationItemDocumentPackageEnqueueOutcome
        {
            Success = true,
            BatchId = result.BatchId,
            NoticeMessage = notice
        });
    }
}

public sealed class ApplicationItemDocumentPackageEnqueueOutcome
{
    public bool Success { get; init; }

    public Guid BatchId { get; init; }

    public string? NoticeMessage { get; init; }

    public string? ErrorMessage { get; init; }
}
