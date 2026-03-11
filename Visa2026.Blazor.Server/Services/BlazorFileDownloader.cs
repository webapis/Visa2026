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
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                var base64 = Convert.ToBase64String(memoryStream.ToArray());
                
                // Trigger file download in the browser using JavaScript
                await _jsRuntime.InvokeVoidAsync("eval", $@"
                    var link = document.createElement('a');
                    link.download = '{fileName}';
                    link.href = 'data:{contentType};base64,{base64}';
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);
                ");
            }
        }
    }
}