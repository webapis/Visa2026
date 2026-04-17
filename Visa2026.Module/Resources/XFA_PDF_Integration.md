# XFA PDF Integration — Visa_Application_TM_QR_08.pdf

## Overview

`Visa_Application_TM_QR_08.pdf` is a Turkmenistan visa application form built with **Adobe XFA (XML Forms Architecture)**. XFA forms store all field data in an embedded XML layer. They render correctly only in **Adobe Reader** and **Foxit PDF Reader** — Chrome, Edge, and LibreOffice show a static "Please wait..." placeholder because they have no XFA engine.

The integration fills this form programmatically using **Spire.PDF 12.x** and serves the result as a downloadable file from the Blazor Server app.

---

## Architecture

### Key Classes

| Class | Location | Role |
|---|---|---|
| `PdfFormFillerService` | `Services/PdfFormFillerService.cs` | Fills XFA fields and saves PDF |
| `IPdfFormFillerService` | `Services/IPdfFormFillerService.cs` | DI interface |
| `PdfMappingHelper` | `Services/PdfMappingHelper.cs` | Maps `ApplicationItem` data to XFA field keys |
| `ApplicationItemPdfController` | `Controllers/ApplicationItemPdfController.cs` | Single-item PDF action |
| `ApplicationPdfController` | `Controllers/ApplicationPdfController.cs` | Bulk PDF action (merge all items) |

### Template

- **File:** `Visa2026.Module/Resources/Visa_Application_TM_QR_08.pdf`
- **Embedded resource** in `Visa2026.Module.csproj` — no file path required at runtime
- **Configured via** `appsettings.json`: `PdfSettings:TemplatePath`
- **Fallback:** if the file path does not exist, the controller falls back to the embedded resource stream

---

## Field Filling — How It Works

### Text, Date, Checkbox, ChoiceList Fields

Spire's `XfaField` subclasses are used directly:

```csharp
XfaTextField       → textField.Value = value.ToString();
XfaDateTimeField   → dateTimeField.Value = dt.ToString("dd.MM.yyyy");
XfaCheckButtonField → checkButtonField.Checked = (bool)value;
XfaChoiceListField → choiceListField.SelectedItem = value.ToString();
```

Dates are always formatted as `dd.MM.yyyy` to match the template's display mask.

### Image Fields (FOTO) — Critical Details

**Background:** Spire.PDF 12.x has two image-assignment APIs that both silently fail for XFA forms:
- `XfaImageField.Image` — works in old Spire versions, no-op in 12.x
- `XfaImageField.ImageValueBase64` — property exists but is never written into the saved PDF

**Working solution:** Direct XML manipulation of the XFA datasets stream.

The XFA datasets XML (`form.XFAForm.XmlDatasets`) is the authoritative data store for all field values. For image fields, the node must contain raw base64 PNG data with a `xfa:contentType="image/png"` attribute.

```csharp
// 1. Collect base64 during field filling
_pendingImageBase64[field.Name] = Convert.ToBase64String(pngBytes);

// 2. After all fields filled, patch the XML directly
XmlNode xfaRoot = form.XFAForm.XmlDatasets;
XmlDocument xfaDoc = xfaRoot as XmlDocument ?? xfaRoot.OwnerDocument;

// Strip XFA array indices from field name: "topmostSubform[0].Page1[0].FOTO[0]" → "FOTO"
string localName = Regex.Replace(fieldName.Split('.')[^1], @"\[\d+\]", "");
var node = xfaRoot.SelectSingleNode($"//*[local-name()='{localName}']");

node.InnerText = base64String;  // raw base64, no data-URI prefix
var attr = xfaDoc.CreateAttribute("xfa", "contentType", "http://www.xfa.org/schema/xfa-data/1.0/");
attr.Value = "image/png";
node.Attributes.SetNamedItem(attr);
```

**Image preparation:** Input `byte[]` is always re-encoded as PNG before embedding to ensure renderer compatibility:

```csharp
using (var ms = new MemoryStream(rawBytes))
using (var img = Image.FromStream(ms))
using (var tmp = new MemoryStream())
{
    img.Save(tmp, ImageFormat.Png);
    imageBytes = tmp.ToArray();
}
```

### Save Strategy

