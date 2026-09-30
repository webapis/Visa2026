using Visa2026.Module.Services.ApplicationProfilePicker;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfilePickerVersionSelectionTests
{
    private static ApplicationProfilePickerVersionOption Chain(string name, bool isDefault) =>
        new()
        {
            VersionId = name == "TE-EN" ? TeEn : TnTgGu,
            Name = name,
            IsDefault = isDefault,
        };

    private static readonly Guid TeEn = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TnTgGu = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Resolve_uses_saved_default_when_another_chain_is_only_leftover()
    {
        var versions = new[]
        {
            Chain("TE-EN", isDefault: false),
            Chain("TN-TG-GU", isDefault: true),
        };

        var selected = ApplicationProfilePickerVersionSelection.Resolve(
            currentVersionId: TeEn,
            explicitlyChosen: false,
            versions);

        Assert.Equal(TnTgGu, selected);
    }

    [Fact]
    public void Resolve_keeps_the_chain_the_officer_clicked_for_this_case()
    {
        var versions = new[]
        {
            Chain("TE-EN", isDefault: false),
            Chain("TN-TG-GU", isDefault: true),
        };

        var selected = ApplicationProfilePickerVersionSelection.Resolve(
            currentVersionId: TeEn,
            explicitlyChosen: true,
            versions);

        Assert.Equal(TeEn, selected);
    }

    [Fact]
    public void Resolve_falls_back_to_saved_default_when_the_choice_is_not_in_the_catalog()
    {
        var versions = new[] { Chain("TN-TG-GU", isDefault: true) };

        var selected = ApplicationProfilePickerVersionSelection.Resolve(
            currentVersionId: Guid.NewGuid(),
            explicitlyChosen: true,
            versions);

        Assert.Equal(TnTgGu, selected);
    }
}
