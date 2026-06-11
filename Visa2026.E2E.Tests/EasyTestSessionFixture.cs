using System;
using System.IO;
using System.Threading.Tasks;
using DevExpress.EasyTest.Framework;
using Xunit;

namespace Visa2026.E2E.Tests;

/// <summary>One EasyTest host + browser session per test assembly (avoids 4× cold starts on CI).</summary>
public sealed class EasyTestSessionFixture : IAsyncLifetime
{
    internal const string BlazorAppName = "Visa2026Blazor";
    internal const string AppDBName = "Visa2026EasyTest";

    internal EasyTestFixtureContext FixtureContext { get; }
    internal IApplicationContext AppContext { get; }

    private readonly string _blazorServerProjectPath;

    public EasyTestSessionFixture()
    {
        FixtureContext = new EasyTestFixtureContext();
        _blazorServerProjectPath = Path.GetFullPath(
            Path.Combine(Environment.CurrentDirectory, @"..\..\..\..\Visa2026.Blazor.Server"));
        string blazorHostExecutable = EasyTestHostLaunch.ResolveHostExecutable(_blazorServerProjectPath);
        string webDriverPath = EasyTestWebDriverPath.Resolve();

        FixtureContext.RegisterApplications(
            new BlazorApplicationOptions(
                name: BlazorAppName,
                physicalPath: blazorHostExecutable,
                url: EasyTestHostEnvironment.BaseUrl,
                configuration: "EasyTest",
                ignoreCase: true,
                browser: "Edge",
                arguments: EasyTestHostLaunch.HostArguments,
                webDriverPath: webDriverPath,
                browserBinaryPath: string.Empty,
                runHeadless: EasyTestBrowserMode.RunHeadless)
        );

        FixtureContext.RegisterDatabases(
            new DatabaseOptions(
                AppDBName,
                EasyTestHostEnvironment.DatabaseName,
                server: EasyTestHostEnvironment.LocalDbServer)
        );

        AppContext = FixtureContext.CreateApplicationContext(BlazorAppName);
    }

    public Task InitializeAsync()
    {
        EasyTestPreflight.PrepareForTestSession(FixtureContext, AppDBName, _blazorServerProjectPath);
        EasyTestHostProcessLauncher.EnsureHostRunning(_blazorServerProjectPath);
        EasyTestApplicationLauncher.RunApplication(FixtureContext, AppContext);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        EasyTestHostLifecycle.StopHost(FixtureContext);
        return Task.CompletedTask;
    }
}
