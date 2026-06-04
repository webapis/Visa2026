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
string securityJsonPath = Path.Combine(toolsDir, "UiStrings.security.json");
string personDetailJsonPath = Path.Combine(toolsDir, "UiStrings.person-detail.json");
string messagesJsonPath = Path.Combine(toolsDir, "UiStrings.messages.json");
string validationJsonPath = Path.Combine(toolsDir, "UiStrings.validation.json");
string validationTemplatesJsonPath = Path.Combine(toolsDir, "UiStrings.validation-templates.json");
string blazorLayoutsJsonPath = Path.Combine(toolsDir, "UiStrings.blazor-layouts.json");
string documentsViewsJsonPath = Path.Combine(toolsDir, "UiStrings.documents-views.json");
string documentCopiesJsonPath = Path.Combine(toolsDir, "UiStrings.document-copies.json");
string lookupEnumsJsonPath = Path.Combine(toolsDir, "UiStrings.lookup-enums.json");
string userFeedbackJsonPath = Path.Combine(toolsDir, "UiStrings.user-feedback.json");
string navigationPathsJsonPath = Path.Combine(toolsDir, "UiStrings.navigation-paths.json");
string moduleDir = Path.Combine(repoRoot, "Visa2026.Module");
string blazorDir = Path.Combine(repoRoot, "Visa2026.Blazor.Server");
string messageCatalogPath = Path.Combine(moduleDir, "Localization", "Generated", "VisaUiMessageCatalog.g.cs");

JsonNode entitiesRoot = JsonNode.Parse(File.ReadAllText(entitiesJsonPath))!;
JsonObject blazorLayoutsRoot = JsonNode.Parse(File.ReadAllText(blazorLayoutsJsonPath))!.AsObject();
JsonObject merged = MergeSources(
    JsonNode.Parse(File.ReadAllText(baseJsonPath))!.AsObject(),
    entitiesRoot,
    JsonNode.Parse(File.ReadAllText(viewsA4JsonPath))!.AsObject(),
    JsonNode.Parse(File.ReadAllText(logonJsonPath))!.AsObject(),
    JsonNode.Parse(File.ReadAllText(personDetailJsonPath))!.AsObject());
JsonObject securityRoot = JsonNode.Parse(File.ReadAllText(securityJsonPath))!.AsObject();
MergeClasses(merged["classes"]!.AsObject(), securityRoot["classes"]?.AsObject());
MergeViews(merged["views"]!.AsObject(), securityRoot["views"]?.AsObject());
merged = MergeBlazorLayoutViews(merged, blazorLayoutsRoot);
JsonObject documentsViewsRoot = JsonNode.Parse(File.ReadAllText(documentsViewsJsonPath))!.AsObject();
MergeViews(merged["views"]!.AsObject(), documentsViewsRoot["views"]?.AsObject());
MergeClassMembers(merged["classes"]!.AsObject(), documentsViewsRoot["classMembers"]?.AsObject());
JsonObject lookupEnumsRoot = JsonNode.Parse(File.ReadAllText(lookupEnumsJsonPath))!.AsObject();
JsonObject userFeedbackRoot = JsonNode.Parse(File.ReadAllText(userFeedbackJsonPath))!.AsObject();
JsonObject navigationPathsRoot = JsonNode.Parse(File.ReadAllText(navigationPathsJsonPath))!.AsObject();
JsonObject mergedEnums = new JsonObject();
MergeObject(mergedEnums, JsonNode.Parse(File.ReadAllText(personDetailJsonPath))!["enums"]?.AsObject());
MergeObject(mergedEnums, lookupEnumsRoot["enums"]?.AsObject());
MergeObject(mergedEnums, userFeedbackRoot["enums"]?.AsObject());
MergeClasses(merged["classes"]!.AsObject(), userFeedbackRoot["classes"]?.AsObject());
MergeViews(merged["views"]!.AsObject(), userFeedbackRoot["views"]?.AsObject());
MergeObject(merged["actions"]!.AsObject(), userFeedbackRoot["actions"]?.AsObject());
MergeEnumMembers(merged["classes"]!.AsObject(), mergedEnums);
MergeClassMembers(merged["classes"]!.AsObject(), lookupEnumsRoot["classMembers"]?.AsObject());
JsonObject documentCopiesRoot = JsonNode.Parse(File.ReadAllText(documentCopiesJsonPath))!.AsObject();
MergeClassMembers(merged["classes"]!.AsObject(), documentCopiesRoot["classMembers"]?.AsObject());
MergeViews(merged["views"]!.AsObject(), documentCopiesRoot["views"]?.AsObject());
UpdateBaseModelEnumCaptions(moduleDir, mergedEnums);
UpdateBaseModelEnumLocalization(moduleDir, mergedEnums);

