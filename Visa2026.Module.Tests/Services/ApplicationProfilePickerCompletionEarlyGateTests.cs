using Visa2026.Module.Services.ApplicationProfilePicker;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Create-application entry gates must fail closed before any ObjectSpace work.
/// </summary>
public sealed class ApplicationProfilePickerCompletionEarlyGateTests
{
    [Fact]
    public void TryCreateApplication_null_application_rejects()
    {
        Assert.False(ApplicationProfilePickerCompletionHelper.TryCreateApplication(
            application: null!,
            profileId: Guid.NewGuid(),
            out var error));
        Assert.Equal("Select an Application Profile first.", error);
    }

    [Fact]
    public void TryCreateApplication_empty_profileId_rejects()
    {
        Assert.False(ApplicationProfilePickerCompletionHelper.TryCreateApplication(
            application: null!,
            profileId: Guid.Empty,
            out var error));
        Assert.Equal("Select an Application Profile first.", error);
    }

    [Fact]
    public void TryCreateApplicationFromPersonStart_null_people_rejects()
    {
        Assert.False(ApplicationProfilePickerCompletionHelper.TryCreateApplicationFromPersonStart(
            application: null!,
            profileId: Guid.NewGuid(),
            personIds: null!,
            out var error,
            out var success));
        Assert.Equal("Select an Application Profile first.", error);
        Assert.Null(success);
    }

    [Fact]
    public void TryCreateApplicationFromPersonStart_empty_people_rejects_when_application_null()
    {
        // Null application short-circuits before the empty-people check.
        Assert.False(ApplicationProfilePickerCompletionHelper.TryCreateApplicationFromPersonStart(
            application: null!,
            profileId: Guid.NewGuid(),
            personIds: Array.Empty<Guid>(),
            out var error,
            out _));
        Assert.Equal("Select an Application Profile first.", error);
    }
}
