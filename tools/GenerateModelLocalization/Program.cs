using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;

const string Cultures = "tr-TR,tk-TM,ru-RU";
const string BoPrefix = "Visa2026.Module.BusinessObjects.";

string repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
string toolsDir = Path.Combine(repoRoot, "tools", "GenerateModelLocalization");
string baseJsonPath = Path.Combine(repoRoot, "Visa2026.Module", "Localization", "UiStrings.json");
string entitiesJsonPath = Path.Combine(toolsDir, "UiStrings.entities.json");
string viewsA4JsonPath = Path.Combine(toolsDir, "UiStrings.views-a4.json");
string logonJsonPath = Path.Combine(toolsDir, "UiStrings.logon.json");
string personDetailJsonPath = Path.Combine(toolsDir, "UiStrings.person-detail.json");
string moduleDir = Path.Combine(repoRoot, "Visa2026.Module");
string blazorDir = Path.Combine(repoRoot, "Visa2026.Blazor.Server");

JsonObject merged = MergeSources(
    JsonNode.Parse(File.ReadAllText(baseJsonPath))!.AsObject(),
    JsonNode.Parse(File.ReadAllText(entitiesJsonPath))!,
    JsonNode.Parse(File.ReadAllText(viewsA4JsonPath))!.AsObject(),
    JsonNode.Parse(File.ReadAllText(logonJsonPath))!.AsObject(),
    JsonNode.Parse(File.ReadAllText(personDetailJsonPath))!.AsObject());

using JsonDocument doc = JsonDocument.Parse(merged.ToJsonString());
JsonElement root = doc.RootElement;

