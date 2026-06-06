using System.Collections.Generic;

namespace Visa2026.Module.Services.WordReports;

public sealed class ApplicationWordReportPackageReadinessHint
{
    public required string MessageKey { get; init; }

    public IReadOnlyList<string> FormatArgs { get; init; } = [];
}
