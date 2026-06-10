using System.Text;
using System.Xml;

namespace Visa2026.Tools.UiScenarioRunner;

internal static class JunitReportWriter
{
    public static void Write(string path, IReadOnlyList<ScenarioRunResult> results, RunReportMetadata metadata)
    {
        int failures = results.Count(r => !r.Ok);
        int tests = results.Sum(r => Math.Max(1, r.Steps.Count));
        double seconds = Math.Max(0.001, (metadata.FinishedAt - metadata.StartedAt).TotalSeconds);

        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        };

        using var writer = XmlWriter.Create(path, settings);
        writer.WriteStartDocument();
        writer.WriteStartElement("testsuites");
        writer.WriteAttributeString("name", "Visa2026 UI Scenarios");
        writer.WriteAttributeString("tests", tests.ToString());
        writer.WriteAttributeString("failures", failures.ToString());
        writer.WriteAttributeString("time", seconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));

        foreach (ScenarioRunResult scenario in results)
        {
            WriteSuite(writer, scenario, metadata);
        }

        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteSuite(XmlWriter writer, ScenarioRunResult scenario, RunReportMetadata metadata)
    {
        IReadOnlyList<StepResult> steps = scenario.Steps.Count > 0
            ? scenario.Steps
            : [new StepResult(1, "scenario", scenario.Ok, scenario.ScenarioId, scenario.Error)];

        int failures = steps.Count(s => !s.Ok);
        writer.WriteStartElement("testsuite");
        writer.WriteAttributeString("name", scenario.ScenarioId);
        writer.WriteAttributeString("tests", steps.Count.ToString());
        writer.WriteAttributeString("failures", failures.ToString());
        writer.WriteAttributeString("errors", "0");
        writer.WriteAttributeString("skipped", "0");

        foreach (StepResult step in steps)
        {
            string name = BuildTestName(step);
            writer.WriteStartElement("testcase");
            writer.WriteAttributeString("classname", scenario.ScenarioId);
            writer.WriteAttributeString("name", name);

            if (!step.Ok)
            {
                writer.WriteStartElement("failure");
                writer.WriteAttributeString("message", XmlEscape(step.Error ?? scenario.Error ?? "failed"));
                writer.WriteString(XmlEscape(step.Error ?? scenario.Error ?? "Step failed."));
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        if (!scenario.Ok && failures == 0)
        {
            writer.WriteStartElement("testcase");
            writer.WriteAttributeString("classname", scenario.ScenarioId);
            writer.WriteAttributeString("name", "scenario");
            writer.WriteStartElement("failure");
            writer.WriteAttributeString("message", XmlEscape(scenario.Error ?? "failed"));
            writer.WriteString(XmlEscape(scenario.Error ?? "Scenario failed."));
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static string BuildTestName(StepResult step)
    {
        var parts = new List<string> { $"step-{step.Index}", step.StepKind };
        if (!string.IsNullOrWhiteSpace(step.Detail))
        {
            parts.Add(step.Detail);
        }

        return string.Join(' ', parts);
    }

    private static string XmlEscape(string? value) =>
        System.Security.SecurityElement.Escape(value ?? string.Empty) ?? string.Empty;
}