foreach (string culture in Cultures.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
{
    XElement moduleApplication = BuildApplication(root, culture, hostOnly: false);
    string modulePath = Path.Combine(moduleDir, $"Model.DesignedDiffs.Localization.{culture}.xafml");
    WriteXafml(modulePath, moduleApplication);
    Console.WriteLine($"Wrote {modulePath}");

    XElement blazorApplication = BuildBlazorApplication(root, culture);
    string blazorPath = Path.Combine(blazorDir, $"Model.{culture}.xafml");
    WriteXafml(blazorPath, blazorApplication);
    Console.WriteLine($"Wrote {blazorPath}");
}

static JsonObject MergeSources(JsonObject baseRoot, JsonNode entitiesRoot, JsonObject viewsA4, JsonObject logon, JsonObject personDetail)
{
    if (viewsA4["navigation"] is JsonObject navPatch)
    {
        MergeObject(baseRoot["navigation"]!.AsObject(), navPatch);
    }

    if (viewsA4["actions"] is JsonObject actionsPatch)
    {
        MergeObject(baseRoot["actions"]!.AsObject(), actionsPatch);
    }

    ExpandEntities(baseRoot, entitiesRoot);
    MergeViews(baseRoot["views"]!.AsObject(), viewsA4["views"]?.AsObject());

    if (viewsA4["classMembers"] is JsonObject classMembers)
    {
        MergeClassMembers(baseRoot["classes"]!.AsObject(), classMembers);
    }

    MergeClasses(baseRoot["classes"]!.AsObject(), logon["classes"]?.AsObject());
    MergeViews(baseRoot["views"]!.AsObject(), logon["views"]?.AsObject());

    if (personDetail["actions"] is JsonObject personActions)
    {
        MergeObject(baseRoot["actions"]!.AsObject(), personActions);
    }

    MergeClasses(baseRoot["classes"]!.AsObject(), personDetail["classes"]?.AsObject());
    MergeViews(baseRoot["views"]!.AsObject(), personDetail["views"]?.AsObject());
    MergeEnumMembers(baseRoot["classes"]!.AsObject(), personDetail["enums"]?.AsObject());

    return baseRoot;
}

static void MergeEnumMembers(JsonObject classes, JsonObject? enums)
{
    if (enums is null)
    {
        return;
    }

    foreach (KeyValuePair<string, JsonNode?> enumEntry in enums)
    {
        if (classes[enumEntry.Key] is not JsonObject classNode)
        {
            classNode = new JsonObject
            {
                ["caption"] = new JsonObject
                {
                    ["tr-TR"] = enumEntry.Key,
                    ["tk-TM"] = enumEntry.Key,
                    ["ru-RU"] = enumEntry.Key,
                },
                ["members"] = new JsonObject(),
            };
            classes[enumEntry.Key] = classNode;
        }

        if (classNode["members"] is not JsonObject members)
        {
            members = new JsonObject();
            classNode["members"] = members;
        }

        MergeObject(members, enumEntry.Value!.AsObject());
    }
}

static void MergeObject(JsonObject target, JsonObject patch)
{
    foreach (KeyValuePair<string, JsonNode?> entry in patch)
    {
        target[entry.Key] = entry.Value!.DeepClone();
    }
}

static void MergeViews(JsonObject targetViews, JsonObject? patchViews)
{
    if (patchViews is null)
    {
        return;
    }

    foreach (KeyValuePair<string, JsonNode?> entry in patchViews)
    {
        if (targetViews[entry.Key] is JsonObject existing)
        {
            MergeView(existing, entry.Value!.AsObject());
        }
        else
        {
            targetViews[entry.Key] = entry.Value!.DeepClone();
        }
    }
}

static void MergeView(JsonObject target, JsonObject patch)
{
    foreach (KeyValuePair<string, JsonNode?> entry in patch)
    {
        if (entry.Key is "columns" or "layoutGroups" or "items"
            && target[entry.Key] is JsonObject existingSection)
        {
            MergeObject(existingSection, entry.Value!.AsObject());
        }
        else
        {
            target[entry.Key] = entry.Value!.DeepClone();
        }
    }
}

static void MergeClasses(JsonObject classes, JsonObject? patchClasses)
{
    if (patchClasses is null)
    {
        return;
    }

    foreach (KeyValuePair<string, JsonNode?> classEntry in patchClasses)
    {
        JsonObject patchClass = classEntry.Value!.AsObject();
        if (classes[classEntry.Key] is not JsonObject classNode)
        {
            classes[classEntry.Key] = patchClass.DeepClone();
            continue;
        }

        if (patchClass["caption"] is JsonNode caption)
        {
            classNode["caption"] = caption.DeepClone();
        }

        if (patchClass["members"] is JsonObject patchMembers)
        {
            if (classNode["members"] is not JsonObject members)
            {
                members = new JsonObject();
                classNode["members"] = members;
            }

            MergeObject(members, patchMembers);
        }
    }
}

static void MergeClassMembers(JsonObject classes, JsonObject classMembers)
{
    foreach (KeyValuePair<string, JsonNode?> classEntry in classMembers)
    {
        if (classes[classEntry.Key] is not JsonObject classNode)
        {
            classNode = new JsonObject { ["members"] = new JsonObject() };
            classes[classEntry.Key] = classNode;
        }

        if (classNode["members"] is not JsonObject members)
        {
            members = new JsonObject();
            classNode["members"] = members;
        }

        MergeObject(members, classEntry.Value!.AsObject());
    }
}

static void ExpandEntities(JsonObject baseRoot, JsonNode entitiesRoot)
{
    JsonObject entities = entitiesRoot["entities"]!.AsObject();
    JsonObject commonMembers = entitiesRoot["commonMembers"]!.AsObject();
    JsonObject classes = baseRoot["classes"]!.AsObject();
    JsonObject views = baseRoot["views"]!.AsObject();

    foreach (KeyValuePair<string, JsonNode?> entityEntry in entities)
    {
        string typeName = entityEntry.Key;
        JsonObject entity = entityEntry.Value!.AsObject();
        string className = BoPrefix + typeName;

        if (classes[className] is null)
        {
            var classNode = new JsonObject
            {
                ["caption"] = entity["caption"]!.DeepClone(),
                ["members"] = new JsonObject(),
            };

            if (entity["members"] is JsonObject entityMembers)
            {
                MergeObject(classNode["members"]!.AsObject(), entityMembers);
            }

            if (entity["lookup"]?.GetValue<bool>() == true)
            {
                JsonObject members = classNode["members"]!.AsObject();
                foreach (KeyValuePair<string, JsonNode?> cm in commonMembers)
                {
                    if (!members.ContainsKey(cm.Key))
                    {
                        members[cm.Key] = cm.Value!.DeepClone();
                    }
                }
            }

            classes[className] = classNode;
        }

        string listViewId = $"{typeName}_ListView";
        if (views[listViewId] is null && entity["listCaption"] is JsonNode listCaption)
        {
            views[listViewId] = new JsonObject { ["caption"] = listCaption.DeepClone() };
        }

        string detailViewId = $"{typeName}_DetailView";
        if (views[detailViewId] is null && entity["caption"] is JsonNode detailCaption)
        {
            views[detailViewId] = new JsonObject { ["caption"] = detailCaption.DeepClone() };
        }
    }
}

static XElement BuildBlazorApplication(JsonElement root, string culture)
{
    var application = new XElement("Application");
    application.SetAttributeValue("Title", GetText(root.GetProperty("applicationTitle"), culture));

    var navItems = new XElement("NavigationItems", new XElement("Items"));
    JsonElement visaExtNav = root.GetProperty("navigation").GetProperty("VisaExt");
    var visaExtItem = new XElement("Item",
        new XAttribute("Id", "VisaExt"),
        new XAttribute("Caption", GetText(visaExtNav, culture)),
        new XElement("Items",
            new XElement("Item",
                new XAttribute("Id", "Ministry1"),
                new XAttribute("Caption", GetText(root.GetProperty("navigation").GetProperty("Ministry1"), culture))),
            new XElement("Item",
                new XAttribute("Id", "Ministry2"),
                new XAttribute("Caption", GetText(root.GetProperty("navigation").GetProperty("Ministry2"), culture)))));
    navItems.Element("Items")!.Add(visaExtItem);

    application.Add(navItems);
    return application;
}

static XElement BuildApplication(JsonElement root, string culture, bool hostOnly)
{
    var application = new XElement("Application");
    if (!hostOnly)
    {
        application.SetAttributeValue("Title", GetText(root.GetProperty("applicationTitle"), culture));
    }

    var actionDesign = new XElement("ActionDesign", new XElement("Actions"));
    foreach (JsonProperty action in root.GetProperty("actions").EnumerateObject())
    {
        actionDesign.Element("Actions")!.Add(new XElement("Action",
            new XAttribute("Id", action.Name),
            new XAttribute("Caption", GetText(action.Value, culture))));
    }

    application.Add(actionDesign);

    var navItems = new XElement("NavigationItems", new XElement("Items"));
    foreach (JsonProperty nav in root.GetProperty("navigation").EnumerateObject().OrderBy(n => n.Name))
    {
        if (nav.Name is "VisaExt" or "Ministry1" or "Ministry2")
        {
            continue;
        }

        navItems.Element("Items")!.Add(BuildNavigationItem(nav.Name, nav.Value, culture));
    }

    application.Add(navItems);

    var boModel = new XElement("BOModel");
    foreach (JsonProperty classEntry in root.GetProperty("classes").EnumerateObject().OrderBy(c => c.Name))
    {
        var classNode = new XElement("Class",
            new XAttribute("Name", classEntry.Name),
            new XAttribute("Caption", GetText(classEntry.Value.GetProperty("caption"), culture)));
        var ownMembers = new XElement("OwnMembers");
        foreach (JsonProperty member in classEntry.Value.GetProperty("members").EnumerateObject().OrderBy(m => m.Name))
        {
            ownMembers.Add(new XElement("Member",
                new XAttribute("Name", member.Name),
                new XAttribute("Caption", GetText(member.Value, culture))));
        }

        classNode.Add(ownMembers);
        boModel.Add(classNode);
    }

    application.Add(boModel);

    var views = new XElement("Views");
    foreach (JsonProperty view in root.GetProperty("views").EnumerateObject().OrderBy(v => v.Name))
    {
        bool isDetail = view.Name.EndsWith("_DetailView", StringComparison.Ordinal);
        var viewNode = new XElement(isDetail ? "DetailView" : "ListView",
            new XAttribute("Id", view.Name),
            new XAttribute("Caption", GetText(view.Value.GetProperty("caption"), culture)));

        if (view.Value.TryGetProperty("columns", out JsonElement columns))
        {
            var columnsNode = new XElement("Columns");
            foreach (JsonProperty column in columns.EnumerateObject().OrderBy(c => c.Name))
            {
                columnsNode.Add(new XElement("ColumnInfo",
                    new XAttribute("Id", column.Name),
                    new XAttribute("Caption", GetText(column.Value, culture))));
            }

            viewNode.Add(columnsNode);
        }

        if (view.Value.TryGetProperty("layoutGroups", out JsonElement layoutGroups))
        {
            var layout = new XElement("Layout",
                new XElement("LayoutGroup",
                    new XAttribute("Id", "Main"),
                    new XAttribute("RelativeSize", "100")));
            foreach (JsonProperty group in layoutGroups.EnumerateObject().OrderBy(g => g.Name))
            {
                layout.Element("LayoutGroup")!.Add(new XElement("LayoutGroup",
                    new XAttribute("Id", group.Name),
                    new XAttribute("Caption", GetText(group.Value, culture))));
            }

            viewNode.Add(layout);
        }

        if (view.Value.TryGetProperty("items", out JsonElement items))
        {
            var itemsNode = new XElement("Items");
            foreach (JsonProperty item in items.EnumerateObject().OrderBy(i => i.Name))
            {
                if (item.Value.TryGetProperty("text", out JsonElement text))
                {
                    itemsNode.Add(new XElement("StaticText",
                        new XAttribute("Id", item.Name),
                        new XAttribute("Text", GetText(text, culture))));
                }
                else if (item.Value.TryGetProperty("caption", out JsonElement itemCaption))
                {
                    itemsNode.Add(new XElement("PropertyEditor",
                        new XAttribute("Id", item.Name),
                        new XAttribute("Caption", GetText(itemCaption, culture))));
                }
            }

            if (itemsNode.HasElements)
            {
                viewNode.Add(itemsNode);
            }
        }

        views.Add(viewNode);
    }

    application.Add(views);
    return application;
}

static XElement BuildNavigationItem(string id, JsonElement node, string culture)
{
    if (node.TryGetProperty("children", out JsonElement children))
    {
        var group = new XElement("Item",
            new XAttribute("Id", id),
            new XAttribute("Caption", GetText(node.GetProperty("caption"), culture)),
            new XElement("Items"));
        foreach (JsonProperty child in children.EnumerateObject().OrderBy(c => c.Name))
        {
            group.Element("Items")!.Add(new XElement("Item",
                new XAttribute("Id", child.Name),
                new XAttribute("Caption", GetText(child.Value, culture))));
        }

        return group;
    }

    return new XElement("Item",
        new XAttribute("Id", id),
        new XAttribute("Caption", GetText(node, culture)));
}

static string GetText(JsonElement node, string culture) =>
    node.GetProperty(culture).GetString() ?? throw new InvalidOperationException($"Missing {culture} for translation.");

static void WriteXafml(string path, XElement application)
{
    var document = new XDocument(new XDeclaration("1.0", "utf-8", null), application);
    var settings = new System.Xml.XmlWriterSettings
    {
        Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
        Indent = true,
        OmitXmlDeclaration = false,
    };
    using var writer = System.Xml.XmlWriter.Create(path, settings);
    document.Save(writer);
}
