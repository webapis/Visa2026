using System;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Blazor.Server.Services;

public sealed class ApplicationWorkspacePendingOpen : IApplicationWorkspacePendingOpen
{
    public Guid ApplicationId { get; set; }
}
