﻿﻿﻿using DevExpress.ExpressApp.ApplicationBuilder;
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
                    .AddDashboards(options =>
                    {
                        options.DashboardDataType = typeof(DevExpress.Persistent.BaseImpl.EF.DashboardData);
                    })
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
                    .AddScheduler()
                    .AddStateMachine(options =>
                    {
                        options.StateMachineStorageType = typeof(DevExpress.Persistent.BaseImpl.EF.StateMachine.StateMachine);
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
                    application.DatabaseUpdateMode = DevExpress.ExpressApp.DatabaseUpdateMode.UpdateDatabaseAlways;
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
                                businessObjectDbContextOptions.UseConnectionString(connectionString);
                            },
                            (serviceProvider, auditHistoryDbContextOptions) =>
                            {
                                string connectionString = Configuration.GetConnectionString("ConnectionString")
                                    ?? Configuration.GetConnectionString("DefaultConnection");
                                ArgumentNullException.ThrowIfNull(connectionString);
                                auditHistoryDbContextOptions.UseConnectionString(connectionString);
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
            services.AddSingleton<Visa2026.Module.Services.VisaCancelExtFilterService>();
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