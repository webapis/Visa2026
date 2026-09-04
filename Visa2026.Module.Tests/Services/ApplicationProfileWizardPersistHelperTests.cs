using System;
using Visa2026.Module.Services.ApplicationProfileWizard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfileWizardPersistHelperTests
{
    [Fact]
    public void FormatCommitError_UniqueCodeIndex_ReturnsOfficerMessage()
    {
        var ex = new InvalidOperationException(
            "duplicate key value violates unique constraint \"IX_ApplicationProfiles_Code\"");

        var message = ApplicationProfileWizardPersistHelper.FormatCommitError(ex);

        Assert.Contains("Code already exists", message, StringComparison.Ordinal);
    }
}