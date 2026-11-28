using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Blazor.Server.Services;

/// <summary>
/// Paths for the officer-PC template folder. An absolute path is returned only when this
/// process is that PC (loopback) or <see cref="TemplateEditStagingOptions.WindowsProfileUserName"/> is set.
/// </summary>
internal static class TemplateEditStagingPathHints
{
    public static string FolderLeaf(TemplateEditStagingOptions options)
    {
        var parts = Subfolder(options).Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? "TemplateEdit" : parts[^1];
    }

    public static string FolderDisplay(TemplateEditStagingOptions options, HttpContext? http)
    {
        if (TryAbsoluteFolder(options, http, createOnLoopback: false, out var absolute))
            return absolute;

        return UnexpandedFolder(options);
    }

    public static string CopyPath(TemplateEditStagingOptions options, HttpContext? http, string fileName)
    {
        if (TryAbsoluteFolder(options, http, createOnLoopback: true, out var absolute))
            return Path.Combine(absolute, fileName);

        return Path.Combine(UnexpandedFolder(options), fileName);
    }

    /// <summary>
    /// True when the browser is on the same machine as this process and that process
    /// has a normal user AppData folder Word can open.
    /// </summary>
    public static bool CanWriteOnThisMachine(HttpContext? http)
    {
        if (!IsClientOnThisMachine(http))
            return false;

        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(root))
            return false;

        return root.Contains("systemprofile", StringComparison.OrdinalIgnoreCase) == false;
    }

    /// <summary>This PC's <c>%LOCALAPPDATA%\Visa2026\TemplateEdit</c>, created when requested.</summary>
    public static string? TryThisMachineFolder(TemplateEditStagingOptions options, HttpContext? http, bool create)
    {
        if (!CanWriteOnThisMachine(http))
            return null;

        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(root, Subfolder(options));
        if (create)
            Directory.CreateDirectory(folder);

        return folder;
    }

    /// <summary>Absolute file path Word or Excel can open. Null when the host is not the officer PC.</summary>
    public static string? OfficeOpenPath(TemplateEditStagingOptions options, HttpContext? http, string fileName)
    {
        if (!TryAbsoluteFolder(options, http, createOnLoopback: true, out var absolute))
            return null;

        return Path.Combine(absolute, fileName);
    }

    private static string Subfolder(TemplateEditStagingOptions options) =>
        string.IsNullOrWhiteSpace(options.LocalFolderSubfolderName)
            ? @"Visa2026\TemplateEdit"
            : options.LocalFolderSubfolderName;

    private static string UnexpandedFolder(TemplateEditStagingOptions options) =>
        @"%LOCALAPPDATA%\" + Subfolder(options).Trim('\\', '/');

    private static bool TryAbsoluteFolder(
        TemplateEditStagingOptions options,
        HttpContext? http,
        bool createOnLoopback,
        out string folder)
    {
        var sub = Subfolder(options);
        if (!string.IsNullOrWhiteSpace(options.WindowsProfileUserName))
        {
            folder = Path.Combine(
                @"C:\Users",
                options.WindowsProfileUserName.Trim(),
                "AppData",
                "Local",
                sub);
            return true;
        }

        if (IsLoopback(http))
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            folder = Path.Combine(root, sub);
            if (createOnLoopback)
            {
                try
                {
                    Directory.CreateDirectory(folder);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            return true;
        }

        folder = string.Empty;
        return false;
    }

    private static bool IsLoopback(HttpContext? http) =>
        IPAddress.IsLoopback(Normalize(http?.Connection.RemoteIpAddress) ?? IPAddress.None);

    private static bool IsClientOnThisMachine(HttpContext? http)
    {
        var ip = Normalize(http?.Connection.RemoteIpAddress);
        if (ip == null)
            return false;

        if (IPAddress.IsLoopback(ip))
            return true;

        try
        {
            foreach (var address in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                var candidate = Normalize(address);
                if (candidate != null && candidate.Equals(ip))
                    return true;
            }
        }
        catch (SocketException)
        {
        }

        return false;
    }

    private static IPAddress? Normalize(IPAddress? ip)
    {
        if (ip == null)
            return null;

        return ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip;
    }
}
