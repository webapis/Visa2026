using System;
using System.Collections.Generic;
using System.Xml;

namespace Visa2026.Module.Services;

/// <summary>
/// Field 25 (Wizanyň derejesi) is an XFA choice list whose hidden save codes are not unique:
/// BS1 and TR2 both save as <c>14</c>. Viewers bind that code to the later row, so a BS1
/// case summary renders as TR2. On each fill, keep <c>14</c> only on the selected visa type.
/// </summary>
internal static class PdfVisaDegreeChoiceList
{
    internal const string FieldKey = "topmostSubform[0].Page2[0]._25[0]";

    /// <summary>Not an XFA field. Carries <see cref="BusinessObjects.VisaType.LocalizationKey"/> into the filler.</summary>
    internal const string LocalizationKeyDataKey = "__PdfVisaDegreeLocalizationKey";

    internal static bool Disambiguate(XmlNode templateRoot, string localizationKey)
    {
        if (templateRoot == null || string.IsNullOrWhiteSpace(localizationKey))
            return false;

        var field = templateRoot.SelectSingleNode("//*[local-name()='field' and @name='_25']");
        if (field == null)
            return false;

        if (!TryGetItemLists(field, out var displays, out var saves))
            return false;

        var keep = IndexOfDisplay(displays, localizationKey.Trim());
        if (keep < 0)
            return false;

        var code = saves[keep].InnerText?.Trim();
        if (string.IsNullOrEmpty(code))
            return false;

        var changed = false;
        for (var i = 0; i < saves.Count; i++)
        {
            if (i == keep)
                continue;
            if (!string.Equals(saves[i].InnerText?.Trim(), code, StringComparison.Ordinal))
                continue;

            var token = DisplayPrefix(displays[i].InnerText);
            if (string.IsNullOrEmpty(token) || string.Equals(token, code, StringComparison.Ordinal))
                token = "dup" + i;
            saves[i].InnerText = token;
            changed = true;
        }

        return changed;
    }

    internal static string SaveCodeFor(XmlNode templateRoot, string localizationKey)
    {
        if (templateRoot == null || string.IsNullOrWhiteSpace(localizationKey))
            return null;

        var field = templateRoot.SelectSingleNode("//*[local-name()='field' and @name='_25']");
        if (field == null || !TryGetItemLists(field, out var displays, out var saves))
            return null;

        var index = IndexOfDisplay(displays, localizationKey.Trim());
        return index < 0 ? null : saves[index].InnerText?.Trim();
    }

    private static bool TryGetItemLists(XmlNode field, out List<XmlNode> displays, out List<XmlNode> saves)
    {
        displays = null;
        saves = null;
        XmlNode displayItems = null;
        XmlNode saveItems = null;
        foreach (XmlNode child in field.ChildNodes)
        {
            if (child.LocalName != "items")
                continue;
            if (child.Attributes?["save"]?.Value == "1")
                saveItems = child;
            else if (displayItems == null)
                displayItems = child;
        }

        if (displayItems == null || saveItems == null)
            return false;

        displays = TextChildren(displayItems);
        saves = TextChildren(saveItems);
        return displays.Count > 0 && displays.Count == saves.Count;
    }

    private static List<XmlNode> TextChildren(XmlNode items)
    {
        var list = new List<XmlNode>();
        foreach (XmlNode child in items.ChildNodes)
        {
            if (child.LocalName == "text")
                list.Add(child);
        }

        return list;
    }

    private static int IndexOfDisplay(List<XmlNode> displays, string localizationKey)
    {
        for (var i = 0; i < displays.Count; i++)
        {
            if (DisplayMatches(displays[i].InnerText, localizationKey))
                return i;
        }

        return -1;
    }

    private static bool DisplayMatches(string display, string localizationKey)
    {
        if (string.IsNullOrWhiteSpace(display) || string.IsNullOrWhiteSpace(localizationKey))
            return false;

        var text = display.Trim();
        return text.StartsWith(localizationKey + " ", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith(localizationKey + "-", StringComparison.OrdinalIgnoreCase);
    }

    private static string DisplayPrefix(string display)
    {
        if (string.IsNullOrWhiteSpace(display))
            return null;

        var text = display.Trim();
        var split = text.IndexOf(' ');
        var dash = text.IndexOf('-');
        var cut = split < 0 ? dash : dash < 0 ? split : Math.Min(split, dash);
        return cut <= 0 ? text : text[..cut].Trim();
    }
}
