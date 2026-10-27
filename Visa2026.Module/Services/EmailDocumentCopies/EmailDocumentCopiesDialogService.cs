using System;
using System.Collections.Generic;

namespace Visa2026.Module.Services.EmailDocumentCopies;

/// <summary>What the <b>Email copies</b> popup opens with.</summary>
public sealed class EmailDocumentCopiesDialogRequest
{
    /// <summary>Selected people in list order.</summary>
    public IReadOnlyList<Guid> PersonIds { get; init; } = Array.Empty<Guid>();

    /// <summary>Resend: exact records per person; the popup opens on Compose and skips type selection.</summary>
    public IReadOnlyList<EmailDocumentCopiesPersonSelection>? FixedRecords { get; init; }

    /// <summary>Resend: recipients, subject and message are copied from this email.</summary>
    public Guid? ResendFromSentEmailId { get; init; }

    /// <summary>Where the officer started, for example "Employees (3 people)".</summary>
    public string? SourceDescription { get; init; }
}

/// <summary>Opens and closes the circuit-wide <b>Email copies</b> popup.</summary>
public interface IEmailDocumentCopiesDialogService
{
    EmailDocumentCopiesDialogRequest? Current { get; }

    /// <summary>Raised on open and close; the popup host re-renders.</summary>
    event Action? Changed;

    void Open(EmailDocumentCopiesDialogRequest request);

    void Close();
}

/// <summary>Scoped (per Blazor circuit) implementation.</summary>
public sealed class EmailDocumentCopiesDialogService : IEmailDocumentCopiesDialogService
{
    public EmailDocumentCopiesDialogRequest? Current { get; private set; }

    public event Action? Changed;

    public void Open(EmailDocumentCopiesDialogRequest request)
    {
        Current = request ?? throw new ArgumentNullException(nameof(request));
        Changed?.Invoke();
    }

    public void Close()
    {
        if (Current == null)
            return;
        Current = null;
        Changed?.Invoke();
    }
}
