using System.Text;
using DevExpress.ExpressApp.WebApi.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OData;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Visa2026.Blazor.Server.WebApi;

/// <summary>
/// Extension methods that register all Backend Web API Service dependencies.
/// Called from Startup.ConfigureServices — keeps Startup.cs clean.
/// </summary>
public static class WebApiServiceExtensions
{
    public static IServiceCollection AddVisaWebApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddWebApiAuthentication(configuration)
            .AddWebApiAuthorization()
            .AddWebApiOData(configuration)   // pass configuration through
            .AddWebApiSwagger();

        return services;
    }

    // ------------------------------------------------------------------
    // Authentication: Cookie (Blazor UI) + JWT (API clients)
    // ------------------------------------------------------------------
    private static IServiceCollection AddWebApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/LoginPage";

                // Return 401/403 JSON for /api routes instead of
                // redirecting to the HTML login page.
                options.Events.OnRedirectToLogin = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode =
                            StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }
                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };

                options.Events.OnRedirectToAccessDenied = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode =
                            StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    }
                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
            })
            .AddJwtBearer(options =>
            {
                // Tokens issued by POST /api/Authentication/Authenticate
                // See: JWT/AuthenticationController.cs
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = configuration["Authentication:Jwt:Issuer"],
                    ValidAudience = configuration["Authentication:Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            configuration["Authentication:Jwt:IssuerSigningKey"]
                                ?? throw new InvalidOperationException(
                                    "Authentication:Jwt:IssuerSigningKey is " +
                                    "missing from appsettings.json.")))
                };
            });

        return services;
    }

    // ------------------------------------------------------------------
    // Authorization: both Cookie and JWT satisfy the default policy
    // ------------------------------------------------------------------
    private static IServiceCollection AddWebApiAuthorization(
        this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder(
                CookieAuthenticationDefaults.AuthenticationScheme,
                JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .RequireXafAuthentication()
                .Build();
        });

        return services;
    }

    // ------------------------------------------------------------------
    // OData: expose Business Objects as REST endpoints
    // ------------------------------------------------------------------
    private static IServiceCollection AddWebApiOData(
        this IServiceCollection services,
        IConfiguration configuration)   // <-- required by AddXafWebApi
    {
        // FIX: AddXafWebApi requires IConfiguration as the first argument
        // when used in an existing XAF Blazor project.
        services.AddXafWebApi(configuration, options =>
        {
            options.BusinessObject<Visa2026.Module.BusinessObjects.VisaType>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.VisaCategory>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.Country>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.ApplicationType>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.ApplicationTypeFilter>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.ApplicationState>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.ApplicationLocation>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.CheckPoint>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.Department>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.EducationInstitution>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.EducationLevel>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.Gender>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.MaritalStatus>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.MigrationService>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.PassportType>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.Position>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.PurposeOfTravel>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.Region>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.Relationship>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.Specialty>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.Subcontractor>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.Urgency>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.ValidityDuration>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.VisaIssuedPlace>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.VisaPeriod>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.AddressOfResidence>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.Application>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.ApplicationItem>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.ApplicationProgress>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.BusinessTrip>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.BusinessTripPlan>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.BusinessTripAddress>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.City>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.Company>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.CompanyHead>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.Education>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.EmployeeContract>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.EmployeePositionHistory>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.Invitation>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.InvitationItem>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.Passport>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.Lodging>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.MedicalRecord>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.Person>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.ProjectContract>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.Registration>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.Rejection>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.RejectionItem>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.Representative>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.TravelHistory>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.ExternalArrival>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.ExternalDeparture>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.InternalArrival>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.InternalDeparture>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.Visa>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.WorkPermit>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.WorkPermitItem>();
            options.BusinessObject<Visa2026.Module.BusinessObjects.LocalEmployee>();
            // Add more entities here as needed:
            // options.BusinessObject<Visa2026.Module.BusinessObjects.VisaApplication>();
        });

        services.AddControllers().AddOData((options, serviceProvider) =>
        {
            options
                .AddRouteComponents("api/odata", new EdmModelBuilder(serviceProvider).GetEdmModel())
                .Select()    // enables $select
                .Filter()    // enables $filter
                .OrderBy()   // enables $orderby
                .SetMaxTop(10000) // removes the MaxTop=0 restriction — allows any $top value
                .Count();    // enables $count
        });

        return services;
    }

    // ------------------------------------------------------------------
    // Swagger UI with JWT bearer support
    // ------------------------------------------------------------------
    private static IServiceCollection AddWebApiSwagger(
        this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.EnableAnnotations();
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Visa2026 API",
                Version = "v1",
                Description =
                    "1) POST /api/Authentication/Authenticate  " +
                    "2) Copy token  " +
                    "3) Click Authorize  " +
                    "4) Paste token"
            });

            c.AddSecurityDefinition("JWT", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Name = "Bearer",
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "JWT"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}