using System;

namespace Visa2026.Module.Services.ApplicationWorkspace;

public interface IApplicationWorkspacePendingOpen
{
    Guid ApplicationId { get; set; }
}