**Do NOT use `SaveToStream` for XFA PDFs.** Spire's stream serialiser strips the rendered image data from the XFA datasets.

Use `SaveToFile` with a temp file, then copy to the output stream:

```csharp
string tempPdf = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pdf");
try
{
    pdfdoc.SaveToFile(tempPdf);
    using var fs = File.OpenRead(tempPdf);
    fs.CopyTo(outputStream);
}
finally
{
    try { File.Delete(tempPdf); } catch { }
}
```

---

## Merging Multiple PDFs

When generating one PDF per `ApplicationItem` and combining them, **do NOT use `PdfDocument.MergeFiles()`**. That API reconstructs the XFA XML layer from the source streams, discarding all filled data and reverting to the "Please wait..." placeholder.

**Working approach:** Import pages one-by-one into a new `PdfDocument`:

```csharp
var mergedDoc = new PdfDocument();
foreach (var sourceStream in sources)
{
    sourceStream.Position = 0;
    var sourceDoc = new PdfDocument();
    sourceDoc.LoadFromStream(sourceStream);
    for (int i = 0; i < sourceDoc.Pages.Count; i++)
        mergedDoc.InsertPage(sourceDoc, i);
}
mergedDoc.SaveToStream(outputStream, FileFormat.PDF);
```

This copies only the rendered page content — the XFA XML is never reconstructed.

---

## Field Mapping

Field keys are stored in the database as `PdfFormMapping` records and loaded via `PdfMappingHelper.GetMappings()`. The mapping uses reflection to traverse the `ApplicationItem` object graph (e.g. `Person.Photo`, `Person.FirstName`) and populate the `Dictionary<string, object>` passed to `FillForm`.

ChoiceList fields (dropdowns in the XFA form) require the **raw XFA code**, not the display label. `PdfMappingHelper.ResolveRawValue()` handles this translation using lookup tables. Unknown values are passed through with a warning log.

---

## Configuration

**`appsettings.json`:**
```json
{
  "PdfSettings": {
    "TemplatePath": "Resources/Visa_Application_TM_QR_08.pdf"
  }
}
```

Path is resolved relative to `AppContext.BaseDirectory`. If the file is not found, the embedded resource `Visa2026.Module.Resources.Visa_Application_TM_QR_08.pdf` is extracted to a temp file and used instead.

---

## Linux / Docker

`System.Drawing` (GDI+) is required for PNG conversion. The Dockerfile installs:

```dockerfile
RUN apt-get install -y libgdiplus libfontconfig1 libx11-6 libxrender1 ...
ENV DOTNET_System_Drawing_EnableUnixSupport=true
ENV LD_LIBRARY_PATH=/usr/lib/x86_64-linux-gnu:${LD_LIBRARY_PATH}
```

---

## Known Limitations

| Limitation | Detail |
|---|---|
| Chrome / Edge | Show "Please wait…" — XFA requires Adobe Reader or Foxit |
| LibreOffice | Cannot render XFA — `soffice --convert-to pdf` produces the static placeholder |
| `form.IsFlatten = true` | Does **not** flatten XFA to static pages in Spire 12.x |
| `SaveToStream` | Strips image data from XFA datasets in Spire 12.x — use `SaveToFile` |
| `MergeFiles()` | Reconstructs XFA XML, discarding filled data — use page-by-page `InsertPage` |
| `XfaImageField.Image` | No-op in Spire 12.x — use direct `XmlDatasets` XML manipulation |
| `ImageValueBase64` | No-op in Spire 12.x without the XML patch |

---

## Troubleshooting

**Photo not appearing in output PDF:**
1. Check log for `Image field 'FOTO' found. Value is null: False` — confirms data reaches the service
2. Check log for `XFA datasets: set image data on <FOTO>` — confirms XML patch succeeded
3. If log shows `XFA datasets: node <FOTO> not found` — the XFA field name in the template differs; check the field name log line `XFA field names in template: [...]`
4. Ensure the PDF is opened in **Foxit** or **Adobe Reader**, not Chrome/Edge

**All fields blank:**
- Verify `PdfSettings:TemplatePath` in `appsettings.json` resolves to a valid file, or that the embedded resource name matches exactly

**ChoiceList field renders blank:**
- The raw XFA code for that dropdown value may not be in the lookup table; check for warning log `display value '...' has no known raw mapping`
