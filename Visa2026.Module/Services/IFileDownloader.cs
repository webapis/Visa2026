using System.IO;
using System.Threading.Tasks;

namespace Visa2026.Module.Services
{
    public interface IFileDownloader
    {
        Task DownloadAsync(string fileName, Stream stream, string contentType = "application/pdf");
    }
}