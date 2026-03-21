namespace Visa2026.Blazor.Server.WebApi;

/// <summary>
/// Extension methods that configure the Web API middleware pipeline.
/// Called from Startup.Configure — keeps Startup.cs clean.
/// </summary>
public static class WebApiAppExtensions
{
    public static IApplicationBuilder UseVisaWebApi(
        this IApplicationBuilder app,
        IWebHostEnvironment env)
    {
        // Swagger UI — development only
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Visa2026 WebApi v1");
            });
        }

        // Short-circuit XAF's /api/challenge redirect.
        // XAF redirects unauthenticated API requests to /api/challenge.
        // We intercept it here and return a clean JSON 401 instead,
        // so JWT clients are not sent to an HTML challenge page.
        app.UseWhen(
            context => context.Request.Path.StartsWithSegments("/api/challenge"),
            branch => branch.Run(async context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    "{\"error\":\"Unauthorized. " +
                    "POST /api/Authentication/Authenticate to obtain a JWT token.\"}");
            }));

        return app;
    }
}