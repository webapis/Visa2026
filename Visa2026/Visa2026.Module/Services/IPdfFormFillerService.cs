using System.Collections.Generic;
using System.IO;

namespace Visa2026.Module.Services
{
    public interface IPdfFormFillerService
    {
        void FillForm(string templatePath, Stream outputStream, Dictionary<string, object> data);
        void MergePdfs(Stream[] sources, Stream outputStream);
    }
}