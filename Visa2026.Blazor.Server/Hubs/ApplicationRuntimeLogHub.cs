using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Visa2026.Blazor.Server.Hubs;

[Authorize]
public sealed class ApplicationRuntimeLogHub : Hub
{
}
