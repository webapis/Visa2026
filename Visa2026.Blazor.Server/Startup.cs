using DevExpress.ExpressApp.ApplicationBuilder;
using DevExpress.ExpressApp.Blazor.ApplicationBuilder;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Visa2026.Blazor.Server.Services;
using Microsoft.AspNetCore.OData;
using Visa2026.Blazor.Server.WebApi;   // <-- our new extension namespace
using Visa2026.Module.Module_Interface;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ExcelReports;
using Visa2026.Module.Services.UserReports;
using Visa2026.Module.Services.StateNotifications;
using Visa2026.Module.Services.Feedback;
using Visa2026.Module.Services.WordReports;
using Visa2026.Module.Services.ApplicationItemLinkedDocuments;
using Visa2026.Blazor.Server.Localization;
using Visa2026.Module.DatabaseUpdate;

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
            long maxUploadBytes = Configuration.GetValue<long?>("FileUpload:MaxRequestBodyBytes") ?? 10485760L;
            if (maxUploadBytes < 6 * 1024 * 1024)
                maxUploadBytes = 10485760L;
            int multipartLimit = maxUploadBytes > int.MaxValue ? int.MaxValue : (int)maxUploadBytes;
            services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = multipartLimit;
                options.ValueLengthLimit = multipartLimit;
            });

            services.AddSingleton(typeof(Microsoft.AspNetCore.SignalR.HubConnectionHandler<>), typeof(ProxyHubConnectionHandler<>));

            var appHolder = new XafApplicationHolder();
            services.AddSingleton(appHolder);

            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddHttpClient();
            services.AddHttpContextAccessor();
            services.AddScoped<CircuitHandler, CircuitHandlerProxy>();
            VisaLocalization.ConfigureServices(services);
            services.AddXaf(Configuration, builder =>
            {
                builder.UseApplication<Visa2026BlazorApplication>();
                builder.Modules
                    .AddAuditTrailEFCore()
                    .AddCloning()
                    .AddConditionalAppearance()
                    .AddFileAttachments()
                    .AddNotifications()
                    .AddOffice()
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
                    }
                    else
                    {
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
                    }

                    // Hosted PDF/Word batch workers start with the host; run schema update here so their tables exist.
                    application.CheckCompatibility();

                    // Hot reload can swap Module DLLs without re-running CheckCompatibility; heal salary columns idempotently.
                    var connectionString = Configuration.GetConnectionString("DefaultConnection")
                        ?? Configuration.GetConnectionString("ConnectionString");
                    if (!string.IsNullOrWhiteSpace(connectionString))
                        ApplicationItemCurrentSalarySchemaSql.ApplyIfMissing(connectionString);
                });
                builder.ObjectSpaceProviders
                    .AddSecuredEFCore()
                    .WithAuditedDbContext(contexts =>
                    {
                        contexts.Configure<Visa2026.Module.BusinessObjects.Visa2026EFCoreDbContext, Visa2026.Module.BusinessObjects.Visa2026AuditingDbContext>(
                            (serviceProvider, businessObjectDbContextOptions) =>
                            {
                                string connectionString = Configuration.GetConnectionString("DefaultConnection")
                                    ?? Configuration.GetConnectionString("ConnectionString");
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
                                string connectionString = Configuration.GetConnectionString("DefaultConnection")
                                    ?? Configuration.GetConnectionString("ConnectionString");
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
            services.AddScoped<XafCultureInfoService>();
            services.AddScoped<IXafCultureInfoService, VisaXafCultureInfoService>();

            // ── Web API (auth, OData, Swagger) ────────────────────────────
            services.AddVisaWebApi(Configuration);
            // ──────────────────────────────────────────────────────────────

            services.Configure<TempFileCleanupSettings>(Configuration.GetSection("TempFileCleanupSettings"));
            services.AddScoped<IPdfFormFillerService, PdfFormFillerService>();
            services.AddScoped<IWordFormFillerService, WordFormFillerService>();
            services.AddScoped<IWordReportDefinition, BusinessTripSanawyReportDef>();
            services.AddScoped<IWordReportDefinition, BusinessTripArrivalLetterReportDef>();
            services.AddScoped<IWordReportDefinition, BusinessTripDepartureLetterReportDef>();
            services.AddScoped<IWordReportDefinition, AppRegCheckInLetterReportDef>();
            services.AddScoped<IWordReportDefinition, AppRegCheckInInternalLetterReportDef>();
            services.AddScoped<IWordReportDefinition, AppRegCheckOutLetterReportDef>();
            services.AddScoped<IWordReportDefinition, AppRegCheckOutInternalLetterReportDef>();
            services.AddScoped<IWordReportDefinition, AppRegExtLetterReportDef>();
            services.AddScoped<IWordReportDefinition, AppRegInfoChangeAddressLetterReportDef>();
            services.AddScoped<IWordReportDefinition, AppRegInfoChangePassportLetterReportDef>();
            services.AddScoped<IWordReportDefinition, AppInvFMLetterReportDef>();
            services.AddScoped<IWordReportDefinition, AppCancelVisaAndWPLetterReportDef>();
            services.AddScoped<IWordReportDefinition, AppCancelInvWPLetterReportDef>();
            services.AddScoped<IWordReportDefinition, AppChangePassportLetterReportDef>();
            services.AddScoped<IWordReportDefinition, AppExitVisaLetterReportDef>();
            services.AddScoped<IWordReportDefinition, AppAdditionalWPLocationLetterReportDef>();
            services.AddScoped<IWordReportDefinition, AppBorderZonePermissionLetterReportDef>();
            services.AddScoped<IWordReportDefinition, AppChangeInvLetterReportDef>();
            services.AddScoped<IWordReportDefinition, AppVisaExtFMLetterReportDef>();
            services.AddScoped<IWordReportDefinition, AppCancelInvWPItemReportDef>();
            services.AddScoped<IWordReportDefinition, AppCancelVisaAndWPItemReportDef>();
            services.AddScoped<IWordReportDefinition, AppChangeInvItemReportDef>();
            services.AddScoped<IWordReportDefinition, AppBorderZonePermissionItemReportDef>();
            services.AddScoped<IWordReportBundleBuilder, WordReportBundleBuilder>();
            services.AddScoped<ApplicationWordReportEntryGenerator>();
            services.AddSingleton<ApplicationWordReportOfficePreviewPdfConverter>();
            services.AddScoped<IFileDownloader, BlazorFileDownloader>();
            services.AddScoped<IReportVisibilityCacheService, ReportVisibilityCacheService>();
            if (Visa2026.Module.MailMergeFeature.Enabled)
                services.AddScoped<IMailMergeVisibilityCacheService, MailMergeVisibilityCacheService>();

            // User-defined Word report templates
            services.AddScoped<IUserReportPlaceholderExtractor, UserReportPlaceholderExtractor>();
            services.AddScoped<IUserReportValidationService, UserReportValidationService>();
            services.AddScoped<IUserReportVisibilityService, UserReportVisibilityService>();
            services.AddScoped<IUserReportGenerator, UserReportGenerator>();
            services.AddScoped<IExcelTemplatePlaceholderExtractor, ExcelTemplatePlaceholderExtractor>();
            services.AddScoped<IExcelReportValidationService, ExcelReportValidationService>();
            services.AddScoped<IExcelReportGenerator, ExcelReportGenerator>();
            services.AddHostedService<TempFileCleanupService>();
            services.AddHostedService<PdfGenerationBatchWorkerService>();
            services.AddHostedService<WordReportGenerationBatchWorkerService>();
            services.AddSingleton<Visa2026.Module.Services.VisaExtFilterService>();
            services.AddSingleton<Visa2026.Module.Services.VisaTransferFilterService>();
            services.AddSingleton<Visa2026.Module.Services.VisaFilterService>();
            services.AddSingleton<Visa2026.Module.Services.VisaStateFilterService>();
            services.AddSingleton<Visa2026.Module.Services.VisaCancelExtFilterService>();
            services.AddSingleton<Visa2026.Module.Services.VisaCancellationFilterService>();
            services.AddSingleton<Visa2026.Module.Services.RegistrationStateFilterService>();
            services.AddSingleton<Visa2026.Module.Services.StateNotifications.BoStateNotificationInboxFilterService>();
            services.AddSingleton<IBoStateNotificationSummaryService, BoStateNotificationPrototypeSummaryService>();
            services.AddScoped<IUserFeedbackSubmitService, UserFeedbackSubmitService>();
            services.AddSingleton<BoStateNotificationNavigationHelper>();
            services.AddScoped<ApplicationItemDocumentCopyPdfMerger>();
            services.AddScoped<ApplicationItemDocumentBatchSummaryPdfBuilder>();
            services.AddScoped<ApplicationItemDocumentFileAccess>();
            services.AddScoped<ApplicationItemPdfBatchEnqueueService>();
            services.AddScoped<ApplicationItemDocumentPackageEnqueueService>();
            services.AddScoped<ApplicationWordReportPackageCatalogService>();
            services.AddScoped<ApplicationWordReportBatchEnqueueService>();
            services.AddSingleton<IWordReportBatchTrackNotifier, WordReportBatchTrackNotifier>();
            services.AddScoped<ApplicationWordReportPackageFileAccess>();
            services.AddScoped<ApplicationWordReportPackageEnqueueService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            BatchWorkerSchemaGate.EnsureBatchSchemaColumns(
                app.ApplicationServices,
                app.ApplicationServices.GetService<ILoggerFactory>()?.CreateLogger(typeof(BatchWorkerSchemaGate)));

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
            VisaLocalization.UseVisaRequestLocalization(app);
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
                endpoints.MapControllers();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}