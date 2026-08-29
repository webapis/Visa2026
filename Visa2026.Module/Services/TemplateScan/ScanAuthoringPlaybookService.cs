#nullable enable

using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Visa2026.Module.Services.TemplateScan;

public interface IScanAuthoringPlaybookService
{
    ScanAuthoringPlaybook GetPlaybook();
}

public sealed class ScanAuthoringPlaybookService : IScanAuthoringPlaybookService
{
    internal const string ResourceName = "Visa2026.Module.Resources.TemplateAuthoring.SCAN_AUTHORING_PLAYBOOK.md";

    private readonly Lazy<ScanAuthoringPlaybook> _playbook;

    public ScanAuthoringPlaybookService()
    {
        _playbook = new Lazy<ScanAuthoringPlaybook>(Load);
    }

    public ScanAuthoringPlaybook GetPlaybook() => _playbook.Value;

    private static ScanAuthoringPlaybook Load()
    {
        var assembly = typeof(ScanAuthoringPlaybookService).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded scan authoring playbook not found: {ResourceName}. Ensure the file is an EmbeddedResource in Visa2026.Module.csproj.");

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var markdown = reader.ReadToEnd();
        if (string.IsNullOrWhiteSpace(markdown))
            throw new InvalidOperationException("Scan authoring playbook resource is empty.");

        var fingerprint = ComputeFingerprint(markdown);
        return new ScanAuthoringPlaybook
        {
            Markdown = markdown,
            Fingerprint = fingerprint,
            VersionLabel = fingerprint[..12],
        };
    }

    internal static string ComputeFingerprint(string markdown)
    {
        var bytes = Encoding.UTF8.GetBytes(markdown);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
