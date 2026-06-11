using System;
using System.Diagnostics;
using System.IO;
using System.Text;
namespace Visa2026.E2E.Tests;

/// <summary>
/// Starts <see cref="Visa2026.Blazor.Server"/> before EasyTest <c>RunApplication</c> so CI can wait longer
/// than DevExpress <c>WaitScriptLoading</c> (60s) while Kestrel binds on :5050.
/// </summary>
internal static class EasyTestHostProcessLauncher
{
    private static Process? _hostProcess;
    private static StreamWriter? _stdoutWriter;
    private static StreamWriter? _stderrWriter;
    private static string? _logDirectory;

    internal static string LogDirectory =>
        _logDirectory ??= Path.Combine(Environment.CurrentDirectory, "easytest-host-logs");

    internal static void EnsureHostRunning(string blazorServerProjectPath)
    {
        if (EasyTestHostLifecycle.IsPortListening(EasyTestHostEnvironment.EasyTestPort))
        {
            WriteDiagnostic("Host already listening on :5050.");
            return;
        }

        string hostExe = EasyTestHostLaunch.ResolveHostExecutable(blazorServerProjectPath);
        Directory.CreateDirectory(LogDirectory);

        string stdoutPath = Path.Combine(LogDirectory, "host-out.log");
        string stderrPath = Path.Combine(LogDirectory, "host-err.log");

        var startInfo = new ProcessStartInfo
        {
            FileName = hostExe,
            Arguments = EasyTestHostLaunch.HostArguments,
            WorkingDirectory = Path.GetDirectoryName(hostExe)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        EasyTestHostLaunch.ApplyHostEnvironment(startInfo);

        WriteDiagnostic($"Starting host: {hostExe} {EasyTestHostLaunch.HostArguments}");
        WriteDiagnostic($"Host logs: {LogDirectory}");

        _stdoutWriter = new StreamWriter(stdoutPath, append: false, Encoding.UTF8) { AutoFlush = true };
        _stderrWriter = new StreamWriter(stderrPath, append: false, Encoding.UTF8) { AutoFlush = true };

        _hostProcess = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start Visa2026.Blazor.Server for EasyTest.");

        _hostProcess.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
                _stdoutWriter.WriteLine(e.Data);
        };
        _hostProcess.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
                _stderrWriter.WriteLine(e.Data);
        };
        _hostProcess.BeginOutputReadLine();
        _hostProcess.BeginErrorReadLine();

        try
        {
            EasyTestHostReadiness.WaitUntilHttpResponds(EasyTestCITuning.HostStartupTimeout);
        }
        catch
        {
            WriteDiagnostic(BuildDiagnostics());
            throw;
        }

        if (_hostProcess.HasExited)
        {
            string message = $"Host exited before HTTP ready (code {_hostProcess.ExitCode}).{Environment.NewLine}{BuildDiagnostics()}";
            throw new InvalidOperationException(message);
        }

        WriteDiagnostic("Managed host is running and HTTP-ready.");
    }

    internal static void StopManagedHost()
    {
        if (_hostProcess == null)
            return;

        try
        {
            if (!_hostProcess.HasExited)
                _hostProcess.Kill(entireProcessTree: true);
        }
        catch (Exception ex)
        {
            WriteDiagnostic($"StopManagedHost kill failed: {ex.Message}");
        }
        finally
        {
            _hostProcess.Dispose();
            _hostProcess = null;
        }

        try
        {
            _stdoutWriter?.Dispose();
            _stderrWriter?.Dispose();
        }
        catch
        {
            // Ignore log writer dispose errors.
        }
        finally
        {
            _stdoutWriter = null;
            _stderrWriter = null;
        }
    }

    internal static string BuildDiagnostics()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Host process exited: {_hostProcess?.HasExited == true}, exit code: {_hostProcess?.ExitCode.ToString() ?? "n/a"}");
        builder.AppendLine(ReadLogTail(Path.Combine(LogDirectory, "host-err.log")));
        builder.AppendLine(ReadLogTail(Path.Combine(LogDirectory, "host-out.log")));
        return builder.ToString();
    }

    private static string ReadLogTail(string path, int maxChars = 6000)
    {
        if (!File.Exists(path))
            return $"{Path.GetFileName(path)}: (missing)";

        string content = File.ReadAllText(path);
        if (content.Length <= maxChars)
            return $"{Path.GetFileName(path)}:{Environment.NewLine}{content}";

        return $"{Path.GetFileName(path)} (tail):{Environment.NewLine}{content[^maxChars..]}";
    }

    private static void WriteDiagnostic(string message)
    {
        string line = $"[EasyTest] {message}";
        Trace.WriteLine(line);
        Console.WriteLine(line);
    }
}
