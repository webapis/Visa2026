using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.ApplicationProfilePicker;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Picker shelf DisplayName localization and null/empty mutation gates (no ObjectSpace graph).
/// </summary>
public sealed class ApplicationProfilePickerShelfServiceGateTests
{
    [Theory]
    [InlineData(ApplicationProfilePickerShelfCodes.Invitations, "ApplicationProfileInstance.Picker.Shelf.Invitations")]
    [InlineData(ApplicationProfilePickerShelfCodes.Registration, "ApplicationProfileInstance.Picker.Shelf.Registration")]
    [InlineData(ApplicationProfilePickerShelfCodes.FamilyMemberOnly, "ApplicationProfileInstance.Picker.Shelf.FamilyMemberOnly")]
    [InlineData(ApplicationProfilePickerShelfCodes.WorkPermitOnly, "ApplicationProfileInstance.Picker.Shelf.WorkPermitOnly")]
    public void DisplayName_SystemShelf_UsesLocalizedCatalog(string code, string messageKey)
    {
        var shelf = new ApplicationProfilePickerShelf
        {
            Code = code,
            Name = "seed-fallback-name",
            IsSystem = true,
        };

        Assert.Equal(VisaUiMessages.Get(messageKey), ApplicationProfilePickerShelfService.DisplayName(shelf));
    }

    [Fact]
    public void DisplayName_CustomShelf_UsesStoredName()
    {
        var shelf = new ApplicationProfilePickerShelf
        {
            Code = null,
            Name = "Mary contracts",
            IsSystem = false,
        };

        Assert.Equal("Mary contracts", ApplicationProfilePickerShelfService.DisplayName(shelf));
        Assert.Equal(string.Empty, ApplicationProfilePickerShelfService.DisplayName(null));
    }

    [Fact]
    public void DisplayName_UnknownSystemCode_FallsBackToName()
    {
        var shelf = new ApplicationProfilePickerShelf
        {
            Code = "custom-system-code",
            Name = "Custom system",
            IsSystem = true,
        };

        Assert.Equal("Custom system", ApplicationProfilePickerShelfService.DisplayName(shelf));
    }

    [Fact]
    public void TryMoveToShelf_Rejects_NullObjectSpace_OrEmptyProfile()
    {
        Assert.False(ApplicationProfilePickerShelfService.TryMoveToShelf(
            null!,
            Guid.NewGuid(),
            null,
            out var error));
        Assert.Equal(VisaUiMessages.Get("ApplicationProfileInstance.Picker.Shelf.MoveFailed"), error);

        Assert.False(ApplicationProfilePickerShelfService.TryMoveToShelf(
            null!,
            Guid.Empty,
            null,
            out error));
        Assert.Equal(VisaUiMessages.Get("ApplicationProfileInstance.Picker.Shelf.MoveFailed"), error);
    }

    [Fact]
    public void TryCreateShelf_Rejects_NullObjectSpace_OrBlankName()
    {
        Assert.Null(ApplicationProfilePickerShelfService.TryCreateShelf(null!, "  ", out var error));
        Assert.Equal(VisaUiMessages.Get("ApplicationProfileInstance.Picker.Shelf.NameRequired"), error);

        Assert.Null(ApplicationProfilePickerShelfService.TryCreateShelf(null!, null!, out error));
        Assert.Equal(VisaUiMessages.Get("ApplicationProfileInstance.Picker.Shelf.NameRequired"), error);
    }

    [Fact]
    public void TryRenameShelf_Rejects_NullObjectSpace_EmptyId_OrBlankName()
    {
        Assert.False(ApplicationProfilePickerShelfService.TryRenameShelf(
            null!,
            Guid.NewGuid(),
            "  ",
            out var error));
        Assert.Equal(VisaUiMessages.Get("ApplicationProfileInstance.Picker.Shelf.NameRequired"), error);

        Assert.False(ApplicationProfilePickerShelfService.TryRenameShelf(
            null!,
            Guid.Empty,
            "ok",
            out error));
        Assert.Equal(VisaUiMessages.Get("ApplicationProfileInstance.Picker.Shelf.NameRequired"), error);
    }

    [Fact]
    public void TryDeleteShelf_Rejects_NullObjectSpace_OrEmptyId()
    {
        Assert.False(ApplicationProfilePickerShelfService.TryDeleteShelf(
            null!,
            Guid.Empty,
            out var error));
        Assert.Equal(VisaUiMessages.Get("ApplicationProfileInstance.Picker.Shelf.DeleteFailed"), error);

        Assert.False(ApplicationProfilePickerShelfService.TryDeleteShelf(
            null!,
            Guid.NewGuid(),
            out error));
        Assert.Equal(VisaUiMessages.Get("ApplicationProfileInstance.Picker.Shelf.DeleteFailed"), error);
    }

    [Fact]
    public void Heuristic_ServicePassportInvitation_ResolvesInvitationsShelf()
    {
        var profile = new ApplicationProfile
        {
            Code = "get_invitation_service_passport",
            ForEmployee = true,
            ActionFamily = ApplicationProfileActionFamily.Issuance,
            ProduceInvitation = true,
        };

        Assert.Equal(
            ApplicationProfilePickerShelfCodes.Invitations,
            ApplicationProfilePickerShelfHeuristic.ResolveShelfCode(profile));
    }
}
