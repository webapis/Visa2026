using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Extensibility;

[assembly: AssemblyTitle("Visa2026.OfficeSync")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace Visa2026.OfficeSync
{
    [ComImport]
    [Guid("000C0396-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IRibbonExtensibility
    {
        [DispId(1)]
        string GetCustomUI(string ribbonId);
    }

    [ComImport]
    [Guid("000C0395-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IRibbonControl
    {
        [DispId(1)]
        string Id { get; }
    }

    [ComVisible(true)]
    [Guid("C4E8A1D2-6B37-4E55-8F10-2A9D7C6E5B41")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IVisaSync
    {
        [DispId(1)]
        void OnSync(IRibbonControl control);
    }

    /// <summary>
    /// Application add-in. The Sync button is on the Home tab in every Word and Excel
    /// window, including templates that already exist and templates added later.
    /// </summary>
    [ComVisible(true)]
    [Guid("A7C3E1B2-4D58-4F0A-9C21-6B8E5D4A1F30")]
    [ProgId("Visa2026.OfficeSync.Connect")]
    [ClassInterface(ClassInterfaceType.None)]
    public sealed class Connect : IVisaSync, IDTExtensibility2, IRibbonExtensibility
    {
        private const string RibbonXml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<customUI xmlns=\"http://schemas.microsoft.com/office/2009/07/customui\">" +
            "<ribbon><tabs><tab idMso=\"TabHome\">" +
            "<group id=\"Visa2026SyncGroup\" label=\"Visa2026\" insertBeforeMso=\"GroupClipboard\">" +
            "<button id=\"Visa2026Sync\" label=\"Sync\" size=\"large\" imageMso=\"FileSave\" " +
            "screentip=\"Sync to Visa2026\" " +
            "supertip=\"Saves this template and publishes it. The same button is on every Word and Excel template.\" " +
            "onAction=\"OnSync\"/>" +
            "</group></tab></tabs></ribbon></customUI>";

        private object _application;

        public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
        {
            _application = application;
        }

        public void OnDisconnection(ext_DisconnectMode removeMode, ref Array custom)
        {
            _application = null;
        }

        public void OnAddInsUpdate(ref Array custom)
        {
        }

        public void OnStartupComplete(ref Array custom)
        {
        }

        public void OnBeginShutdown(ref Array custom)
        {
        }

        public string GetCustomUI(string ribbonId)
        {
            return RibbonXml;
        }

        public void OnSync(IRibbonControl control)
        {
            try
            {
                var path = SaveAndGetPath();
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    Show("Save the file, or open it with Edit template in Visa2026.");
                    return;
                }

                var metaPath = path + ".meta.json";
                if (!File.Exists(metaPath))
                {
                    Show("This file was not opened from Visa2026. Use Edit template, then Sync.");
                    return;
                }

                var meta = File.ReadAllText(metaPath);
                var templateId = JsonString(meta, "templateId");
                var server = JsonString(meta, "serverBase");
                var token = JsonString(meta, "ribbonToken");
                if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(token))
                {
                    var host = ReadHost(Path.GetDirectoryName(path));
                    if (string.IsNullOrWhiteSpace(server))
                        server = host.Server;
                    if (string.IsNullOrWhiteSpace(token))
                        token = host.Token;
                }

                if (string.IsNullOrWhiteSpace(templateId) || string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(token))
                {
                    Show("Click Edit template in Visa2026 once. After that, Sync works for every template in this folder.");
                    return;
                }

                if (!Allowed(server))
                {
                    Show("Visa2026 address is not allowed.");
                    return;
                }

                byte[] bytes;
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, bytes.Length);
                }

                var url = server.TrimEnd('/') + "/api/user-report-templates/staging/ribbon-sync";
                using (var client = new HttpClient())
                using (var form = new MultipartFormDataContent())
                {
                    client.Timeout = TimeSpan.FromMinutes(2);
                    client.DefaultRequestHeaders.TryAddWithoutValidation("X-Visa2026-Ribbon-Token", token);
                    form.Add(new StringContent(templateId), "templateId");
                    var file = new ByteArrayContent(bytes);
                    file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    form.Add(file, "file", Path.GetFileName(path));
                    var response = client.PostAsync(url, form).Result;
                    var body = response.Content.ReadAsStringAsync().Result;
                    if (!response.IsSuccessStatusCode)
                    {
                        Show("Sync failed (" + (int)response.StatusCode + ").");
                        return;
                    }

                    if (body.IndexOf("Failed", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Show("Sync failed. " + JsonString(body, "errorMessage"));
                        return;
                    }

                    Show("Synced to Visa2026.");
                }
            }
            catch (Exception ex)
            {
                Show(ex.Message);
            }
        }

        private string SaveAndGetPath()
        {
            if (_application == null)
                return null;

            var app = (dynamic)_application;
            string name = app.Name;
            if (name != null && name.IndexOf("Excel", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                dynamic book = app.ActiveWorkbook;
                if (book == null)
                    return null;
                string fullName = book.FullName;
                if (string.IsNullOrWhiteSpace(fullName) || fullName.IndexOf("\\", StringComparison.Ordinal) < 0)
                    return null;
                book.Save();
                return book.FullName;
            }

            dynamic document = app.ActiveDocument;
            if (document == null)
                return null;
            string path = document.FullName;
            if (string.IsNullOrWhiteSpace(path) || path.IndexOf("\\", StringComparison.Ordinal) < 0)
                return null;
            document.Save();
            return document.FullName;
        }

        private static HostFile ReadHost(string folder)
        {
            var local = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Visa2026",
                "TemplateEdit",
                "ribbon-host.json");
            var beside = string.IsNullOrWhiteSpace(folder) ? null : Path.Combine(folder, "ribbon-host.json");
            var path = beside != null && File.Exists(beside) ? beside : local;
            if (!File.Exists(path))
                return new HostFile();

            var json = File.ReadAllText(path);
            return new HostFile
            {
                Server = JsonString(json, "serverBase"),
                Token = JsonString(json, "ribbonToken")
            };
        }

        private static string JsonString(string json, string name)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(name))
                return null;

            var match = Regex.Match(
                json,
                "\"" + Regex.Escape(name) + "\"\\s*:\\s*\"([^\"]*)\"",
                RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Replace("\\/", "/") : null;
        }

        private static bool Allowed(string server)
        {
            Uri uri;
            if (!Uri.TryCreate(server, UriKind.Absolute, out uri))
                return false;
            if (string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
                return true;
            return string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase)
                && (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
                    || uri.Host == "127.0.0.1");
        }

        private static void Show(string message)
        {
            MessageBox.Show(message, "Visa2026", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private sealed class HostFile
        {
            public string Server;
            public string Token;
        }
    }
}
