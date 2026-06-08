using Microsoft.AspNetCore.Http;
using Visa2026.Module.Services.RuntimeLogging;

namespace Visa2026.Blazor.Server.Services;

public sealed class HttpApplicationRuntimeLogUserContext : IApplicationRuntimeLogUserContext
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public HttpApplicationRuntimeLogUserContext(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public string? GetCurrentUserName()
    {
        var identity = httpContextAccessor.HttpContext?.User?.Identity;
        return identity?.IsAuthenticated == true ? identity.Name : null;
    }
}
