using System.Collections.Generic;
using System.IO;

namespace Visa2026.Module.Services
{
    /// <summary>
    /// Fills Word (.docx) templates with business object data and writes the result to an output stream.
    /// Templates use DocxTemplater syntax: {{ds.Property}}, {{#ds.rows}} ... {{/ds.rows}}.
    /// </summary>
    public interface IWordFormFillerService
    {
        /// <summary>
        /// Fills a single-record template.
        /// All top-level <paramref name="data"/> keys become {{ds.Key}} placeholders.
        /// </summary>
        void FillForm(Stream templateStream, Stream outputStream, IDictionary<string, object> data);

        /// <summary>
        /// Fills a list/tabular template.
        /// <paramref name="header"/> keys map to {{ds.Key}} placeholders outside the loop.
        /// <paramref name="rows"/> maps to {{#ds.rows}} ... {{/ds.rows}} with {{.FieldName}} per row.
        /// </summary>
        void FillListForm(Stream templateStream, Stream outputStream,
                          IDictionary<string, object> header,
                          IEnumerable<IDictionary<string, object>> rows);
    }
}
