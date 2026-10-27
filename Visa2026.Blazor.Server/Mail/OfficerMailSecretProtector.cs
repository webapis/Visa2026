using Microsoft.AspNetCore.DataProtection;
using Visa2026.Module.Services.EmailDocumentCopies;

namespace Visa2026.Blazor.Server.Mail;

/// <summary>ASP.NET Data Protection wrapper for the shared SMTP password.</summary>
public sealed class OfficerMailSecretProtector : IOfficerMailSecretProtector
{
    public const string Purpose = "Visa2026.SmtpMail.Password.v1";

    private readonly IDataProtector protector;
    private readonly ILogger<OfficerMailSecretProtector> logger;

    public OfficerMailSecretProtector(
        IDataProtectionProvider dataProtectionProvider,
        ILogger<OfficerMailSecretProtector> logger)
    {
        protector = dataProtectionProvider.CreateProtector(Purpose);
        this.logger = logger;
    }

    public string Protect(string plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);
        return IOfficerMailSecretProtector.Prefix + protector.Protect(plaintext);
    }

    public string? Unprotect(string? protectedValue)
    {
        if (string.IsNullOrWhiteSpace(protectedValue))
            return null;

        var payload = protectedValue.StartsWith(IOfficerMailSecretProtector.Prefix, StringComparison.Ordinal)
            ? protectedValue[IOfficerMailSecretProtector.Prefix.Length..]
            : protectedValue;

        try
        {
            return protector.Unprotect(payload);
        }
        catch (Exception ex)
        {
            // Row saved before encryption was wired, or the key ring changed: use the stored value as-is.
            logger.LogWarning(ex, "Could not decrypt the stored SMTP password; using the saved value as plain text. Re-enter the password on Configuration → Mail server if send still fails.");
            return payload;
        }
    }
}
