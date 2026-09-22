using Visa2026.Module.Editors;
using Visa2026.Module.Localization;
using Xunit;

namespace Visa2026.Module.Tests.Localization;

public class CommaSeparatedMultiSelectLocalizationTests
{
    [Fact]
    public void Resolve_WorkPermittedLocationAlias_UsesWorkPermitTitles()
    {
        var texts = CommaSeparatedMultiSelectLocalization.Resolve(
            CommaSeparatedMultiSelectEditorAliases.WorkPermittedLocation);

        Assert.Equal(
            VisaUiMessages.Get("CommaMultiSelect.WorkPermit.PopupTitle"),
            texts.PopupTitle);
        Assert.Equal(
            VisaUiMessages.Get("CommaMultiSelect.WorkPermit.AddPlaceholder"),
            texts.AddPlaceholder);
        Assert.Equal(VisaUiMessages.Get("CommaMultiSelect.Add"), texts.AddButtonText);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(CommaSeparatedMultiSelectEditorAliases.BorderZone)]
    [InlineData("unknown-alias")]
    public void Resolve_NonWorkPermitAlias_UsesBorderZoneTitles(string editorAlias)
    {
        var texts = CommaSeparatedMultiSelectLocalization.Resolve(editorAlias);

        Assert.Equal(
            VisaUiMessages.Get("CommaMultiSelect.BorderZone.PopupTitle"),
            texts.PopupTitle);
        Assert.Equal(
            VisaUiMessages.Get("CommaMultiSelect.BorderZone.AddPlaceholder"),
            texts.AddPlaceholder);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void LocalizeCatalogMessage_Blank_ReturnsOperationFailed(string message)
    {
        var localized = CommaSeparatedMultiSelectLocalization.LocalizeCatalogMessage(message);

        Assert.Equal(VisaUiMessages.Get("CommaMultiSelect.Error.OperationFailed"), localized);
    }

    [Fact]
    public void LocalizeCatalogMessage_KnownKey_ResolvesViaVisaUiMessages()
    {
        const string key = "CommaMultiSelect.RenameSuccess";

        var localized = CommaSeparatedMultiSelectLocalization.LocalizeCatalogMessage(key);

        Assert.Equal(VisaUiMessages.Get(key), localized);
        Assert.NotEqual(key, localized);
    }

    [Fact]
    public void LocalizeCatalogMessage_PlainText_PassesThrough()
    {
        const string raw = "Already exists in catalog";

        Assert.Equal(raw, CommaSeparatedMultiSelectLocalization.LocalizeCatalogMessage(raw));
    }
}
