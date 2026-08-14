using System;

namespace Visa2026.Module.Services.ApplicationWorkspace;

public interface IApplicationWorkspacePendingOpen
{
    Guid ApplicationProfileInstanceId { get; set; }
}
