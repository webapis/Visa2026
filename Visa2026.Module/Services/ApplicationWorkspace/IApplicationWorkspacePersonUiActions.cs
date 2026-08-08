using System;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Bridges in-component workspace toolbar buttons to XAF link/unlink popup actions.
/// </summary>
public interface IApplicationWorkspacePersonUiActions
{
    event Action? WorkspaceChanged;

    bool IsAvailable { get; }

    void LinkPerson();

    void UnlinkPerson();

    void NotifyWorkspaceChanged();
}
