namespace Visa2026.Module.Services.EmailDocumentCopies;

/// <summary>Encrypts the shared SMTP password before it is stored on <c>EmailSmtpSettings</c>.</summary>
public interface IOfficerMailSecretProtector
{
    const string Prefix = "dp:v1:";

    string Protect(string plaintext);

    string? Unprotect(string? protectedValue);

    bool IsProtected(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.StartsWith(Prefix, StringComparison.Ordinal);
}
