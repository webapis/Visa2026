namespace Visa2026.Module.Services.RuntimeLogging;

public interface IApplicationErrorReporter
{
    void Report(ApplicationErrorReport report);
}
