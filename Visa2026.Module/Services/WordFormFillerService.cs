using DocxTemplater;
using System.Collections.Generic;
using System.IO;

namespace Visa2026.Module.Services
{
    /// <summary>
    /// Implements <see cref="IWordFormFillerService"/> using DocxTemplater.
    /// Model is bound under the prefix "ds" — templates use {{ds.Property}} and {{#ds.rows}}.
    /// </summary>
    public class WordFormFillerService : IWordFormFillerService
    {
        public void FillForm(Stream templateStream, Stream outputStream,
                             IDictionary<string, object> data)
        {
            var template = new DocxTemplate(templateStream);
            template.BindModel("ds", data);
            template.Save(outputStream);
        }

        public void FillListForm(Stream templateStream, Stream outputStream,
                                 IDictionary<string, object> header,
                                 IEnumerable<IDictionary<string, object>> rows)
        {
            // Merge header fields + rows collection into one model dict bound as "ds"
            var model = new Dictionary<string, object>(header)
            {
                ["rows"] = rows
            };
            var template = new DocxTemplate(templateStream);
            template.BindModel("ds", model);
            template.Save(outputStream);
        }
    }
}
