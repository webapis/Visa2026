namespace Visa2026.Module.Services.RuntimeLogging;

public interface IApplicationRuntimeLogResolution
{
    Task<bool> TryApplyAsync(RuntimeLogResolutionUpdate update, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApplicationRuntimeLogResolutionSummary>> ListOpenAsync(
        int limit,
        CancellationToken cancellationToken = default);

    Task<ApplicationRuntimeLogResolutionSummary?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
