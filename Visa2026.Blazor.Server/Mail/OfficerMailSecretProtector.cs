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

        // Passwords saved before encryption have no marker. Use them as typed.
        if (!protectedValue.StartsWith(IOfficerMailSecretProtector.Prefix, StringComparison.Ordinal))
            return protectedValue;

        var payload = protectedValue[IOfficerMailSecretProtector.Prefix.Length..];
        try
        {
            return protector.Unprotect(payload);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not decrypt the stored SMTP password. Re-enter it on Configuration → Mail server.");
            return null;
        }
    }
}
