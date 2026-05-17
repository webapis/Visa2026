using Microsoft.JSInterop;
using System;
using System.IO;
using System.Threading.Tasks;
using Visa2026.Module.Services;

namespace Visa2026.Blazor.Server.Services
{
    public class BlazorFileDownloader : IFileDownloader
    {
        private readonly IJSRuntime _jsRuntime;

        public BlazorFileDownloader(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task DownloadAsync(string fileName, Stream stream, string contentType = "application/pdf")
        {
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var base64 = Convert.ToBase64String(memoryStream.ToArray());

            var safeFileName = (fileName ?? "download").Replace("\\", "\\\\").Replace("'", "\\'");
            var safeContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;

            await _jsRuntime.InvokeVoidAsync("eval", $@"
                var link = document.createElement('a');
                link.download = '{safeFileName}';
                link.href = 'data:{safeContentType};base64,{base64}';
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
            ");
        }
    }
}