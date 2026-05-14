# User-Defined Word Templates — Design Idea

> **Vision:** Enable application users to create, upload, and manage Word report templates without developer involvement. Templates use placeholders that map to Business Object properties.

---

## Problem Statement

**Current State:**
- Word templates require developer involvement (C# `*ReportDef`, embedded resources, rebuild, redeploy)
- Ministry form changes or new report types need a full development cycle
- Users cannot create ad-hoc reports or customize layouts

**Desired State:**
- Users design templates in Microsoft Word with `{{placeholder}}` syntax
- Upload templates via XAF UI
- Configure which BO properties each placeholder maps to
- Associate templates with Application Types
- Generate reports on-demand without code changes

---

## User Workflow (Desktop → Upload → Use)

### Step 1: Create in Microsoft Word (User's Desktop)

1. **Open Word** on local PC
2. **Design the document**:
   - Set page orientation (portrait/landscape)
   - Set margins (follow ministry standards)
   - Choose font family (Times New Roman for official letters)
   - Set font sizes (15pt headers, 14pt body, etc.)
   - Add static content (letter body, legal clauses, ministry addresses)
   - Create tables for person lists if needed
3. **Insert placeholders** where dynamic data should appear:
   - Type `{{ApplicationNumber}}` where the app number goes
   - Type `{{ApplicationDate}}` for the date
   - Type `{{CompanyHead.FullName}}` for signatory name
   - Use `{{#Items}}` ... `{{/Items}}` for table rows
4. **Save as .docx** (normal Word format)

### Step 2: Upload to Visa2026 Application

1. **Navigate** to Report Templates section in XAF
2. **Click "New Template"**
3. **Upload** the `.docx` file
4. **System extracts** all `{{placeholder}}` tags automatically
5. **Configure**:
   - Template name and description
   - **Select Business Object** — the BO this template draws data from (Application, ApplicationItem, Registration, or BusinessTrip)
   - Which Application Types this template applies to
6. **Validate** — system verifies all placeholders exist on selected BO
   - Shows green check for valid placeholders
   - Shows red warning for unknown placeholders (typos or missing properties)
7. **Preview** with sample data (yellow highlights show dynamic parts)
8. **Activate** — template now appears in Resminamalar

> **No manual mapping needed.** Placeholder names directly match BO property names. `{{ApplicationNumber}}` automatically pulls from `Application.ApplicationNumber`.

### Step 3: Generate Reports

1. **Open** an Application in XAF
2. **Click Resminamalar**
3. **See** the user-created template in the list (alongside system templates)
4. **Click to generate** — filled Word document downloads immediately

---

## Core Concepts

### 1. Template = Word Document + Metadata

| Component | Source | Purpose |
|-----------|--------|---------|
| **`.docx` file** | User-uploaded | Layout, static text, styling, placeholders |
| **BO Selection** | User-configured in UI | Which BO (Application, ApplicationItem, etc.) provides the data |
| **Placeholder resolution** | **Automatic** | `{{PropertyName}}` → matches BO property by name |
| **Applicability rules** | User-configured | Which Application Types, conditions |

### 2. Placeholder Syntax (Convention-Based)

**Rule: Placeholder name = BO Property Path**

Users type placeholders that exactly match the Business Object property names:

```
{{ApplicationNumber}}           → maps to Application.ApplicationNumber
{{ApplicationDate}}             → maps to Application.ApplicationDate  
{{FullApplicationNumber}}         → maps to Application.FullApplicationNumber
{{CompanyHead.FullName}}          → maps to Application.CompanyHead.FullName
{{CompanyHead.Position.NameTm}}   → maps to Application.CompanyHead.Position.NameTm
{{TotalPersonCount}}              → maps to Application.TotalPersonCount
```

**For row/collection properties:**
```
{{#ApplicationItems}}           → loops through Application.ApplicationItems
{{.RowNumber}}                    → each row gets RowNumber property
{{.Person_FullName}}              → row's Person_FullName property
{{/ApplicationItems}}
```

**Multi-row table example:**
```
| # | Full Name | Passport |
|---|---|---|
| {{#ApplicationItems}} | {{.Person_FullName}} | {{.Passport_Number}} |
| {{/ApplicationItems}} |  |  |
```

> **Reference:** See `docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md` for complete list of available properties per BO.

### 3. BO Property Path Resolution

Placeholders map to Business Object properties using dot-notation:

| Placeholder | Resolves To | Example Value |
|-------------|-------------|---------------|
| `{{ApplicationNumber}}` | `Application.ApplicationNumber` | "1234" |
| `{{ApplicationDate}}` | `Application.ApplicationDate` | "14.05.2026" |
| `{{CompanyHead.FullName}}` | `Application.CompanyHead.FullName` | "Gurbanguly Berdimuhamedow" |
| `{{CompanyHead.Position.NameTm}}` | `Application.CompanyHead.Position.NameTm` | "Direktor" |
| `{{Items.Count}}` | `Application.ApplicationItems.Count` | "3" |

---

## Data Model

### `UserReportTemplate` (New Business Object)

```csharp
public class UserReportTemplate : BaseObject
{
    // Identity
    public virtual string TemplateName { get; set; }           // "Invitation Letter 2026"
    public virtual string Description { get; set; }              // User notes
    
    // Storage
    public virtual FileData TemplateFile { get; set; }         // The .docx file
    
    // Target Business Object (root data source)
    public virtual UserReportBoType RootBoType { get; set; }     // Enum: Application, ApplicationItem, Registration, BusinessTrip, Person
    
    // Applicability
    public virtual IList<ApplicationType> ApplicableTypes { get; set; }  // Which ApplicationTypes
    public virtual string ApplicabilityCondition { get; set; }   // Optional: "BusinessTrips.Any()"
    
    // Layout Category (for UI grouping)
    public virtual ReportLayoutFamily LayoutFamily { get; set; } // Letter, Sanawy, Form, Table
    
    // Status
    public virtual bool IsActive { get; set; }                 // Enable/disable
    public virtual int SortOrder { get; set; }                 // Display order in Resminamalar
    
    // Audit
    public virtual DateTime CreatedDate { get; set; }
    public virtual ApplicationUser CreatedBy { get; set; }
    public virtual DateTime? ModifiedDate { get; set; }
    public virtual ApplicationUser ModifiedBy { get; set; }
}
```

### `UserReportPlaceholder` (Auto-Extracted for Validation)

```csharp
public class UserReportPlaceholder : BaseObject
{
    public virtual UserReportTemplate Template { get; set; }
    
    // Placeholder as found in the .docx
    public virtual string PlaceholderKey { get; set; }           // "ApplicationNumber", "CompanyHead.FullName"
    
    // Validation status
    public virtual bool IsValid { get; set; }                   // Exists on selected BO?
    public virtual string ResolvedPropertyPath { get; set; }      // "ApplicationNumber" or "CompanyHead.FullName"
    
    // For UI help - shows what this maps to
    public virtual string ExampleValue { get; set; }            // "1234", "Gurbanguly Berdimuhamedow"
}
```

> **No manual configuration.** Placeholders are auto-extracted and validated against the selected BO. The system resolves `{{PropertyName}}` → `BO.PropertyName` automatically.

### `UserReportBoType` (Enum)

```csharp
public enum UserReportBoType
{
    Application,        // Root = Application, header-style placeholders
    ApplicationItem,    // Per-person forms (single item context)
    Registration,       // Check-in/check-out context
    BusinessTrip,       // Trip-related reports
    Person              // Person-centric reports
}
```

---

## UI Workflow

### Phase 1: Template Upload & Configuration

**Step 1 — Upload:**
- User navigates to "Report Templates" (new navigation item)
- Clicks "New", uploads `.docx` file
- System extracts all `{{placeholder}}` tags from document

**Step 2 — Configure Metadata:**
- Set Template Name, Description
- Select **Root BO Type** (Application, ApplicationItem, etc.)
- Select **Applicable Application Types** (multi-select lookup)
- Set Layout Family (for UI grouping)
- Set Sort Order

**Step 3 — Map Placeholders:**
- System shows extracted placeholders in grid
- User maps each to BO property path
- Set format strings, transforms, default values
- For row placeholders: specify collection path (`ApplicationItems`, `Registrations`, etc.)

**Step 4 — Validate & Test:**
- "Preview with Sample Data" button
- System generates preview with yellow highlights
- User downloads, verifies layout

**Step 5 — Activate:**
- Mark as Active
- Template appears in Resminamalar for matching applications

### Phase 2: Report Generation

**Runtime Flow:**

```
User opens Application detail view
    ↓
Clicks "Resminamalar"
    ↓
System queries: UserReportTemplate.Where(t => 
    t.IsActive && 
    t.ApplicableTypes.Contains(application.ApplicationType))
    ↓
For each matching template:
    1. Load .docx from FileData into MemoryStream
    2. Build data dictionary from Application using placeholder maps
    3. Call DocxTemplater to fill
    4. Add to zip (or single download)
    ↓
Return generated document(s)
```

---

## Placeholder Resolution Engine

### Path Resolution

Given `{{CompanyHead.Position.NameTm}}` on `Application`:

```csharp
// Split path
var segments = "CompanyHead.Position.NameTm".Split('.');
// ["CompanyHead", "Position", "NameTm"]

// Navigate
object current = application;
foreach (var segment in segments)
{
    current = GetPropertyValue(current, segment);
    if (current == null) break;
}
return current?.ToString();
```

### Built-in Transforms

| Transform | Input | Output |
|-----------|-------|--------|
| `TurkmenWords` | `3` | `"üç"` |
| `GenitiveCase` | `"Mary welaýaty"` | `"Mary welaýatynyň"` |
| `AblativeCase` | `"Aşgabat şäheri"` | `"Aşgabat şäherinden"` |
| `DativeCase` | `"Akbugdaý etraby"` | `"Akbugdaý etrabyna"` |
| `ToUpper` | `"test"` | `"TEST"` |
| `DateOnly` | `DateTime` | `"14.05.2026"` |

### Collection Handling

For `{{#Items}}` ... `{{/Items}}` loops:

```csharp
// Resolve collection path
var collection = GetPropertyValue(application, "ApplicationItems") as IEnumerable;

// Build row dictionaries
var rows = new List<Dictionary<string, object>>();
int rowNum = 1;
foreach (var item in collection)
{
    var rowDict = new Dictionary<string, object>();
    rowDict["RowNumber"] = rowNum++;
    
    // Map each inner placeholder
    foreach (var ph in template.Placeholders.Where(p => p.Type == Row))
    {
        rowDict[ph.PlaceholderKey] = ResolvePath(item, ph.BoPropertyPath);
    }
    rows.Add(rowDict);
}

headerDict["rows"] = rows;
```

---

## Architecture Components

### New Services

| Service | Responsibility |
|---------|----------------|
| `IUserReportTemplateService` | CRUD for templates, placeholder extraction |
| `IUserReportPlaceholderResolver` | Path resolution, transform application |
| `IUserReportGenerator` | Fill templates, coordinate DocxTemplater |
| `IUserReportPreviewService` | Generate previews with sample/highlight data |

### Modified Components

| Component | Change |
|-----------|--------|
| `WordReportsController` | Query `UserReportTemplate` alongside `IWordReportDefinition`; unify generation |
| `Resminamalar` UI | Group by source: "System Reports" vs "Custom Reports" |

### New Controllers

| Controller | Purpose |
|------------|---------|
| `UserReportTemplateController` | ListView/DetailView for template management |
| `UserReportPlaceholderController` | Nested collection editor for placeholder mapping |
| `UserReportValidatorController` | Validate .docx, extract placeholders, preview |

---

## Security & Governance

| Concern | Mitigation |
|---------|------------|
| **Malicious uploads** | Validate .docx is valid OpenXML, scan for macros (reject if present) |
| **Data exposure** | Only expose BO properties marked `[VisibleInDetailView]` or explicit allowlist |
| **Performance** | Cache resolved templates; timeout on complex loops |
| **Audit** | Log template usage, who generated what when |
| **Versioning** | Keep history of template changes, allow rollback |

---

## Migration Path

### Phase 1 (MVP)
- Create `UserReportTemplate` BO and basic upload
- Support `Application` root type only
- Support header placeholders (`{{Field}}`)
- Simple preview with sample data

### Phase 2
- Add row/collection support (`{{#Items}}` loops)
- Support `ApplicationItem`, `Registration`, `BusinessTrip` root types
- Add transform functions

### Phase 3
- Visual placeholder builder (pick properties from tree)
- Conditional logic (`{?{Condition}}`)
- Template versioning and approval workflow

### Phase 4 (Optional)
- Web-based template designer (drag-and-drop)
- AI-assisted placeholder suggestions from document text

---

## Open Questions

1. **Placeholder syntax:** Stick to `{{Name}}` or allow user-friendly labels mapped to paths?
2. **BO property discovery:** Auto-scan and suggest based on `[NotMapped]` report properties?
3. **Complex conditions:** Allow users to write simple expressions ("Items.Count > 1")?
4. **Multi-file output:** Support generating one document per item (zip download)?
5. **Cross-template reuse:** Allow "include" or shared header/footer templates?
6. **Localization:** Support templates per language, or inline translations?

---

## Related Files

- `docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md` — Available BO properties
- `docs/WORD_REPORT_GENERATION_IDEA.md` — Existing Word pipeline architecture
- `.cursor/skills/visa2026-word-reports/SKILL.md` — Developer-focused report skill

---

## Next Steps (If Proceeding)

1. **Design review** — Discuss open questions, confirm scope
2. **Database migration** — Create `UserReportTemplate` and `UserReportPlaceholder` tables
3. **UI mockups** — Placeholder mapping interface
4. **MVP implementation** — Single BO type (Application), header placeholders only
5. **Test with power users** — Ministry forms team validates workflow
