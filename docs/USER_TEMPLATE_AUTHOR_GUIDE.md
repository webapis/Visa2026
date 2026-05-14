# User Template Author Guide

How to create custom Word report templates for the Visa 2026 application.

---

## Quick Start (5 Minutes)

1. **Create a .docx file** in Microsoft Word
2. **Add placeholders** using the **`ds`** model prefix (DocxTemplater), e.g. `{{ds.FullApplicationNumber}}` or `{{ds.CompanyHead_FullName}}` — see **Model prefix `ds`** below
3. **Save and upload** via **User Report Templates** in the application
4. **Click "Extract Placeholders"** — the system scans the `.docx` for `{{…}}` tokens
5. **Click "Validate Placeholders"** — valid keys resolve to BO properties; fix any red rows, then re-upload if needed
6. **Set visibility** (application types and/or **Visibility Criteria** popup — same style as **Report Visibility** in System)
7. **Mark Active** and test from an **Application** with **Resminamalar**

---

## Model prefix `ds` (required)

Built-in ministry Word reports and **user-uploaded** templates both use DocxTemplater with **`BindModel("ds", …)`**. In the `.docx` you must address fields under **`ds`**:

- **Scalars:** `{{ds.PropertyName}}` — use the same names **validation** shows after extract (often aligned with `[NotMapped]` flat names on `Application` / `ApplicationItem`, e.g. `CompanyHead_FullName` with underscores).
- **Loops:** start `{{#ds.CollectionName}}` … end `{{/ds.CollectionName}}` (e.g. collection `ApplicationItems`). **Inside the loop row**, row fields use a **leading dot** on the token in the template, e.g. `{{.Person_FullName}}` — follow **Validate Placeholders** output for exact keys.

If placeholders omit `ds.`, merge will not fill them.

---

## Placeholder Syntax

Placeholders are text wrapped in double curly braces. **Always include the `ds.` prefix** for top-level fields and `#ds.` for loops.

### Simple Fields

| What you type | What gets filled |
|--------------|------------------|
| `{{ds.FullApplicationNumber}}` | e.g. CEC-0042/2026 |
| `{{ds.ApplicationDate}}` | date from the application |
| `{{ds.CompanyHead_FullName}}` | company head display name |
| `{{ds.CompanyHead_PositionTm}}` | position (Türkmen) |

**Tip:** Property paths are validated against the **Root Business Object** you select on the template (`Application`, `ApplicationItem`, …). Use **Extract** + **Validate** to see the resolved paths; prefer those exact strings in Word.

### List/Table Loops

For repeating sections (e.g. table of people on an **Application**-root template):

```
{{#ds.ApplicationItems}}
  Name: {{.Person_FullName}}
  Passport: {{.Passport_Number}}
  Visa: {{.Visa_Number}}
{{/ds.ApplicationItems}}
```

| Syntax | Meaning |
|--------|---------|
| `{{#ds.ApplicationItems}}` | Start loop over the `ApplicationItems` collection |
| `{{.Person_FullName}}` | Inside loop: field on the current row (leading `.` = row scope) |
| `{{/ds.ApplicationItems}}` | End loop |

**Example: Simple Table**

Create a Word table. In the first data row, put:

| # | Full Name | Passport | Visa Number |
|---|-----------|----------|-------------|
| 1 | `{{.Person_FullName}}` | `{{.Passport_Number}}` | `{{.Visa_Number}}` |

Then wrap the data row with loop tags (each marker in its **own paragraph** / cell pattern per DocxTemplater rules — see `docs/WORD_REPORT_GENERATION_IDEA.md`):

- Before the row: `{{#ds.ApplicationItems}}`
- After the row: `{{/ds.ApplicationItems}}`

Word will duplicate the row for each matching collection item.

---

## Common Placeholders Reference

### Application Fields (examples — confirm with Validate)

| Placeholder | Example Output |
|-------------|----------------|
| `{{ds.FullApplicationNumber}}` | e.g. CEC-0042/2026 |
| `{{ds.ApplicationDate}}` | application date |
| `{{ds.ApplicationDateText}}` | formatted text |
| `{{ds.VisaPeriod_NameTm}}` | period (locale) |
| `{{ds.VisaCategory_NameTm}}` | category (locale) |
| `{{ds.Urgency_NameTm}}` | urgency (locale) |
| `{{ds.ProjectContract_Description}}` | contract line |

### Company Fields (examples)

| Placeholder | Example Output |
|-------------|----------------|
| `{{ds.Company_Code}}` | company code |
| `{{ds.CompanyHead_FullName}}` | signatory name |
| `{{ds.CompanyHead_PositionTm}}` | signatory position |
| `{{ds.CompanyHead_PassportNumber}}` | passport number |

### Row Loops (inside `{{#ds.ApplicationItems}}` … `{{/ds.ApplicationItems}}`)

| Placeholder | Example Output |
|-------------|----------------|
| `{{.Person_FullName}}` | person name |
| `{{.Person_DateOfBirthText}}` | DOB text |
| `{{.Passport_Number}}` | passport number |
| `{{.Visa_NumberAndType}}` | visa summary |
| `{{.Address_FullAddress}}` | address |
| `{{.WorkPermit_Number}}` | work permit no. |
| `{{.Invitation_Number}}` | invitation no. |

---

## Step-by-Step: Creating Your First Template

### Step 1: Create the Document

Open Microsoft Word and create a new document.

### Step 2: Design Your Layout

Create your letter or form layout. Use:
- **Tables** for alignment (easier than tabs)
- **Styles** for consistent formatting
- **Headers/Footers** for letterheads

