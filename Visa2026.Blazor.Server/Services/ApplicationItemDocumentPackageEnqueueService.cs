using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Visa2026.Module.Localization;
using Visa2026.Module.Services;

namespace Visa2026.Blazor.Server.Services;

public sealed class ApplicationItemDocumentPackageEnqueueService
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ApplicationItemPdfBatchEnqueueService batchEnqueueService;

    public ApplicationItemDocumentPackageEnqueueService(
        IHttpContextAccessor httpContextAccessor,
        ApplicationItemPdfBatchEnqueueService batchEnqueueService)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.batchEnqueueService = batchEnqueueService;
    }

    public Task<ApplicationItemDocumentPackageEnqueueOutcome> EnqueueDefaultPackageAsync(
        IReadOnlyList<Guid> applicationItemIds) =>
        EnqueuePackageAsync(applicationItemIds, ApplicationItemDocumentPackageOptions.CreateDefaults());

    public Task<ApplicationItemDocumentPackageEnqueueOutcome> EnqueuePackageAsync(
        IReadOnlyList<Guid> applicationItemIds,
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

        if (!batchEnqueueService.TryEnqueuePackage(
                applicationItemIds,
                packageOptions,
                userName,
                culture,
                out var result,
                out var errorMessageKey)
            || result == null)
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
