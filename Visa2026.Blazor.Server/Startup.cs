﻿using DevExpress.ExpressApp.ApplicationBuilder;
using DevExpress.ExpressApp.Blazor.ApplicationBuilder;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.WebApi.Services;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Visa2026.Blazor.Server.Services;
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

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(typeof(Microsoft.AspNetCore.SignalR.HubConnectionHandler<>), typeof(ProxyHubConnectionHandler<>));

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
                    application.DatabaseUpdateMode = DevExpress.ExpressApp.DatabaseUpdateMode.UpdateDatabaseAlways;
                });
                builder.ObjectSpaceProviders
                    .AddSecuredEFCore(options =>
                    {
                        options.PreFetchReferenceProperties();
                    })
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

            // Cookie auth for the Blazor UI + JWT bearer for Web API clients.
            // The default scheme stays as Cookie so the Blazor UI is unaffected.
            var authentication = services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            });
            authentication.AddCookie(options =>
            {
                options.LoginPath = "/LoginPage";
            });
            // --- WEB API: JWT bearer scheme used by REST/OData clients.
            // Tokens are issued by POST /api/Authentication/Authenticate (built into XAF Web API).
            authentication.AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidIssuer = Configuration["Authentication:Jwt:Issuer"],
                    ValidAudience = Configuration["Authentication:Jwt:Audience"],
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes(
                            Configuration["Authentication:Jwt:IssuerSigningKey"]
                                ?? throw new InvalidOperationException("JWT IssuerSigningKey is not configured.")))
                };
            });

            // --- WEB API: Register Web API services and expose Business Objects.
            // Add a options.BusinessObject<T>() call for every entity you want to expose via REST.
            // Example:
            //   options.BusinessObject<Visa2026.Module.BusinessObjects.VisaApplication>();
            services.AddXafWebApi(Configuration, options =>
            {
                // TODO: Uncomment / add the business objects you want to expose:
                 options.BusinessObject<Visa2026.Module.BusinessObjects.VisaType>();
            });

            // --- WEB API: Register MVC controllers + OData routing
            services.AddControllers().AddOData((options, serviceProvider) =>
            {
                options
                    .AddRouteComponents("api/odata", new EdmModelBuilder(serviceProvider).GetEdmModel())
                    .EnableQueryFeatures(100);
            });

            // --- WEB API: Swagger / OpenAPI for browsing and testing endpoints
            services.AddSwaggerGen(c =>
            {
                c.EnableAnnotations();
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Visa2026 API",
                    Version = "v1",
                    Description = "Use options.BusinessObject<T>() in Startup.ConfigureServices to expose entities through this API."
                });
                // Allow JWT bearer tokens to be entered in the Swagger UI
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your JWT token. Obtain it via POST /api/Authentication/Authenticate."
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            services.Configure<TempFileCleanupSettings>(Configuration.GetSection("TempFileCleanupSettings"));
            services.AddScoped<IPdfFormFillerService, PdfFormFillerService>();
            services.AddScoped<IFileDownloader, BlazorFileDownloader>();
            services.AddScoped<IReportVisibilityCacheService, ReportVisibilityCacheService>();
            services.AddScoped<IMailMergeVisibilityCacheService, MailMergeVisibilityCacheService>();
            services.AddHostedService<TempFileCleanupService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                // --- WEB API: Swagger UI is served in Development only
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Visa2026 WebApi v1");
                });
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseRequestLocalization();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseAntiforgery();
            app.UseXaf();
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