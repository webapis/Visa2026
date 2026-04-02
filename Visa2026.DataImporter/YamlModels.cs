namespace Visa2026.DataImporter;

/// <summary>Root of data.yaml — contains the ordered list of scenarios.</summary>
public class YamlRoot
{
    public List<YamlScenario> Scenarios { get; set; } = new();
}

/// <summary>
/// A single scenario entry in data.yaml.
///
/// Example:
///   - order: 1
///     name: InvitationScenario
///     description: Person A full invitation flow
///     dependsOn: Shared
///     anchor:
///       entity: Person
///       key: Email
///       value: john.doe@company.com
///     data:
///       Persons:
///         - "First Name": John
///           "Last Name": Doe
///           Email: john.doe@company.com
/// </summary>
public class YamlScenario
{
    public int Order { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string DependsOn { get; set; } = "";
    public YamlAnchor? Anchor { get; set; }

    /// <summary>
    /// Sheet name → list of rows. Each row is a dictionary of column header → cell value.
    /// Sheet names must match ExcelMappings.Sheets[].SheetName (e.g. "Persons", "Passports").
    /// </summary>
    public Dictionary<string, List<Dictionary<string, string>>>? Data { get; set; }
}

/// <summary>
/// Idempotency anchor: if a record matching Entity/Key/Value exists, the scenario is skipped.
/// </summary>
public class YamlAnchor
{
    public string Entity { get; set; } = "";
    public string Key    { get; set; } = "";
    public string Value  { get; set; } = "";
}
