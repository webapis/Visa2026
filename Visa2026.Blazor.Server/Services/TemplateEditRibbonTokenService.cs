using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Blazor.Server.Services;

/// <summary>Issues and checks ribbon-sync tokens. The secret never leaves this server.</summary>
public sealed class TemplateEditRibbonTokenService
{
    private readonly string _secret;

    public TemplateEditRibbonTokenService(IOptions<TemplateEditStagingOptions> options)
    {
        _secret = ResolveSecret(options.Value.RibbonSyncKey);
    }

    public string IssueHost(string userName) =>
        TemplateEditRibbonToken.IssueHost(_secret, userName, DateTimeOffset.UtcNow.AddHours(12));

    public bool TryValidateHost(string? token, out string userName) =>
        TemplateEditRibbonToken.TryValidateHost(_secret, token, DateTimeOffset.UtcNow, out userName);

    private static string ResolveSecret(string? configured)
    {
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.Trim();

        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Visa2026",
            "ribbon-sync.key");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (File.Exists(path))
        {
            var existing = File.ReadAllText(path).Trim();
            if (existing.Length >= 32)
                return existing;
        }

        var created = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        File.WriteAllText(path, created);
        return created;
    }
}
