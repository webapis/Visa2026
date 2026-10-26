#nullable enable

using System;
using Visa2026.Module.Services.PreviewSlot;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Progress-letter preview-slot occupant keys added with multi-file correct-step
/// (inline drafts + open-preview-only). Distinct from base empty/app keys in PR #26.
/// </summary>
public class VisaPreviewSlotProgressLettersOccupantKeysTests
{
    private static readonly Guid AppId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly Guid ProgressId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly Guid FileId = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa");

    [Fact]
    public void ForProgressLetters_request_without_preview_uses_app_key()
    {
        var key = VisaPreviewSlotOccupantKeys.ForProgressLetters(new ProgressLettersSlotRequest
        {
            ApplicationProfileInstanceId = AppId,
        });

        Assert.Equal($"progress-letters:app:{AppId:N}", key);
    }

    [Fact]
    public void ForProgressLetters_inline_files_win_over_preview_flags()
    {
        var key = VisaPreviewSlotOccupantKeys.ForProgressLetters(new ProgressLettersSlotRequest
        {
            ApplicationProfileInstanceId = AppId,
            OpenPreviewOnly = true,
            FocusProgressId = ProgressId,
            FocusDisplayName = "Letter",
            PreviewAllFiles = true,
            InlineFiles =
            [
                new ProgressLetterInlineFile { FileName = "a.pdf", Content = [1] },
                new ProgressLetterInlineFile { FileName = "b.pdf", Content = [2] },
            ],
        });

        Assert.Equal($"progress-letters:app:{AppId:N}|inline:2:Letter", key);
    }

    [Fact]
    public void ForProgressLetters_open_preview_all_files_uses_all_token()
    {
        var key = VisaPreviewSlotOccupantKeys.ForProgressLetters(new ProgressLettersSlotRequest
        {
            ApplicationProfileInstanceId = AppId,
            OpenPreviewOnly = true,
            FocusProgressId = ProgressId,
            PreviewAllFiles = true,
        });

        Assert.Equal($"progress-letters:app:{AppId:N}|preview:{ProgressId:N}:all", key);
    }

    [Fact]
    public void ForProgressLetters_open_preview_one_file_uses_file_id()
    {
        var key = VisaPreviewSlotOccupantKeys.ForProgressLetters(new ProgressLettersSlotRequest
        {
            ApplicationProfileInstanceId = AppId,
            OpenPreviewOnly = true,
            FocusProgressId = ProgressId,
            FocusFileId = FileId,
        });

        Assert.Equal($"progress-letters:app:{AppId:N}|preview:{ProgressId:N}:{FileId:N}", key);
    }

    [Fact]
    public void ForProgressLetters_open_preview_without_file_id_uses_one_token()
    {
        var key = VisaPreviewSlotOccupantKeys.ForProgressLetters(new ProgressLettersSlotRequest
        {
            ApplicationProfileInstanceId = AppId,
            OpenPreviewOnly = true,
            FocusProgressId = ProgressId,
        });

        Assert.Equal($"progress-letters:app:{AppId:N}|preview:{ProgressId:N}:one", key);
    }

    [Fact]
    public void ForProgressLetters_empty_application_stays_empty_even_with_preview()
    {
        var key = VisaPreviewSlotOccupantKeys.ForProgressLetters(new ProgressLettersSlotRequest
        {
            ApplicationProfileInstanceId = Guid.Empty,
            OpenPreviewOnly = true,
            FocusProgressId = ProgressId,
        });

        Assert.Equal($"progress-letters:empty|preview:{ProgressId:N}:one", key);
    }
}
