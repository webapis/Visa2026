#nullable enable

using Visa2026.Module.Services.TemplateScan;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanWizardGenerateGuardTests
{
    [Fact]
    public void Remap_busy_blocks_continue_so_review_does_not_jump_to_preview()
    {
        Assert.False(ScanWizardGenerateGuard.AllowStart(
            busy: true,
            onFieldReview: true,
            onClarification: false,
            onPreview: false));
    }

    [Fact]
    public void Review_can_continue_when_idle()
    {
        Assert.True(ScanWizardGenerateGuard.AllowStart(
            busy: false,
            onFieldReview: true,
            onClarification: false,
            onPreview: false));
    }

    [Fact]
    public void Preview_can_regenerate_when_idle()
    {
        Assert.True(ScanWizardGenerateGuard.AllowStart(
            busy: false,
            onFieldReview: false,
            onClarification: false,
            onPreview: true));
    }

    [Fact]
    public void Upload_and_generating_cannot_start_generate()
    {
        Assert.False(ScanWizardGenerateGuard.AllowStart(
            busy: false,
            onFieldReview: false,
            onClarification: false,
            onPreview: false));
    }
}