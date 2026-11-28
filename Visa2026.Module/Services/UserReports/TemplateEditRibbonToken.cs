using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Visa2026.Module.Services.UserReports;

/// <summary>
/// Stateless token so a Word or Excel ribbon button can publish one template
/// without the officer's browser cookie. The secret stays on the Visa2026 server.
/// </summary>
public static class TemplateEditRibbonToken
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
    };

    public static string Issue(string secret, Guid templateId, string userName, DateTimeOffset expiresUtc)
    {
        if (string.IsNullOrWhiteSpace(secret))
            throw new ArgumentException("Ribbon sync secret is required.", nameof(secret));
        if (templateId == Guid.Empty)
            throw new ArgumentException("Template id is required.", nameof(templateId));

        var payload = JsonSerializer.Serialize(
            new Payload(templateId.ToString("D"), userName ?? string.Empty, expiresUtc.ToUnixTimeSeconds()),
            JsonOptions);
        var payloadPart = Base64Url(Encoding.UTF8.GetBytes(payload));
        return "v1." + payloadPart + "." + Sign(secret, payloadPart);
    }

    /// <summary>One token for every template this officer edits, current and future.</summary>
    public static string IssueHost(string secret, string userName, DateTimeOffset expiresUtc)
    {
        if (string.IsNullOrWhiteSpace(secret))
            throw new ArgumentException("Ribbon sync secret is required.", nameof(secret));

        var payload = JsonSerializer.Serialize(
            new Payload("*", userName ?? string.Empty, expiresUtc.ToUnixTimeSeconds()),
            JsonOptions);
        var payloadPart = Base64Url(Encoding.UTF8.GetBytes(payload));
        return "v1." + payloadPart + "." + Sign(secret, payloadPart);
    }

    public static bool TryValidateHost(
        string secret,
        string? token,
        DateTimeOffset now,
        out string userName)
    {
        userName = string.Empty;
        if (!TryRead(secret, token, now, out var templateKey, out userName))
            return false;
        if (!string.Equals(templateKey, "*", StringComparison.Ordinal))
        {
            userName = string.Empty;
            return false;
        }

        return true;
    }

    public static bool TryValidate(
        string secret,
        string? token,
        DateTimeOffset now,
        out Guid templateId,
        out string userName)
    {
        templateId = Guid.Empty;
        userName = string.Empty;
        if (!TryRead(secret, token, now, out var templateKey, out userName)
            || !Guid.TryParse(templateKey, out templateId)
            || templateId == Guid.Empty)
        {
            templateId = Guid.Empty;
            userName = string.Empty;
            return false;
        }

        return true;
    }

    private static bool TryRead(
        string secret,
        string? token,
        DateTimeOffset now,
        out string templateKey,
        out string userName)
    {
        templateKey = string.Empty;
        userName = string.Empty;
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(token))
            return false;

        var parts = token.Split('.');
        if (parts.Length != 3 || !string.Equals(parts[0], "v1", StringComparison.Ordinal))
            return false;

        var expected = Sign(secret, parts[1]);
        var actual = Encoding.ASCII.GetBytes(parts[2]);
        var expectedBytes = Encoding.ASCII.GetBytes(expected);
        if (actual.Length != expectedBytes.Length
            || !CryptographicOperations.FixedTimeEquals(actual, expectedBytes))
        {
            return false;
        }

        Payload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<Payload>(Encoding.UTF8.GetString(FromBase64Url(parts[1])), JsonOptions);
        }
        catch (Exception)
        {
            return false;
        }

        if (payload == null || string.IsNullOrEmpty(payload.T) || payload.E <= now.ToUnixTimeSeconds())
            return false;

        templateKey = payload.T;
        userName = payload.U ?? string.Empty;
        return true;
    }

    private static string Sign(string secret, string payloadPart)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Base64Url(hmac.ComputeHash(Encoding.ASCII.GetBytes(payloadPart)));
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromBase64Url(string text)
    {
        var padded = text.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2:
                padded += "==";
                break;
            case 3:
                padded += "=";
                break;
        }

        return Convert.FromBase64String(padded);
    }

    private sealed class Payload
    {
        public Payload(string t, string u, long e)
        {
            T = t;
            U = u;
            E = e;
        }

        public string T { get; set; }

        public string U { get; set; }

        public long E { get; set; }
    }
}
