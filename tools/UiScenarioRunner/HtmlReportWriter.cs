using System.Net;
using System.Text;

namespace Visa2026.Tools.UiScenarioRunner;

internal static class HtmlReportWriter
{
    public static void Write(string path, IReadOnlyList<ScenarioRunResult> results, RunReportMetadata metadata)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string reportDir = directory ?? Directory.GetCurrentDirectory();
        int passed = results.Count(r => r.Ok);
        int failed = results.Count(r => !r.Ok);
        bool allOk = failed == 0;

        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"en\">");
        html.AppendLine("<head>");
        html.AppendLine("  <meta charset=\"utf-8\" />");
        html.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        html.AppendLine("  <title>Visa2026 UI Scenario Report</title>");
        html.AppendLine("  <style>");
        html.AppendLine("    body { font-family: Segoe UI, system-ui, sans-serif; margin: 24px; color: #1f2328; background: #f6f8fa; }");
        html.AppendLine("    .card { background: #fff; border: 1px solid #d0d7de; border-radius: 8px; padding: 16px 20px; margin-bottom: 16px; }");
        html.AppendLine("    h1 { margin: 0 0 8px; font-size: 1.5rem; }");
        html.AppendLine("    .badge { display: inline-block; padding: 4px 10px; border-radius: 999px; font-weight: 600; font-size: 0.85rem; }");
        html.AppendLine("    .badge-pass { background: #dafbe1; color: #116329; }");
        html.AppendLine("    .badge-fail { background: #ffebe9; color: #82071e; }");
        html.AppendLine("    table { width: 100%; border-collapse: collapse; font-size: 0.92rem; }");
        html.AppendLine("    th, td { border-bottom: 1px solid #d0d7de; padding: 8px 10px; text-align: left; vertical-align: top; }");
        html.AppendLine("    th { background: #f6f8fa; }");
        html.AppendLine("    .pass { color: #116329; font-weight: 600; }");
        html.AppendLine("    .fail { color: #82071e; font-weight: 600; }");
        html.AppendLine("    .meta { color: #57606a; font-size: 0.9rem; }");
        html.AppendLine("    img.thumb { max-width: 240px; border: 1px solid #d0d7de; border-radius: 4px; margin-top: 6px; }");
        html.AppendLine("    code { background: #f6f8fa; padding: 1px 4px; border-radius: 4px; }");
        html.AppendLine("  </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");

        html.AppendLine("  <div class=\"card\">");
        html.AppendLine("    <h1>Visa2026 UI Scenario Report</h1>");
        html.AppendLine($"    <p><span class=\"badge {(allOk ? "badge-pass" : "badge-fail")}\">{(allOk ? "PASSED" : "FAILED")}</span></p>");
        html.AppendLine("    <p class=\"meta\">");
        html.AppendLine($"      Version: <code>{Encode(metadata.ApplicationVersion ?? "unknown")}</code><br />");
        html.AppendLine($"      Base URL: <code>{Encode(metadata.BaseUrl ?? "-")}</code><br />");
        html.AppendLine($"      Started: {metadata.StartedAt:u}<br />");
        html.AppendLine($"      Finished: {metadata.FinishedAt:u}<br />");
        if (!string.IsNullOrWhiteSpace(metadata.GitSha))
        {
            html.AppendLine($"      Git: <code>{Encode(metadata.GitSha[..Math.Min(7, metadata.GitSha.Length)])}</code> {Encode(metadata.GitRef ?? string.Empty)}<br />");
        }

        if (!string.IsNullOrWhiteSpace(metadata.WorkflowRunUrl))
        {
            html.AppendLine($"      CI run: <a href=\"{EncodeAttr(metadata.WorkflowRunUrl)}\">{Encode(metadata.WorkflowRunUrl)}</a><br />");
        }

        html.AppendLine($"      Summary: {passed} passed, {failed} failed, {results.Count} total");
        html.AppendLine("    </p>");
        html.AppendLine("  </div>");

        foreach (ScenarioRunResult scenario in results)
        {
            html.AppendLine("  <div class=\"card\">");
            html.AppendLine($"    <h2>{Encode(scenario.ScenarioId)} <span class=\"{(scenario.Ok ? "pass" : "fail")}\">{(scenario.Ok ? "PASS" : "FAIL")}</span></h2>");
            if (!string.IsNullOrWhiteSpace(scenario.Error))
            {
                html.AppendLine($"    <p class=\"fail\">{Encode(scenario.Error)}</p>");
            }

            html.AppendLine("    <table>");
            html.AppendLine("      <thead><tr><th>#</th><th>Step</th><th>Detail</th><th>Status</th><th>Error</th></tr></thead>");
            html.AppendLine("      <tbody>");
            foreach (StepResult step in scenario.Steps)
            {
                html.AppendLine("        <tr>");
                html.AppendLine($"          <td>{step.Index}</td>");
                html.AppendLine($"          <td><code>{Encode(step.StepKind)}</code></td>");
                html.AppendLine($"          <td>{Encode(step.Detail ?? string.Empty)}</td>");
                html.AppendLine($"          <td class=\"{(step.Ok ? "pass" : "fail")}\">{(step.Ok ? "PASS" : "FAIL")}</td>");
                html.AppendLine($"          <td>{Encode(step.Error ?? string.Empty)}</td>");
                html.AppendLine("        </tr>");
            }

            html.AppendLine("      </tbody>");
            html.AppendLine("    </table>");

            AppendScreenshot(html, reportDir, scenario.ScenarioId, "failure");
            AppendScreenshot(html, reportDir, scenario.ScenarioId, "before-save");
            AppendScreenshot(html, reportDir, scenario.ScenarioId, "after-save");

            html.AppendLine("  </div>");
        }

        html.AppendLine("</body>");
        html.AppendLine("</html>");
        File.WriteAllText(path, html.ToString(), Encoding.UTF8);
    }

    private static void AppendScreenshot(StringBuilder html, string reportDir, string scenarioId, string suffix)
    {
        string fileName = $"{scenarioId}-{suffix}.png";
        string absolute = Path.Combine(reportDir, "screenshots", fileName);
        if (!File.Exists(absolute))
        {
            return;
        }

        string relative = Path.Combine("screenshots", fileName).Replace('\\', '/');
        html.AppendLine($"    <p><strong>{Encode(suffix)}</strong><br /><img class=\"thumb\" src=\"{EncodeAttr(relative)}\" alt=\"{EncodeAttr(fileName)}\" /></p>");
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private static string EncodeAttr(string value) => WebUtility.HtmlEncode(value);
}
