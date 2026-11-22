using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.Services.MigrationImport;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Path A issuing-invitation matching must stay a no-op for import, already-applied, and detached visas.
/// </summary>
public class VisaIssuingLinkPathAMatcherGateTests
{
    [Fact]
    public void TryApplyOnce_NullVisa_DoesNotThrow()
    {
        VisaIssuingLinkPathAMatcher.TryApplyOnce(null!);
    }

    [Fact]
    public void TryApplyOnce_AlreadyApplied_LeavesInvitationUnset()
    {
        var visa = new Visa
        {
            PathAIssuingLinksApplied = true,
            IssuingApplicationProfileInstance = new ApplicationProfileInstance(),
        };

        VisaIssuingLinkPathAMatcher.TryApplyOnce(visa);

        Assert.True(visa.PathAIssuingLinksApplied);
        Assert.Null(visa.IssuingInvitationItem);
    }

    [Fact]
    public void TryApplyOnce_DuringDataImport_DoesNotMarkApplied()
    {
        var visa = new Visa
        {
            IssuingApplicationProfileInstance = new ApplicationProfileInstance(),
        };

        using (MigrationImportContext.BeginDataImportScope())
            VisaIssuingLinkPathAMatcher.TryApplyOnce(visa);

        Assert.False(visa.PathAIssuingLinksApplied);
        Assert.Null(visa.IssuingInvitationItem);
    }

    [Fact]
    public void TryApplyOnce_MissingIssuingInstance_DoesNotMarkApplied()
    {
        var visa = new Visa();

        VisaIssuingLinkPathAMatcher.TryApplyOnce(visa);

        Assert.False(visa.PathAIssuingLinksApplied);
        Assert.Null(visa.IssuingInvitationItem);
    }

    [Fact]
    public void TryApplyOnce_DetachedVisaWithIssuingInstance_DoesNotMarkApplied()
    {
        // No ObjectSpace on a plain new Visa — Path A must not guess or stamp applied.
        var visa = new Visa
        {
            IssuingApplicationProfileInstance = new ApplicationProfileInstance(),
        };

        VisaIssuingLinkPathAMatcher.TryApplyOnce(visa);

        Assert.False(visa.PathAIssuingLinksApplied);
        Assert.Null(visa.IssuingInvitationItem);
    }
}
