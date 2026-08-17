using System;

namespace Visa2026.Module.Services;

/// <summary>
/// pdf.js XFA does not paint ImageField1 (Foxit/Adobe do). Browser preview overlays
/// <c>Person.Photo</c> as a data URI on the FOTO box.
/// </summary>
public static class PdfPersonPhotoDataUri
{
    public static string? FromBytes(byte[]? photo)
    {
        if (photo == null || photo.Length == 0)
            return null;

        var mime = photo.Length > 2 && photo[0] == 0xFF && photo[1] == 0xD8 && photo[2] == 0xFF
            ? "image/jpeg"
            : "image/png";
        return "data:" + mime + ";base64," + Convert.ToBase64String(photo);
    }
}
