namespace Visa2026.Module.Services.RuntimeLogging;

public sealed class NullApplicationErrorReporter : IApplicationErrorReporter
{
    public void Report(ApplicationErrorReport report)
    {
    }
}
