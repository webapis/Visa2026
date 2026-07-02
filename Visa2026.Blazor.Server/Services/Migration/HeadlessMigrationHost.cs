using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.DesignTime;
using DevExpress.ExpressApp.Core;
using DevExpress.ExpressApp.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.Blazor.Server.Services.Migration;

/// <summary>
/// Boots the same XAF stack as the Blazor host for VISA2014 in-process import.
/// Kestrel binds <see cref="HeadlessMigrationHost.DefaultImportUrls"/> (:5002) so F5 on :5001 can stay up.
/// Import writes use ObjectSpace directly — HTTP is not used during <c>--inprocess</c>.
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

    /// <summary>
    /// Default Kestrel URL when the headless host starts (avoids clashing with F5 on :5001).
    /// Override with env <c>VISA2026_MIGRATION_IMPORT_URLS</c> (e.g. <c>http://localhost:5003</c>).
    /// In-process import uses ObjectSpace only — nothing calls this HTTP endpoint during <c>--inprocess</c>.
    /// </summary>
    public const string DefaultImportUrls = "http://localhost:5002";

    public static HeadlessMigrationHost Start(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is required.", nameof(connectionString));

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", connectionString);
        Environment.SetEnvironmentVariable("ConnectionStrings__ConnectionString", connectionString);

        Environment.SetEnvironmentVariable("Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command", "Warning");
        Environment.SetEnvironmentVariable("Logging__LogLevel__Microsoft.EntityFrameworkCore", "Warning");
        Environment.SetEnvironmentVariable("Logging__LogLevel__DevExpress", "Warning");

        var importUrls = Environment.GetEnvironmentVariable("VISA2026_MIGRATION_IMPORT_URLS") ?? DefaultImportUrls;
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", importUrls);

        var hostBuilder = Program.CreateHostBuilder(new[] { "--urls", importUrls });
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
