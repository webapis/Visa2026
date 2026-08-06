using Visa2026.Module.BusinessObjects.Operations;
using Visa2026.Module.Services.RuntimeLogging;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationRuntimeLogEnvironmentHelperTests
{
    [Fact]
    public void DetectDeploymentEnvironment_AppPoolId_MeansIisProduction()
    {
        using var _ = EnvScope.Set("APP_POOL_ID", "Visa2026Prod");
        using var __ = EnvScope.Set("ASPNETCORE_ENVIRONMENT", "Development");

        Assert.Equal(
            ApplicationRuntimeLogDeploymentEnvironment.IisProduction,
            ApplicationRuntimeLogEnvironmentHelper.DetectDeploymentEnvironment());
    }

    [Fact]
    public void DetectDeploymentEnvironment_Development_MeansLocalVisualStudio()
    {
        using var _ = EnvScope.Clear("APP_POOL_ID");
        using var __ = EnvScope.Set("ASPNETCORE_ENVIRONMENT", "Development");

        Assert.Equal(
            ApplicationRuntimeLogDeploymentEnvironment.LocalVisualStudio,
            ApplicationRuntimeLogEnvironmentHelper.DetectDeploymentEnvironment());
    }

    [Fact]
    public void DetectDeploymentEnvironment_ProductionWithoutPool_MeansIisProduction()
    {
        using var _ = EnvScope.Clear("APP_POOL_ID");
        using var __ = EnvScope.Set("ASPNETCORE_ENVIRONMENT", "Production");

        Assert.Equal(
            ApplicationRuntimeLogDeploymentEnvironment.IisProduction,
            ApplicationRuntimeLogEnvironmentHelper.DetectDeploymentEnvironment());
    }

    [Fact]
    public void DetectDeploymentEnvironment_Other_MeansUnknown()
    {
        using var _ = EnvScope.Clear("APP_POOL_ID");
        using var __ = EnvScope.Set("ASPNETCORE_ENVIRONMENT", "Staging");

        Assert.Equal(
            ApplicationRuntimeLogDeploymentEnvironment.Unknown,
            ApplicationRuntimeLogEnvironmentHelper.DetectDeploymentEnvironment());
    }

    [Fact]
    public void ResolveApplicationVersion_ReturnsNonEmpty()
    {
        var version = ApplicationRuntimeLogEnvironmentHelper.ResolveApplicationVersion();
        Assert.False(string.IsNullOrWhiteSpace(version));
    }

    private sealed class EnvScope : IDisposable
    {
        private readonly string _name;
        private readonly string _previous;

        private EnvScope(string name, string previous)
        {
            _name = name;
            _previous = previous;
        }

        public static EnvScope Set(string name, string value)
        {
            var previous = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
            return new EnvScope(name, previous);
        }

        public static EnvScope Clear(string name)
        {
            var previous = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, null);
            return new EnvScope(name, previous);
        }

        public void Dispose() => Environment.SetEnvironmentVariable(_name, _previous);
    }
}
