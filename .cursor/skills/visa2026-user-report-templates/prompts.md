# Prompt Keywords — visa2026-user-report-templates

Copy-paste any of these into chat to activate the skill for the relevant task.

---

## Create a new seed template

- `Embed this Word template as a user report seed in Resources/Templates and register it.`
- `Add a new EnsureTemplateExists entry for MyTemplate.docx under Resources/Templates.`
- `Ship Resources/Templates/Foo.docx as a seeded UserReportTemplate visible for App_Xxx.`
- `Wire Resources/Templates/Foo.docx into UserReportTemplateUpdater and csproj as a user report template.`

---

## Update an existing seed (e.g. Sazakow)

- `Update the Sazakow (seed) EnsureTemplateExists block with new applicability.`
- `Change the boType / root BO for the Sazakow user report template seed.`
- `Add App_WP_Ext to the applicable application types for the Sazakow seed.`
- `Rename the seeded user template file and keep UserReportTemplateUpdater and csproj in sync.`
- `Adjust visibility criteria for the Sazakow user report template seed.`

---

## Placeholder lookup

- `Which placeholder should I use for [field] in a user report template?`
- `What is the ds.* token for [business data] in an Application-root user template?`
- `Show me the placeholder for company head name / urgency / visa period in a Resources/Templates docx.`

---

## Implement a missing placeholder

- `Add a missing placeholder for [business data] to the user report template system.`
- `The placeholder {{ds.X}} is not resolving — help me implement it on the Application BO.`
- `Update Application.cs and WORD_REPORT_PLACEHOLDER_REFERENCE.md so I can bind [X] in a Resources/Templates docx.`

---

## General / skill activation

- `user report template seed`
- `Resources/Templates embedded template`
- `UserReportTemplateUpdater / EnsureTemplateExists`
- `visa2026-user-report-templates skill`
