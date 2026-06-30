using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.DesignTime;
using DevExpress.ExpressApp.Core;
using DevExpress.ExpressApp.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.Blazor.Server.Services.Migration;

/// <summary>
/// Boots the same XAF stack as the Blazor host without Kestrel — for VISA2014 in-process import.
/// </summary>
public sealed class HeadlessMigrationHost : IDisposable
{
    private readonly IHost _host;
    private readonly IDisposable _importScope;

    public INonSecuredObjectSpaceFactory ObjectSpaceFactory { get; }

    private HeadlessMigrationHost(IHost host, INonSecuredObjectSpaceFactory objectSpaceFactory, IDisposable importScope)
    {
        _host = host;
        ObjectSpaceFactory = objectSpaceFactory;
        _importScope = importScope;
    }

    public static HeadlessMigrationHost Start(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is required.", nameof(connectionString));

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", connectionString);
        Environment.SetEnvironmentVariable("ConnectionStrings__ConnectionString", connectionString);

        var hostBuilder = Program.CreateHostBuilder(Array.Empty<string>());
        var host = hostBuilder.Build();
        host.Start();

        var factory = host.Services.GetRequiredService<INonSecuredObjectSpaceFactory>();
        var importScope = MigrationImportContext.BeginDataImportScope();
        return new HeadlessMigrationHost(host, factory, importScope);
    }

    public void Dispose()
    {
        _importScope.Dispose();
        _host.StopAsync().GetAwaiter().GetResult();
        _host.Dispose();
    }
}
