using Visa2026.Blazor.Server.Services.Migration;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Boots the headless XAF host and prepares lookup resolver + import target for in-process legacy load.
/// </summary>
internal sealed class Visa2014HeadlessImportSession : IAsyncDisposable
{
    private readonly HeadlessMigrationHost _host;
    private readonly Visa2014ObjectSpaceImportTarget _target;

    public Visa2014ODataLookupResolver Resolver { get; }

    public IVisa2014ImportTarget Target => _target;

    private Visa2014HeadlessImportSession(
        HeadlessMigrationHost host,
        Visa2014ODataLookupResolver resolver,
        Visa2014ObjectSpaceImportTarget target)
    {
        _host = host;
        Resolver = resolver;
        _target = target;
    }

    public static Task<Visa2014HeadlessImportSession> OpenAsync(
        string targetConnectionString,
        int batchSize = 50,
        bool updateDatabase = false)
    {
        if (updateDatabase)
            Console.WriteLine("WRN --update-database on headless host is not supported yet; ensure schema is current.");

        var host = HeadlessMigrationHost.Start(targetConnectionString);

        var tenantCatalogDir = ResolveTenantCatalogDir();
        var resolver = new Visa2014ODataLookupResolver();
        using (var lookupSpace = host.ObjectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Visa2026.Module.BusinessObjects.Person)))
        {
            MigrationImportContext.ApplyImportObjectSpaceHooks(lookupSpace);
            resolver.LoadFromObjectSpace(lookupSpace, tenantCatalogDir);
        }

        var target = new Visa2014ObjectSpaceImportTarget(host.ObjectSpaceFactory, batchSize);
        return Task.FromResult(new Visa2014HeadlessImportSession(host, resolver, target));
    }

    private static string? ResolveTenantCatalogDir()
    {
        var solutionRoot = Visa2014ContentRoot.FindSolutionRoot();
        if (string.IsNullOrWhiteSpace(solutionRoot))
            return null;

        return Path.Combine(
            solutionRoot,
            "Visa2026.Module",
            "DatabaseUpdate",
            "LookupCatalogs",
            "tenant");
    }

    internal static string? ResolveTenantCatalogDirStatic() => ResolveTenantCatalogDir();

    public ValueTask DisposeAsync()
    {
        _target.Dispose();
        _host.Dispose();
        return ValueTask.CompletedTask;
    }
}
