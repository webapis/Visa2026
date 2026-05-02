using DevExpress.ExpressApp.ApplicationBuilder;
using DevExpress.ExpressApp.Blazor.ApplicationBuilder;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.EntityFrameworkCore;
using Visa2026.Blazor.Server.Services;
using Microsoft.AspNetCore.OData;
using Visa2026.Blazor.Server.WebApi;   // <-- our new extension namespace
using Visa2026.Module.Module_Interface;
using Visa2026.Module.Services;

namespace Visa2026.Blazor.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(typeof(Microsoft.AspNetCore.SignalR.HubConnectionHandler<>), typeof(ProxyHubConnectionHandler<>));

            var appHolder = new XafApplicationHolder();
            services.AddSingleton(appHolder);

            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddHttpContextAccessor();
            services.AddScoped<CircuitHandler, CircuitHandlerProxy>();
            services.AddXaf(Configuration, builder =>
            {
                builder.UseApplication<Visa2026BlazorApplication>();
                builder.Modules
                    .AddAuditTrailEFCore()
                    .AddCloning()
                    .AddConditionalAppearance()
                    .AddFileAttachments()
                    .AddNotifications()
                    .AddOffice(options => {
                        options.RichTextMailMergeDataType = typeof(DevExpress.Persistent.BaseImpl.EF.RichTextMailMergeData);
                    })
                    .AddReports(options =>
                    {
                        options.EnableInplaceReports = true;
                        options.ReportDataType = typeof(DevExpress.Persistent.BaseImpl.EF.ReportDataV2);
                        options.ReportStoreMode = DevExpress.ExpressApp.ReportsV2.ReportStoreModes.XML;
                    })
                    .AddValidation(options =>
                    {
                        options.AllowValidationDetailsAccess = false;
                    })
                    .AddViewVariants()
                    .Add<Visa2026.Module.Visa2026Module>()
                    .Add<Visa2026BlazorModule>();
                builder.AddBuildStep(application =>
                {
                    appHolder.Application = application;
                    // One-shot (e.g. Docker): FORCE_XAF_DB_UPDATE=true runs all ModuleUpdaters every start — slow; remove after DB is updated.
                    // Compose may set the var to "" when unset; GetValue<bool> throws on empty — only treat true/1 as enabled.
                    var forceXaf = Configuration["FORCE_XAF_DB_UPDATE"];
                    if (string.Equals(forceXaf, "true", StringComparison.OrdinalIgnoreCase) || forceXaf == "1")
                    {
                        application.DatabaseUpdateMode = DevExpress.ExpressApp.DatabaseUpdateMode.UpdateDatabaseAlways;
                        Console.WriteLine(
                            "Visa2026: FORCE_XAF_DB_UPDATE is enabled — DatabaseUpdateMode=UpdateDatabaseAlways. " +
                            "Unset after ModuleUpdaters have run once.");
                        return;
                    }

                    // UpdateDatabaseAlways runs ModuleUpdater + schema work on every launch (very slow).
                    // UpdateOldDatabase runs only when DB version is behind the app (schema / ModuleInfo check).
#if DEBUG
                    if (System.Diagnostics.Debugger.IsAttached
                        && application.CheckCompatibilityType == DevExpress.ExpressApp.CheckCompatibilityType.DatabaseSchema)
                        application.DatabaseUpdateMode = DevExpress.ExpressApp.DatabaseUpdateMode.UpdateDatabaseAlways;
                    else
                        application.DatabaseUpdateMode = DevExpress.ExpressApp.DatabaseUpdateMode.UpdateOldDatabase;
#else
                    application.DatabaseUpdateMode = DevExpress.ExpressApp.DatabaseUpdateMode.UpdateOldDatabase;
#endif
                });
                builder.ObjectSpaceProviders
                    .AddSecuredEFCore()
                    .WithAuditedDbContext(contexts =>
                    {
                        contexts.Configure<Visa2026.Module.BusinessObjects.Visa2026EFCoreDbContext, Visa2026.Module.BusinessObjects.Visa2026AuditingDbContext>(
                            (serviceProvider, businessObjectDbContextOptions) =>
                            {
                                string connectionString = Configuration.GetConnectionString("ConnectionString")
                                    ?? Configuration.GetConnectionString("DefaultConnection");
                                ArgumentNullException.ThrowIfNull(connectionString);
                                businessObjectDbContextOptions.UseSqlServer(connectionString, sqlOptions =>
                                {
                                    sqlOptions.CommandTimeout(180);
                                    // EF Core 8: split queries for multi-collection Includes (avoids cartesian explosion / warning 20504).
                                    sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                                });
                                // Required for HasChangeTrackingStrategy(ChangingAndChangedNotifications*) with BaseImpl types (e.g. FileData):
                                // proxies supply INotifyPropertyChanged / INotifyPropertyChanging on the CLR types. See DX doc XAF0031 / 404292.
                                businessObjectDbContextOptions.UseChangeTrackingProxies();
                                businessObjectDbContextOptions.UseObjectSpaceLinkProxies();
                                businessObjectDbContextOptions.UseLazyLoadingProxies();
                            },
                            (serviceProvider, auditHistoryDbContextOptions) =>
                            {
                                string connectionString = Configuration.GetConnectionString("ConnectionString")
                                    ?? Configuration.GetConnectionString("DefaultConnection");
                                ArgumentNullException.ThrowIfNull(connectionString);
                                auditHistoryDbContextOptions.UseSqlServer(connectionString, sqlOptions =>
                                {
                                    sqlOptions.CommandTimeout(180);
                                    sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                                });
                                auditHistoryDbContextOptions.UseChangeTrackingProxies();
                                auditHistoryDbContextOptions.UseObjectSpaceLinkProxies();
                                auditHistoryDbContextOptions.UseLazyLoadingProxies();
                            });
                    })
                    .AddNonPersistent();
                builder.Security
                    .UseIntegratedMode(options =>
                    {
                        options.Lockout.Enabled = true;
                        options.RoleType = typeof(PermissionPolicyRole);
                        options.UserType = typeof(Visa2026.Module.BusinessObjects.ApplicationUser);
                        options.UserLoginInfoType = typeof(Visa2026.Module.BusinessObjects.ApplicationUserLoginInfo);
                        options.Events.OnSecurityStrategyCreated += securityStrategy =>
                        {
                            ((SecurityStrategy)securityStrategy).PermissionsReloadMode = PermissionsReloadMode.NoCache;
                        };
                    })
                    .AddPasswordAuthentication(options =>
                    {
                        options.IsSupportChangePassword = true;
                    });
            });

            // ── Web API (auth, OData, Swagger) ────────────────────────────
            services.AddVisaWebApi(Configuration);
            // ──────────────────────────────────────────────────────────────

            services.Configure<TempFileCleanupSettings>(Configuration.GetSection("TempFileCleanupSettings"));
            services.AddScoped<IPdfFormFillerService, PdfFormFillerService>();
            services.AddScoped<IFileDownloader, BlazorFileDownloader>();
            services.AddScoped<IReportVisibilityCacheService, ReportVisibilityCacheService>();
            services.AddScoped<IMailMergeVisibilityCacheService, MailMergeVisibilityCacheService>();
            services.AddHostedService<TempFileCleanupService>();
            services.AddSingleton<Visa2026.Module.Services.VisaExtFilterService>();
            services.AddSingleton<Visa2026.Module.Services.VisaTransferFilterService>();
            services.AddSingleton<Visa2026.Module.Services.VisaFilterService>();
            services.AddSingleton<Visa2026.Module.Services.VisaStateFilterService>();
            services.AddSingleton<Visa2026.Module.Services.VisaCancelExtFilterService>();
            services.AddSingleton<Visa2026.Module.Services.VisaCancellationFilterService>();
            services.AddSingleton<Visa2026.Module.Services.RegistrationStateFilterService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRequestLocalization();
            app.UseStaticFiles();
            app.UseODataBatching();
            app.UseRouting();

            // ── Web API middleware (Swagger UI + /api/challenge fix) ───────
            app.UseVisaWebApi(env);
            // ──────────────────────────────────────────────────────────────

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseAntiforgery();
            app.UseXaf();

            // Redirect root "/" to the login page
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/")
                {
                    context.Response.Redirect("/LoginPage");
                    return;
                }
                await next();
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapXafEndpoints();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
                endpoints.MapControllers();
            });
        }
    }
}