JsonObject messagesRoot = JsonNode.Parse(File.ReadAllText(messagesJsonPath))!.AsObject();
JsonObject validationRoot = JsonNode.Parse(File.ReadAllText(validationJsonPath))!.AsObject();
JsonObject validationTemplatesRoot = JsonNode.Parse(File.ReadAllText(validationTemplatesJsonPath))!.AsObject();
string[] blazorLayoutDetailViews = blazorLayoutsRoot["detailViewsWithHostLayout"]!.AsArray()
    .Select(n => n!.GetValue<string>()).ToArray();
WriteMessageCatalog(messageCatalogPath, messagesRoot);
Console.WriteLine($"Wrote {messageCatalogPath}");

using JsonDocument doc = JsonDocument.Parse(merged.ToJsonString());
JsonElement root = doc.RootElement;

foreach (string culture in Cultures.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
{
    XElement moduleApplication = BuildApplication(root, culture, hostOnly: false, validationRoot, validationTemplatesRoot, navigationPathsRoot, mergedEnums);
    string modulePath = Path.Combine(moduleDir, $"Model.DesignedDiffs.Localization.{culture}.xafml");
    WriteXafml(modulePath, moduleApplication);
    Console.WriteLine($"Wrote {modulePath}");

    XElement blazorApplication = BuildBlazorApplication(root, culture, validationTemplatesRoot, blazorLayoutDetailViews);
    string blazorPath = Path.Combine(blazorDir, $"Model.{culture}.xafml");
    WriteXafml(blazorPath, blazorApplication);
    Console.WriteLine($"Wrote {blazorPath}");
}

static JsonObject MergeSources(JsonObject baseRoot, JsonNode entitiesNode, JsonObject viewsA4, JsonObject logon, JsonObject personDetail)
{
    if (viewsA4["navigation"] is JsonObject navPatch)
    {
        MergeObject(baseRoot["navigation"]!.AsObject(), navPatch);
    }

    if (viewsA4["actions"] is JsonObject actionsPatch)
    {
        MergeObject(baseRoot["actions"]!.AsObject(), actionsPatch);
    }

    ExpandEntities(baseRoot, entitiesNode);
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
    MergeSoftDeleteMemberCaptions(baseRoot["classes"]!.AsObject(), entitiesNode["commonMembers"]!.AsObject());

    return baseRoot;
}

static JsonObject MergeBlazorLayoutViews(JsonObject mergedRoot, JsonObject blazorLayoutsRoot)
{
    if (blazorLayoutsRoot["views"] is JsonObject blazorViews)
    {
        MergeViews(mergedRoot["views"]!.AsObject(), blazorViews);
    }

    return mergedRoot;
}

static void MergeSoftDeleteMemberCaptions(JsonObject classes, JsonObject commonMembers)
{
    string[] keys = ["DateDeleted", "DeletedBy"];
    foreach (KeyValuePair<string, JsonNode?> classEntry in classes)
    {
        if (classEntry.Value is not JsonObject classNode)
        {
            continue;
        }

        if (classNode["members"] is not JsonObject members)
        {
            members = new JsonObject();
            classNode["members"] = members;
        }

        foreach (string key in keys)
        {
            if (commonMembers[key] is JsonObject caption && !members.ContainsKey(key))
            {
                members[key] = caption.DeepClone();
            }
        }
    }
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

        foreach (KeyValuePair<string, JsonNode?> memberEntry in enumEntry.Value!.AsObject())
        {
            if (memberEntry.Key == "caption" && memberEntry.Value is JsonObject captionNode)
            {
                classNode["caption"] = captionNode.DeepClone();
                continue;
            }

            members[memberEntry.Key] = memberEntry.Value!.DeepClone();
        }
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
        else if (entry.Key == "nestedLayoutGroups")
        {
            target[entry.Key] = entry.Value!.DeepClone();
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
            classes[className] = new JsonObject
            {
                ["caption"] = entity["caption"]!.DeepClone(),
                ["members"] = new JsonObject(),
            };
        }

        JsonObject classNode = classes[className]!.AsObject();
        if (entity["members"] is JsonObject entityMembers)
        {
            JsonObject members = classNode["members"] as JsonObject ?? new JsonObject();
            if (classNode["members"] is null)
            {
                classNode["members"] = members;
            }

            MergeObject(members, entityMembers);
        }

        if (entity["lookup"]?.GetValue<bool>() == true && classNode["members"] is JsonObject lookupMembers)
        {
            foreach (KeyValuePair<string, JsonNode?> cm in commonMembers)
            {
                if (!lookupMembers.ContainsKey(cm.Key))
                {
                    lookupMembers[cm.Key] = cm.Value!.DeepClone();
                }
            }
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

static XElement BuildBlazorApplication(
    JsonElement root,
    string culture,
    JsonObject validationTemplatesRoot,
    string[] blazorLayoutDetailViews)
{
    var application = new XElement("Application");
    application.SetAttributeValue("Title", GetText(root.GetProperty("applicationTitle"), culture));

    var actionDesign = new XElement("ActionDesign", new XElement("Actions"));
    foreach (JsonProperty action in root.GetProperty("actions").EnumerateObject().OrderBy(a => a.Name))
    {
        actionDesign.Element("Actions")!.Add(new XElement("Action",
            new XAttribute("Id", action.Name),
            new XAttribute("Caption", GetText(action.Value, culture))));
    }

    application.Add(actionDesign);
    AppendBlazorShellLocalization(application, validationTemplatesRoot, culture);
    AppendBlazorHostViewLayouts(application, root, culture, blazorLayoutDetailViews);

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

static XElement BuildApplication(
    JsonElement root,
    string culture,
    bool hostOnly,
    JsonObject validationRoot,
    JsonObject validationTemplatesRoot,
    JsonObject navigationPathsRoot,
    JsonObject mergedEnums)
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

    if (navigationPathsRoot["paths"] is JsonObject paths)
    {
        MergeNavigationPaths(navItems.Element("Items")!, paths, culture);
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

        if (TryBuildViewLayout(view.Value, culture, out XElement? layout) && layout is not null)
        {
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
    AppendValidation(application, validationRoot, validationTemplatesRoot, culture);
    AppendApplicationLocalization(application, validationTemplatesRoot, mergedEnums, culture);
    return application;
}

static void AppendValidation(XElement application, JsonObject validationRoot, JsonObject validationTemplatesRoot, string culture)
{
    var validationNode = new XElement("Validation");
    AppendErrorMessageTemplates(validationNode, validationTemplatesRoot, culture);

    var rulesNode = new XElement("Rules");
    foreach (KeyValuePair<string, JsonNode?> ruleEntry in validationRoot.OrderBy(r => r.Key))
    {
        JsonObject rule = ruleEntry.Value!.AsObject();
        string ruleElementName = rule["rule"]?.GetValue<string>() ?? "RuleCriteria";
        string targetType = BoPrefix + rule["targetType"]!.GetValue<string>();
        rulesNode.Add(new XElement(ruleElementName,
            new XAttribute("Id", ruleEntry.Key),
            new XAttribute("TargetType", targetType),
            new XAttribute("CustomMessageTemplate", GetLocalizedText(rule, culture))));
    }

    validationNode.Add(rulesNode);
    application.Add(validationNode);
}

static void AppendErrorMessageTemplates(XElement validationNode, JsonObject validationTemplatesRoot, string culture)
{
    if (validationTemplatesRoot["errorMessageTemplates"] is not JsonObject templates)
    {
        return;
    }

    var templatesNode = new XElement("ErrorMessageTemplates");
    foreach (KeyValuePair<string, JsonNode?> ruleTypeEntry in templates.OrderBy(e => e.Key))
    {
        JsonObject properties = ruleTypeEntry.Value!.AsObject();
        var ruleTypeNode = new XElement(ruleTypeEntry.Key);
        foreach (KeyValuePair<string, JsonNode?> propertyEntry in properties.OrderBy(p => p.Key))
        {
            ruleTypeNode.SetAttributeValue(propertyEntry.Key, GetLocalizedText(propertyEntry.Value!.AsObject(), culture));
        }

        templatesNode.Add(ruleTypeNode);
    }

    validationNode.Add(templatesNode);
}

static void AppendApplicationLocalization(
    XElement application,
    JsonObject validationTemplatesRoot,
    JsonObject mergedEnums,
    string culture)
{
    var localization = new XElement("Localization");

    if (validationTemplatesRoot["userVisibleExceptions"] is JsonObject exceptions)
    {
        var validationNode = new XElement("Validation");
        foreach (KeyValuePair<string, JsonNode?> propertyEntry in exceptions.OrderBy(e => e.Key))
        {
            validationNode.SetAttributeValue(propertyEntry.Key, GetLocalizedText(propertyEntry.Value!.AsObject(), culture));
        }

        localization.Add(new XElement("Exceptions",
            new XElement("UserVisibleExceptions", validationNode)));
    }

    if (mergedEnums.Count > 0)
    {
        var enumsGroup = new XElement("LocalizationGroup", new XAttribute("Name", "Enums"));
        foreach (KeyValuePair<string, JsonNode?> enumEntry in mergedEnums.OrderBy(e => e.Key))
        {
            JsonObject enumNode = enumEntry.Value!.AsObject();
            var enumGroup = new XElement("LocalizationGroup", new XAttribute("Name", enumEntry.Key));
            foreach (KeyValuePair<string, JsonNode?> memberEntry in enumNode.OrderBy(m => m.Key))
            {
                if (memberEntry.Key == "caption")
                {
                    continue;
                }

                enumGroup.Add(new XElement("LocalizationItem",
                    new XAttribute("Name", memberEntry.Key),
                    new XAttribute("Value", GetLocalizedText(memberEntry.Value!.AsObject(), culture))));
            }

            enumsGroup.Add(enumGroup);
        }

        localization.Add(enumsGroup);
    }

    AppendFrameworkTextsLocalization(localization, validationTemplatesRoot, culture);

    if (localization.HasElements)
    {
        application.Add(localization);
    }
}

static void AppendFrameworkTextsLocalization(
    XElement localizationParent,
    JsonObject validationTemplatesRoot,
    string culture)
{
    if (validationTemplatesRoot["frameworkTexts"] is not JsonObject frameworkTexts)
    {
        return;
    }

    var textsGroup = new XElement("LocalizationGroup", new XAttribute("Name", "Texts"));
    foreach (KeyValuePair<string, JsonNode?> itemEntry in frameworkTexts.OrderBy(e => e.Key))
    {
        textsGroup.Add(new XElement("LocalizationItem",
            new XAttribute("Name", itemEntry.Key),
            new XAttribute("Value", GetLocalizedText(itemEntry.Value!.AsObject(), culture))));
    }

    localizationParent.Add(textsGroup);
}

static void UpdateBaseModelEnumLocalization(string moduleDir, JsonObject mergedEnums)
{
    string basePath = Path.Combine(moduleDir, "Model.DesignedDiffs.xafml");
    var document = XDocument.Load(basePath, LoadOptions.PreserveWhitespace);
    XElement application = document.Root ?? throw new InvalidOperationException("Missing Application root.");

    XElement localization = application.Element("Localization") ?? new XElement("Localization");
    if (application.Element("Localization") is null)
    {
        application.Add(localization);
    }

    localization.Elements("LocalizationGroup")
        .FirstOrDefault(g => g.Attribute("Name")?.Value == "Enums")
        ?.Remove();

    var enumsGroup = new XElement("LocalizationGroup", new XAttribute("Name", "Enums"));
    foreach (KeyValuePair<string, JsonNode?> enumEntry in mergedEnums.OrderBy(e => e.Key))
    {
        JsonObject enumNode = enumEntry.Value!.AsObject();
        var enumGroup = new XElement("LocalizationGroup", new XAttribute("Name", enumEntry.Key));
        foreach (KeyValuePair<string, JsonNode?> memberEntry in enumNode.OrderBy(m => m.Key))
        {
            if (memberEntry.Key == "caption")
            {
                continue;
            }

            enumGroup.Add(new XElement("LocalizationItem",
                new XAttribute("Name", memberEntry.Key),
                new XAttribute("Value", GetEnglishCaption(memberEntry.Value!.AsObject(), memberEntry.Key))));
        }

        enumsGroup.Add(enumGroup);
    }

    localization.Add(enumsGroup);
    WriteXafml(basePath, application);
    Console.WriteLine($"Updated enum localization in {basePath}");
}

static bool TryBuildViewLayout(JsonElement view, string culture, out XElement? layout)
{
    layout = null;
    bool hasNested = view.TryGetProperty("nestedLayoutGroups", out JsonElement nestedLayoutGroups)
        && nestedLayoutGroups.ValueKind == JsonValueKind.Array
        && nestedLayoutGroups.GetArrayLength() > 0;
    bool hasFlat = view.TryGetProperty("layoutGroups", out JsonElement layoutGroups)
        && layoutGroups.EnumerateObject().Any();

    if (!hasNested && !hasFlat)
    {
        return false;
    }

    layout = new XElement("Layout",
        new XElement("LayoutGroup",
            new XAttribute("Id", "Main"),
            new XAttribute("RelativeSize", "100")));
    XElement main = layout.Element("LayoutGroup")!;

    if (hasNested)
    {
        foreach (JsonElement nest in nestedLayoutGroups.EnumerateArray())
        {
            AppendNestedLayoutGroups(main, nest, culture);
        }
    }

    if (hasFlat)
    {
        foreach (JsonProperty group in layoutGroups.EnumerateObject().OrderBy(g => g.Name))
        {
            main.Add(new XElement("LayoutGroup",
                new XAttribute("Id", group.Name),
                new XAttribute("Caption", GetText(group.Value, culture))));
        }
    }

    return true;
}

static void AppendNestedLayoutGroups(XElement main, JsonElement nest, string culture)
{
    if (!nest.TryGetProperty("path", out JsonElement pathSegments)
        || !nest.TryGetProperty("groups", out JsonElement groups))
    {
        return;
    }

    XElement current = main;
    foreach (JsonElement segment in pathSegments.EnumerateArray())
    {
        string id = segment.GetString()
            ?? throw new InvalidOperationException("Layout path segment is missing.");
        XElement? child = current.Elements()
            .FirstOrDefault(e => e.Attribute("Id")?.Value == id);
        if (child is null)
        {
            bool isTabbedGroup = id is "Item1" or "Item2" or "Tabs";
            child = new XElement(isTabbedGroup ? "TabbedGroup" : "LayoutGroup", new XAttribute("Id", id));
            current.Add(child);
        }

        current = child;
    }

    foreach (JsonProperty group in groups.EnumerateObject().OrderBy(g => g.Name))
    {
        XElement? existing = current.Elements("LayoutGroup")
            .FirstOrDefault(g => g.Attribute("Id")?.Value == group.Name);
        string caption = GetText(group.Value, culture);
        if (existing is null)
        {
            current.Add(new XElement("LayoutGroup",
                new XAttribute("Id", group.Name),
                new XAttribute("Caption", caption)));
        }
        else
        {
            existing.SetAttributeValue("Caption", caption);
        }
    }
}

static void AppendBlazorHostViewLayouts(
    XElement application,
    JsonElement root,
    string culture,
    string[] blazorLayoutDetailViews)
{
    JsonElement views = root.GetProperty("views");
    var viewsNode = new XElement("Views");

    foreach (string viewId in blazorLayoutDetailViews)
    {
        if (!views.TryGetProperty(viewId, out JsonElement view)
            || !TryBuildViewLayout(view, culture, out XElement? layout)
            || layout is null)
        {
            continue;
        }

        var detailView = new XElement("DetailView", new XAttribute("Id", viewId));
        if (view.TryGetProperty("caption", out JsonElement caption))
        {
            detailView.SetAttributeValue("Caption", GetText(caption, culture));
        }

        detailView.Add(layout);
        viewsNode.Add(detailView);
    }

    if (viewsNode.HasElements)
    {
        application.Add(viewsNode);
    }
}

static void AppendBlazorShellLocalization(XElement application, JsonObject validationTemplatesRoot, string culture)
{
    var localization = new XElement("Localization");
    var visualComponents = new XElement("LocalizationGroup", new XAttribute("Name", "VisualComponents"));

    if (validationTemplatesRoot["blazorAlert"] is JsonObject alertItems)
    {
        var alertGroup = new XElement("LocalizationGroup", new XAttribute("Name", "Alert"));
        foreach (KeyValuePair<string, JsonNode?> itemEntry in alertItems.OrderBy(e => e.Key))
        {
            alertGroup.Add(new XElement("LocalizationItem",
                new XAttribute("Name", itemEntry.Key),
                new XAttribute("Value", GetLocalizedText(itemEntry.Value!.AsObject(), culture))));
        }

        visualComponents.Add(alertGroup);
    }

    if (visualComponents.HasElements)
    {
        localization.Add(visualComponents);
    }

    AppendFrameworkTextsLocalization(localization, validationTemplatesRoot, culture);

    if (localization.HasElements)
    {
        application.Add(localization);
    }
}

static void WriteMessageCatalog(string path, JsonObject messages)
{
    string[] cultures = ["en-US", "tr-TR", "tk-TM", "ru-RU"];
    var sb = new StringBuilder();
    sb.AppendLine("// <auto-generated />");
    sb.AppendLine("namespace Visa2026.Module.Localization.Generated;");
    sb.AppendLine();
    sb.AppendLine("public static partial class VisaUiMessageCatalog");
    sb.AppendLine("{");
    sb.AppendLine("    private static readonly Dictionary<string, Dictionary<string, string>> Messages = new(StringComparer.Ordinal)");
    sb.AppendLine("    {");

    foreach (KeyValuePair<string, JsonNode?> messageEntry in messages.OrderBy(m => m.Key))
    {
        JsonObject node = messageEntry.Value!.AsObject();
        sb.AppendLine($"        [\"{messageEntry.Key}\"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)");
        sb.AppendLine("        {");
        foreach (string culture in cultures)
        {
            string text = GetMessageText(node, culture);
            sb.AppendLine($"            [\"{culture}\"] = {ToLiteral(text)},");
        }

        sb.AppendLine("        },");
    }

    sb.AppendLine("    };");
    sb.AppendLine();
    sb.AppendLine("    public static bool TryGet(string culture, string key, out string? message)");
    sb.AppendLine("    {");
    sb.AppendLine("        message = null;");
    sb.AppendLine("        if (!Messages.TryGetValue(key, out Dictionary<string, string>? cultures))");
    sb.AppendLine("        {");
    sb.AppendLine("            return false;");
    sb.AppendLine("        }");
    sb.AppendLine();
    sb.AppendLine("        if (cultures.TryGetValue(culture, out string? exact))");
    sb.AppendLine("        {");
    sb.AppendLine("            message = exact;");
    sb.AppendLine("            return true;");
    sb.AppendLine("        }");
    sb.AppendLine();
    sb.AppendLine("        if (culture.Length >= 2 && cultures.TryGetValue(culture[..2], out string? twoLetter))");
    sb.AppendLine("        {");
    sb.AppendLine("            message = twoLetter;");
    sb.AppendLine("            return true;");
    sb.AppendLine("        }");
    sb.AppendLine();
    sb.AppendLine("        return cultures.TryGetValue(\"en-US\", out message);");
    sb.AppendLine("    }");
    sb.AppendLine("}");

    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
}

static string GetLocalizedText(JsonObject node, string culture) => GetMessageText(node, culture);

static string GetMessageText(JsonObject node, string culture)
{
    if (TryGetEnglishNode(node, out JsonNode? enNode))
    {
        return culture switch
        {
            "en-US" => enNode!.GetValue<string>() ?? throw new InvalidOperationException("Missing en-US text."),
            "tr-TR" => node["tr-TR"]?.GetValue<string>() ?? enNode!.GetValue<string>()!,
            "tk-TM" => node["tk-TM"]?.GetValue<string>() ?? enNode!.GetValue<string>()!,
            "ru-RU" => node["ru-RU"]?.GetValue<string>() ?? enNode!.GetValue<string>()!,
            _ => enNode!.GetValue<string>()!,
        };
    }

    return node[culture]?.GetValue<string>()
        ?? throw new InvalidOperationException($"Missing {culture} for translation.");
}

static string GetEnglishCaption(JsonObject node, string fallbackMemberName)
{
    if (TryGetEnglishNode(node, out JsonNode? enNode))
    {
        return enNode!.GetValue<string>() ?? fallbackMemberName;
    }

    return fallbackMemberName;
}

static bool TryGetEnglishNode(JsonObject node, out JsonNode? enNode)
{
    if (node.TryGetPropertyValue("en-US", out enNode))
    {
        return true;
    }

    if (node.TryGetPropertyValue("en", out enNode))
    {
        return true;
    }

    enNode = null;
    return false;
}

static void UpdateBaseModelEnumCaptions(string moduleDir, JsonObject mergedEnums)
{
    string basePath = Path.Combine(moduleDir, "Model.DesignedDiffs.xafml");
    var document = XDocument.Load(basePath, LoadOptions.PreserveWhitespace);
    XElement application = document.Root ?? throw new InvalidOperationException("Missing Application root.");
    XElement boModel = application.Element("BOModel") ?? new XElement("BOModel");
    if (application.Element("BOModel") is null)
    {
        application.Add(boModel);
    }

    foreach (KeyValuePair<string, JsonNode?> enumEntry in mergedEnums.OrderBy(e => e.Key))
    {
        JsonObject enumNode = enumEntry.Value!.AsObject();
        XElement? existing = boModel.Elements("Class")
            .FirstOrDefault(c => c.Attribute("Name")?.Value == enumEntry.Key);
        existing?.Remove();

        var classElement = new XElement("Class", new XAttribute("Name", enumEntry.Key));
        if (enumNode.TryGetPropertyValue("caption", out JsonNode? captionNode) && captionNode is JsonObject captionObj)
        {
            classElement.SetAttributeValue("Caption", GetEnglishCaption(captionObj, enumEntry.Key));
        }

        var ownMembers = new XElement("OwnMembers");
        foreach (KeyValuePair<string, JsonNode?> memberEntry in enumNode.OrderBy(m => m.Key))
        {
            if (memberEntry.Key == "caption")
            {
                continue;
            }

            ownMembers.Add(new XElement("Member",
                new XAttribute("Name", memberEntry.Key),
                new XAttribute("Caption", GetEnglishCaption(memberEntry.Value!.AsObject(), memberEntry.Key))));
        }

        classElement.Add(ownMembers);
        boModel.Add(classElement);
    }

    WriteXafml(basePath, application);
    Console.WriteLine($"Updated enum captions in {basePath}");
}

static string ToLiteral(string value) =>
    "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\"";

static void MergeNavigationPaths(XElement navItemsRoot, JsonObject paths, string culture)
{
    XElement? lookup = navItemsRoot.Elements("Item").FirstOrDefault(i => i.Attribute("Id")?.Value == "Lookup");
    if (lookup is null)
    {
        lookup = new XElement("Item", new XAttribute("Id", "Lookup"), new XElement("Items"));
        navItemsRoot.Add(lookup);
    }

    XElement lookupItems = lookup.Element("Items") ?? new XElement("Items");
    if (lookup.Element("Items") is null)
    {
        lookup.Add(lookupItems);
    }

    foreach (KeyValuePair<string, JsonNode?> pathEntry in paths.OrderBy(p => p.Key.Count(c => c == '/')))
    {
        string path = pathEntry.Key;
        if (!path.StartsWith("Lookup/", StringComparison.Ordinal))
        {
            continue;
        }

        string[] segments = path["Lookup/".Length..].Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            continue;
        }

        XElement currentContainer = lookupItems;
        for (int i = 0; i < segments.Length; i++)
        {
            string segment = segments[i];
            XElement? child = currentContainer.Elements("Item").FirstOrDefault(x => x.Attribute("Id")?.Value == segment);
            if (child is null)
            {
                child = new XElement("Item", new XAttribute("Id", segment), new XElement("Items"));
                currentContainer.Add(child);
            }

            if (i == segments.Length - 1)
            {
                child.SetAttributeValue("Caption", GetLocalizedText(pathEntry.Value!.AsObject(), culture));
            }

            currentContainer = child.Element("Items") ?? new XElement("Items");
            if (child.Element("Items") is null)
            {
                child.Add(currentContainer);
            }
        }
    }
}

static XElement BuildNavigationItem(string id, JsonElement node, string culture)
{
    if (node.TryGetProperty("children", out JsonElement children))
    {
        if (!node.TryGetProperty("caption", out JsonElement captionElement))
        {
            throw new InvalidOperationException($"Navigation item '{id}' has children but no caption.");
        }

        var group = new XElement("Item",
            new XAttribute("Id", id),
            new XAttribute("Caption", GetText(captionElement, culture)),
            new XElement("Items"));
        foreach (JsonProperty child in children.EnumerateObject().OrderBy(c => c.Name))
        {
            group.Element("Items")!.Add(BuildNavigationItem(child.Name, child.Value, culture));
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
