using Visa2026.Module.BusinessObjects.Email;
using Visa2026.Module.Services.EmailDocumentCopies;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects.Email;

public class EmailSmtpSettingsReadyAndPasswordTests
{
    [Fact]
    public void OnCreated_SetsOffice365Defaults()
    {
        var settings = new EmailSmtpSettings();
        settings.OnCreated();

        Assert.Equal(EmailSmtpSettings.DefaultHost, settings.Host);
        Assert.Equal(EmailSmtpSettings.DefaultPort, settings.Port);
        Assert.Equal(SmtpMailSecurity.StartTls, settings.Security);
        Assert.Equal(EmailSmtpSettings.DefaultFromName, settings.FromName);
        Assert.True(settings.BccOfficer);
        Assert.Equal(EmailSmtpSettings.DefaultMaxBytes, settings.MaxTotalAttachmentBytes);
        Assert.Equal(EmailSmtpSettings.DefaultTimeoutSeconds, settings.TimeoutSeconds);
        Assert.False(settings.PasswordIsSet);
        Assert.False(settings.IsReady);
    }

    [Theory]
    [InlineData(null, "officer@example.com", "secret", false)]
    [InlineData("", "officer@example.com", "secret", false)]
    [InlineData("   ", "officer@example.com", "secret", false)]
    [InlineData("smtp.office365.com", null, "secret", false)]
    [InlineData("smtp.office365.com", "", "secret", false)]
    [InlineData("smtp.office365.com", "officer@example.com", null, false)]
    [InlineData("smtp.office365.com", "officer@example.com", "", false)]
    [InlineData("smtp.office365.com", "officer@example.com", "secret", true)]
    public void IsReady_RequiresHostFromAddressAndPassword(
        string? host,
        string? fromAddress,
        string? protectedPassword,
        bool expected)
    {
        var settings = new EmailSmtpSettings
        {
            Host = host!,
            FromAddress = fromAddress!,
            ProtectedPassword = protectedPassword!,
        };

        Assert.Equal(expected, settings.IsReady);
        Assert.Equal(!string.IsNullOrWhiteSpace(protectedPassword), settings.PasswordIsSet);
    }

    [Fact]
    public void NewPassword_BlankOrCipherText_DoesNotReplaceStoredSecret()
    {
        var settings = new EmailSmtpSettings { ProtectedPassword = "already-set" };

        settings.NewPassword = "   ";
        Assert.Equal("already-set", settings.ProtectedPassword);

        settings.NewPassword = IOfficerMailSecretProtector.Prefix + "payload";
        Assert.Equal("already-set", settings.ProtectedPassword);
        Assert.Equal(IOfficerMailSecretProtector.Prefix + "payload", settings.NewPassword);
    }

    [Fact]
    public void NewPassword_Plaintext_UpdatesProtectedPassword()
    {
        var settings = new EmailSmtpSettings();

        settings.NewPassword = "MailboxPass1!";

        Assert.Equal("MailboxPass1!", settings.ProtectedPassword);
        Assert.True(settings.PasswordIsSet);
    }
}
