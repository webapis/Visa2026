using Microsoft.Extensions.Logging;

namespace Visa2026.Module.Services.RuntimeLogging;

public static class ApplicationRuntimeLogLoggerExtensions
{
    public static void LogErrorWithCode(
        this ILogger logger,
        string errorCode,
        string message,
        params object?[] args) =>
        LogWithCode(logger, LogLevel.Error, errorCode, null, message, args);

    public static void LogErrorWithCode(
        this ILogger logger,
        string errorCode,
        Exception exception,
        string message,
        params object?[] args) =>
        LogWithCode(logger, LogLevel.Error, errorCode, exception, message, args);

    public static void LogWarningWithCode(
        this ILogger logger,
        string errorCode,
        string message,
        params object?[] args) =>
        LogWithCode(logger, LogLevel.Warning, errorCode, null, message, args);

    public static void LogWarningWithCode(
        this ILogger logger,
        string errorCode,
        Exception exception,
        string message,
        params object?[] args) =>
        LogWithCode(logger, LogLevel.Warning, errorCode, exception, message, args);

    private static void LogWithCode(
        ILogger logger,
        LogLevel level,
        string errorCode,
        Exception? exception,
        string message,
        params object?[] args)
    {
        var merged = new object?[args.Length + 1];
        if (args.Length > 0)
            Array.Copy(args, merged, args.Length);
        merged[args.Length] = errorCode;

        var template = message + " {ErrorCode}";

        if (exception != null)
            logger.Log(level, exception, template, merged);
        else
            logger.Log(level, template, merged);
    }
}
