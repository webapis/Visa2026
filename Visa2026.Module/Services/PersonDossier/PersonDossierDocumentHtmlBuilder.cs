using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Services.PersonDossier;

/// <summary>
/// Renders a <see cref="PersonDossierSnapshot"/> as print-oriented HTML for the director hand-over
/// document (see <c>docs/PERSON_DOSSIER.md</c> phase 4).
/// </summary>
/// <remarks>
/// The output is consumed by <see cref="PersonDossierPdfBuilder"/> through DevExpress
/// <c>RichEditDocumentServer</c>, whose HTML importer supports only a narrow subset of CSS. Layout
/// therefore relies on tables and inline styles rather than flex/grid, and colors are light-theme
/// literals instead of the CSS custom properties the on-screen dossier uses.
/// </remarks>
public static class PersonDossierDocumentHtmlBuilder
{
    private const string InkColor = "#1a1a1a";
    private const string MutedColor = "#666666";
    private const string RuleColor = "#cccccc";
    private const string HeadBackground = "#f2f4f7";

    /// <summary>
    /// Full HTML document for RichEdit PDF conversion. Prefer
    /// <see cref="BuildFragment"/> for the on-screen Paper preview (no nested html/body).
    /// </summary>
    public static string Build(PersonDossierSnapshot snapshot, string? cultureName)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var html = new StringBuilder(16 * 1024);
        html.Append("<html><head><meta charset=\"utf-8\" /></head>");
        html.Append(CultureInfo.InvariantCulture, $"<body style=\"font-family:'Segoe UI',Arial,sans-serif;font-size:9pt;color:{InkColor};\">");
        html.Append(BuildFragment(snapshot, cultureName));
        html.Append("</body></html>");
        return html.ToString();
    }

    /// <summary>
    /// Inner print markup only — same content as the PDF, safe to host inside the dossier's
    /// A4 paper chrome via <c>MarkupString</c>.
    /// </summary>
    public static string BuildFragment(PersonDossierSnapshot snapshot, string? cultureName)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var html = new StringBuilder(16 * 1024);
        AppendTitle(html, snapshot, cultureName);
        AppendIdentity(html, snapshot, cultureName);
        AppendStatusTiles(html, snapshot);
        AppendSections(html, snapshot, cultureName);
        AppendFooter(html, cultureName);
        return html.ToString();
    }

    private static void AppendTitle(StringBuilder html, PersonDossierSnapshot snapshot, string? culture)
    {
        html.Append(CultureInfo.InvariantCulture,
            $"<p style=\"font-size:15pt;font-weight:bold;margin:0 0 2pt 0;\">{Enc(Msg("PersonDossier.Title", culture))}</p>");
        html.Append(CultureInfo.InvariantCulture,
            $"<p style=\"font-size:8pt;color:{MutedColor};margin:0 0 10pt 0;border-bottom:1px solid {RuleColor};padding-bottom:6pt;\">{Enc(snapshot.PersonDisplayName)}</p>");
    }

    private static void AppendIdentity(StringBuilder html, PersonDossierSnapshot snapshot, string? culture)
    {
        html.Append("<table style=\"width:100%;border-collapse:collapse;margin-bottom:10pt;\"><tr>");

        html.Append("<td style=\"width:120px;vertical-align:top;padding:0 12pt 0 0;\">");
        if (!string.IsNullOrEmpty(snapshot.PhotoDataUri))
        {
            html.Append(CultureInfo.InvariantCulture,
                $"<img src=\"{snapshot.PhotoDataUri}\" width=\"113\" height=\"151\" />");
        }
        else
        {
            html.Append(CultureInfo.InvariantCulture,
                $"<div style=\"width:113px;height:151px;border:1px solid {RuleColor};color:{MutedColor};font-size:8pt;text-align:center;\">{Enc(Msg("PersonDossier.Chrome.NoPhoto", culture))}</div>");
        }
        html.Append("</td>");

        html.Append("<td style=\"vertical-align:top;\">");
        html.Append(CultureInfo.InvariantCulture,
            $"<p style=\"font-size:13pt;font-weight:bold;margin:0 0 4pt 0;\">{Enc(snapshot.PersonDisplayName)}</p>");

        var badges = new List<string>();
        if (!string.IsNullOrWhiteSpace(snapshot.PersonRoleLabel))
            badges.Add(snapshot.PersonRoleLabel);
        if (!string.IsNullOrWhiteSpace(snapshot.ProjectContractName))
            badges.Add(snapshot.ProjectContractName);
        if (snapshot.IsArchived)
            badges.Add(Msg("PersonDossier.Status.Archived", culture));

        if (badges.Count > 0)
        {
            html.Append(CultureInfo.InvariantCulture,
                $"<p style=\"font-size:8pt;color:{MutedColor};margin:0 0 6pt 0;\">{Enc(string.Join("  \u00b7  ", badges))}</p>");
        }

        AppendIdentityFields(html, snapshot);
        html.Append("</td></tr></table>");
    }

    private static void AppendIdentityFields(StringBuilder html, PersonDossierSnapshot snapshot)
    {
        if (snapshot.IdentityFields.Count == 0)
            return;

        html.Append("<table style=\"width:100%;border-collapse:collapse;\">");
        for (int i = 0; i < snapshot.IdentityFields.Count; i += 2)
        {
            html.Append("<tr>");
            AppendIdentityFieldCells(html, snapshot.IdentityFields[i]);
            if (i + 1 < snapshot.IdentityFields.Count)
                AppendIdentityFieldCells(html, snapshot.IdentityFields[i + 1]);
            else
                html.Append("<td></td><td></td>");
            html.Append("</tr>");
        }
        html.Append("</table>");
    }

    private static void AppendIdentityFieldCells(StringBuilder html, PersonDossierField field)
    {
        html.Append(CultureInfo.InvariantCulture,
            $"<td style=\"width:15%;font-size:8pt;color:{MutedColor};padding:1pt 4pt 1pt 0;vertical-align:top;\">{Enc(field.Label)}</td>");
        html.Append(CultureInfo.InvariantCulture,
            $"<td style=\"width:35%;font-size:8pt;font-weight:bold;padding:1pt 8pt 1pt 0;vertical-align:top;\">{Enc(field.Value)}</td>");
    }

    private static void AppendStatusTiles(StringBuilder html, PersonDossierSnapshot snapshot)
    {
        if (snapshot.StatusTiles.Count == 0)
            return;

        html.Append("<table style=\"width:100%;border-collapse:collapse;margin-bottom:10pt;\"><tr>");
        foreach (var tile in snapshot.StatusTiles)
        {
            html.Append(CultureInfo.InvariantCulture,
                $"<td style=\"border:1px solid {RuleColor};padding:5pt;vertical-align:top;\">");
            html.Append(CultureInfo.InvariantCulture,
                $"<div style=\"font-size:7pt;color:{MutedColor};\">{Enc(tile.Label)}</div>");
            html.Append(CultureInfo.InvariantCulture,
                $"<div style=\"font-size:10pt;font-weight:bold;\">{Enc(string.IsNullOrWhiteSpace(tile.Value) ? "-" : tile.Value)}</div>");
            if (!string.IsNullOrWhiteSpace(tile.StatusLabel))
                AppendPill(html, tile.StatusLabel, tile.StatusCssClass);
            html.Append("</td>");
        }
        html.Append("</tr></table>");
    }

    private static void AppendSections(StringBuilder html, PersonDossierSnapshot snapshot, string? culture)
    {
        string statusHeader = Msg("PersonDossier.Column.Status", culture);

        foreach (var section in snapshot.Sections)
        {
            html.Append(CultureInfo.InvariantCulture,
                $"<p style=\"font-size:9pt;font-weight:bold;margin:8pt 0 3pt 0;\">{Enc(section.SectionLabel)} ({section.Records.Count})</p>");

            html.Append(CultureInfo.InvariantCulture,
                $"<table style=\"width:100%;border-collapse:collapse;border:1px solid {RuleColor};\">");

            html.Append(CultureInfo.InvariantCulture, $"<tr style=\"background-color:{HeadBackground};\">");
            foreach (var header in section.ColumnHeaders)
                AppendHeaderCell(html, header);
            AppendHeaderCell(html, statusHeader);
            html.Append("</tr>");

            foreach (var record in section.Records)
            {
                html.Append("<tr>");
                foreach (var cell in record.Cells)
                {
                    html.Append(CultureInfo.InvariantCulture,
                        $"<td style=\"border:1px solid {RuleColor};padding:3pt;font-size:8pt;vertical-align:top;\">{Enc(cell)}</td>");
                }

                html.Append(CultureInfo.InvariantCulture,
                    $"<td style=\"border:1px solid {RuleColor};padding:3pt;font-size:8pt;vertical-align:top;\">");
                if (record.IsCurrent)
                    AppendPill(html, Msg("PersonDossier.Status.Current", culture), "st-approved");
                if (!string.IsNullOrWhiteSpace(record.StatusLabel))
                    AppendPill(html, record.StatusLabel, record.StatusCssClass);
                html.Append("</td></tr>");
            }

            html.Append("</table>");
        }
    }

    private static void AppendHeaderCell(StringBuilder html, string caption)
    {
        html.Append(CultureInfo.InvariantCulture,
            $"<td style=\"border:1px solid {RuleColor};padding:3pt;font-size:7pt;font-weight:bold;color:{MutedColor};\">{Enc(caption)}</td>");
    }

    private static void AppendFooter(StringBuilder html, string? culture)
    {
        string generated = VisaUiMessages.FormatForCulture(
            culture,
            "PersonDossier.Export.GeneratedOn",
            DateTime.Now.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture));

        html.Append(CultureInfo.InvariantCulture,
            $"<p style=\"font-size:7pt;color:{MutedColor};margin-top:12pt;border-top:1px solid {RuleColor};padding-top:4pt;\">{Enc(generated)}</p>");
    }

    private static void AppendPill(StringBuilder html, string label, string cssClass)
    {
        (string background, string foreground) = PillColors(cssClass);
        html.Append(CultureInfo.InvariantCulture,
            $"<span style=\"background-color:{background};color:{foreground};font-size:7pt;padding:1pt 4pt;\">{Enc(label)}</span> ");
    }

    /// <summary>Print-safe equivalents of the <c>st-*</c> status vocabulary shared with the Report Dashboard.</summary>
    private static (string Background, string Foreground) PillColors(string cssClass) => cssClass switch
    {
        "st-approved" => ("#e3f4e6", "#1b6b2c"),
        "st-pending" => ("#fdf1dc", "#8a5a06"),
        "st-expiring" => ("#fbe3e3", "#a12020"),
        _ => ("#eeeeee", MutedColor)
    };

    private static string Msg(string key, string? culture) => VisaUiMessages.Get(key, culture);

    private static string Enc(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