### Step 3: Insert Placeholders

Type placeholders directly where data should appear:

```
To: Ministry of Foreign Affairs of Turkmenistan

Date: {{ds.ApplicationDateText}}

Application Number: {{ds.FullApplicationNumber}}

Dear Sir/Madam,

Please find enclosed visa application for {{ds.CompanyHead_FullName}},
{{ds.CompanyHead_PositionTm}} of {{ds.Company_Code}}.

Sincerely,
{{ds.CompanyHead_FullName}}
{{ds.CompanyHead_PositionTm}}
```

### Step 4: Save as .docx

File → Save As → Word Document (*.docx)

**Important:** Do not use .doc or .rtf. Only .docx works.

### Step 5: Upload to Application

1. In the application, go to **User Report Templates**
2. Click **New**
3. Fill in:
   - **Template Name**: "My Cover Letter" (display name in Resminamalar)
   - **Description**: "Standard cover letter for visa applications"
   - **Root Business Object**: e.g. **Application** (criteria editor + merge root must match)
   - **Template File**: Upload your .docx
4. Click **Save**

### Step 6: Extract Placeholders

After saving:
1. Click **Extract Placeholders** (toolbar button)
2. The system scans your .docx and lists all `{{placeholders}}`
3. Check the **Placeholders** tab to see what was found

### Step 7: Validate Placeholders

1. Click **Validate Placeholders**
2. Green checkmark ✅ = valid field that exists
3. Red X ❌ = typo or non-existent field
4. Fix any errors in your .docx and re-upload

### Step 8: Set Visibility

Choose when this template should appear:

| Option | When to Use |
|--------|-------------|
| **All types** | Show for every application |
| **Specific types** | Prefer **Applicable Application Types** (link rows). If **Visibility Criteria** is also set, it is evaluated as well. |
| **Data driven** | **Visibility Criteria** is required — use the **popup criteria editor** (pencil icon), same idea as **System → Report Visibility**. |

**Root Business Object** controls which type the criteria editor lists (Application, Application Item, …). For **Application**, the expression is evaluated on the current application. For other roots, the template is shown when **any non-deleted** row in that collection matches the criteria (still from the open **Application**).

For raw-syntax examples (when not using the popup), criteria still use XAF field names, e.g.:
```
[ApplicationType.Name] = 'App_Change_Passport'
```
Adjust property names to match the selected root type.

### Step 9: Activate and Test

1. Check **Is Active**
2. Click **Save**
3. Go to an Application that matches your visibility
4. Click **Resminamalar** — your template should appear
5. Your user template is included in the **Resminamalar** bundle when visibility rules pass (same download as built-in Word reports — `.zip` if multiple files)

---

## Tips and Best Practices

### Formatting

- **Don't format the placeholder text differently** — it gets replaced entirely
- **Format the surrounding text** — the replacement inherits that formatting
- **Use Word styles** — easier to maintain than direct formatting

### Tables

For best results with row loops:
1. Create a table with headers in the first row
2. Put your data row (with placeholders) in the second row
3. Put `{{#ds.ApplicationItems}}` (or your validated collection name) before the second row
4. Put `{{/ds.ApplicationItems}}` after the second row

### Placeholder Names

- **Include `ds.`** for document-level fields: `{{ds.FullApplicationNumber}}` not `{{FullApplicationNumber}}`
- **Case-sensitive** on property segments
- **Underscores vs dots:** validated paths often use **`_`** for flattened `[NotMapped]` names (e.g. `CompanyHead_FullName`); follow **Validate Placeholders**, not guesswork
- **Use existing fields**: run **Validate Placeholders** after every `.docx` change

### Testing

1. **Upload a test template first** — use a name like "TEST - My Template"
2. **Verify placeholders extract correctly**
3. **Generate on a real application** and check output
4. **Fix any issues** and re-upload
5. **Rename to remove "TEST"** when ready

---

## Troubleshooting

### "Placeholder not found" (Red X)

- Check spelling against **Validate Placeholders** / resolved property path
- Check capitalization
- For nested values, use the **full validated path** (often with `_`), e.g. `CompanyHead_FullName` under `ds.`

### Loop not duplicating rows

- Verify `{{#ds.Collection}}` and `{{/ds.Collection}}` match exactly (including `ds.`)
- DocxTemplater often requires loop markers in **their own paragraph** — see `docs/WORD_REPORT_GENERATION_IDEA.md` (sanawy / critical template rules)
- Check that the collection name matches the validated placeholder (e.g. `ApplicationItems`)

### Data not appearing

- Verify **Root BO Type** matches the data source
- Check **Visibility Criteria** — template might be filtered out
- Ensure template is **Is Active = true**

### File won't upload

- Must be .docx format (Word 2007+)
- File size should be under 10MB
- Try re-saving in Word if corrupted

---

## Full Field Reference

See **`docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md`** and **`docs/WORD_REPORT_GENERATION_IDEA.md`** (binding tables for built-in reports). For user templates, **Extract Placeholders** + **Validate Placeholders** is the source of truth for which names exist on your selected **Root Business Object** — use those resolved keys in Word with the **`ds.`** / loop rules above.

---

## Getting Help

If a placeholder you need doesn't exist:
1. Check if it's a `[NotMapped]` property on the business object
2. Ask a developer to add it to the business object
3. Most common fields are already available

For template issues:
- Extract and validate placeholders first
- Check the Validation Status column
- Look for red X marks indicating invalid placeholders
