using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Visa2026.PortalFiling.Worker;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(prefix: "PORTALFILING_");

builder.Services.Configure<PortalFilingOptions>(
    builder.Configuration.GetSection(PortalFilingOptions.SectionName));
builder.Services.AddHttpClient<Visa2026ApiClient>();
builder.Services.AddSingleton<PortalFormFiller>();
builder.Services.AddHostedService<PortalFilingWorkerHostedService>();

var host = builder.Build();
await host.RunAsync();