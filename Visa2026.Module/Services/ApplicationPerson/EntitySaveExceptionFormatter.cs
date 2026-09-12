using System;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

/// <summary>EF wraps Postgres unique violations; officers need the inner text.</summary>
public static class EntitySaveExceptionFormatter
{
    public static string ToOfficerMessage(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var innermost = exception;
        while (innermost.InnerException != null)
            innermost = innermost.InnerException;

        return string.IsNullOrWhiteSpace(innermost.Message)
            ? exception.Message
            : innermost.Message;
    }
}
