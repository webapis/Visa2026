#nullable enable

using System;
using Visa2026.Module.Services.PreviewSlot;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Invitation compose defaults: case summary id wins over catalog default and first lookup option.
/// </summary>
public sealed class IssueIssuedHeaderFirstAssignedIdTests
{
    [Fact]
    public void FirstAssignedId_prefers_first_non_empty_guid()
    {
        var caseId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var defaultId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var firstLookup = Guid.Parse("99999999-8888-7777-6666-555555555555");

        Assert.Equal(
            caseId,
            IssueIssuedHeaderComposeService.FirstAssignedId(caseId, defaultId, firstLookup));
        Assert.Equal(
            defaultId,
            IssueIssuedHeaderComposeService.FirstAssignedId(null, defaultId, firstLookup));
        Assert.Equal(
            firstLookup,
            IssueIssuedHeaderComposeService.FirstAssignedId(Guid.Empty, null, firstLookup));
        Assert.Null(IssueIssuedHeaderComposeService.FirstAssignedId(null, Guid.Empty));
        Assert.Null(IssueIssuedHeaderComposeService.FirstAssignedId());
    }
}
