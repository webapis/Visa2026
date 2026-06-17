namespace Visa2026.Blazor.Server.Services;

/// <summary>
/// Resolves <see cref="IFilePreviewSource"/> implementations by <see cref="IFilePreviewSource.SourceType"/>.
/// Register sources via DI as <c>IFilePreviewSource</c> implementations; this registry is injected into the drawer.
/// </summary>
public sealed class FilePreviewSourceRegistry
{
    private readonly IReadOnlyDictionary<string, IFilePreviewSource> _sources;

    public FilePreviewSourceRegistry(IEnumerable<IFilePreviewSource> sources) =>
        _sources = sources.ToDictionary(s => s.SourceType, StringComparer.OrdinalIgnoreCase);

    public IFilePreviewSource? TryResolve(string sourceType) =>
        _sources.TryGetValue(sourceType, out var source) ? source : null;
}
