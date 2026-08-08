using System;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Scoped bridge from Blazor workspace UI to XAF link/unlink popup actions.
/// </summary>
public sealed class ApplicationWorkspacePersonUiActions : IApplicationWorkspacePersonUiActions
{
    private Action? _linkPerson;
    private Action? _unlinkPerson;

    public event Action? WorkspaceChanged;

    public bool IsAvailable => _linkPerson != null && _unlinkPerson != null;

    internal void Register(Action linkPerson, Action unlinkPerson)
    {
        _linkPerson = linkPerson;
        _unlinkPerson = unlinkPerson;
    }

    internal void Clear()
    {
        _linkPerson = null;
        _unlinkPerson = null;
    }

    public void LinkPerson() => _linkPerson?.Invoke();

    public void UnlinkPerson() => _unlinkPerson?.Invoke();

    public void NotifyWorkspaceChanged() => WorkspaceChanged?.Invoke();
}
