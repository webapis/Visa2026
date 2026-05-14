# User Template Author Guide

How to create custom Word report templates for the Visa 2026 application.

---

## Quick Start (5 Minutes)

1. **Create a .docx file** in Microsoft Word
2. **Add placeholders** like `{{ApplicationNumber}}` or `{{CompanyHead.FullName}}`
3. **Save and upload** via User Report Templates in the application
4. **Click "Extract Placeholders"** — the system finds all your placeholders
5. **Click "Validate"** — green checkmarks = valid, red = fix the name
6. **Set visibility** (when should this template appear?)
7. **Mark Active** and test in Resminamalar

---

## Placeholder Syntax

Placeholders are text wrapped in double curly braces: `{{FieldName}}`

### Simple Fields

| What you type | What gets filled |
|--------------|------------------|
| `{{ApplicationNumber}}` | 2026-001234 |
| `{{ApplicationDate}}` | 14.05.2026 |
| `{{CompanyHead.FullName}}` | Gurbanguly Berdimuhamedow |
| `{{CompanyHead.PositionTm}}` | Direktor |

**Tip:** Use the dot (`.`) to access related objects. For example, `CompanyHead.FullName` gets the full name of the company head linked to this application.

### List/Table Loops

For repeating sections (like a table of people):

```
{{#ApplicationItems}}
  Name: {{.Person.FullName}}
  Passport: {{.Passport.Number}}
  Visa: {{.Visa.Number}}
{{/ApplicationItems}}
```

| Syntax | Meaning |
|--------|---------|
| `{{#ApplicationItems}}` | Start loop over application items |
| `{{.Person.FullName}}` | Inside loop: person's name (dot means "current row") |
| `{{/ApplicationItems}}` | End loop |

**Example: Simple Table**

Create a Word table. In the first data row, put:

| # | Full Name | Passport | Visa Number |
|---|-----------|----------|-------------|
| 1 | `{{.Person.FullName}}` | `{{.Passport.Number}}` | `{{.Visa.Number}}` |

Then select that row and wrap with loop tags:
- Before the row: `{{#ApplicationItems}}`
- After the row: `{{/ApplicationItems}}`

Word will duplicate the row for each person in the application.

---

## Common Placeholders Reference

### Application Fields

| Placeholder | Example Output |
|-------------|----------------|
| `{{ApplicationNumber}}` | 2026-V-001234 |
| `{{ApplicationDate}}` | 14.05.2026 |
| `{{ApplicationDateText}}` | 14-nji maý, 2026 ý. |
| `{{VisaPeriod.Name}}` | 1 (ýyl) |
| `{{VisaCategory.Name}}` | A (Işgär) |
| `{{Urgency.Name}}` | Adatça (30 gün) |
| `{{ProjectContract.ContractNumber}}` | MT-X-2024-001 |

### Company Fields

| Placeholder | Example Output |
|-------------|----------------|
| `{{Company.Name}}` | ABC ÝK |
| `{{Company.Code}}` | 123456789 |
| `{{CompanyHead.FullName}}` | Myrat Myradow Myradowiç |
| `{{CompanyHead.PositionTm}}` | Ýolbaşçy |
| `{{CompanyHead.PassportNumber}}` | A-1234567 |

### Row Loops (Inside {{#ApplicationItems}})

| Placeholder | Example Output |
|-------------|----------------|
| `{{.Person.FullName}}` | Anna Wekilowa |
| `{{.Person.DateOfBirthText}}` | 15.03.1990 |
| `{{.Passport.Number}}` | B-7654321 |
| `{{.Visa.NumberAndType}}` | 12345 / A |
| `{{.Address.FullAddress}}` | Aşgabat şäheri, Köşi 123 |
| `{{.WorkPermit.Number}}` | WP-2024-001 |
| `{{.Invitation.Number}}` | INV-12345 |

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

Date: {{ApplicationDate}}

Application Number: {{ApplicationNumber}}

Dear Sir/Madam,

Please find enclosed visa application for {{CompanyHead.FullName}},
{{CompanyHead.PositionTm}} of {{Company.Name}}.

Sincerely,
{{CompanyHead.FullName}}
{{CompanyHead.PositionTm}}
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
   - **Root BO Type**: Application (what data source?)
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
| **AllTypes** | Show for every application |
| **SpecificTypes** | Show only for certain application types |
| **DataDriven** | Show based on custom criteria |

For **DataDriven**, use XAF criteria syntax:
```
[ApplicationType.Name] = 'Visa' AND [Urgency.Name] = 'Çalt'
```

### Step 9: Activate and Test

1. Check **Is Active**
2. Click **Save**
3. Go to an Application that matches your visibility
4. Click **Resminamalar** — your template should appear
5. Click your template name → Word document downloads with data filled

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
3. Put `{{#LoopName}}` before the second row
4. Put `{{/LoopName}}` after the second row

### Placeholder Names

- **Case-sensitive**: `ApplicationNumber` ≠ `applicationnumber`
- **No spaces**: Use `CompanyHead.FullName` not `Company Head.Full Name`
- **Use existing fields**: Check the reference below or ask for a field list

### Testing

1. **Upload a test template first** — use a name like "TEST - My Template"
2. **Verify placeholders extract correctly**
3. **Generate on a real application** and check output
4. **Fix any issues** and re-upload
5. **Rename to remove "TEST"** when ready

---

## Troubleshooting

### "Placeholder not found" (Red X)

- Check spelling against the field reference
- Check capitalization
- For related objects, verify the path: `CompanyHead.FullName` not just `FullName`

### Loop not duplicating rows

- Verify `{{#Name}}` and `{{/Name}}` match exactly
- Make sure they are in the same table cell/paragraph
- Check that the collection name is correct (e.g., `ApplicationItems` not `Items`)

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

See `docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md` for the complete list of available fields on each business object.

Or: Open any existing application, click **Resminamalar** → **View Placeholders** to see available fields for that specific record type.

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
