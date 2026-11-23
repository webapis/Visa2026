# Person entry wizard — Employee and Family member

**Status:** prototype only. No application code. Implementation plan: [PERSON_INTAKE_WIZARD.md](../PERSON_INTAKE_WIZARD.md).

Replaces the Employee and Family member **DetailView** for create and later update. Officers enter the person and the child records (passport, education, address, medical, work, family documents) in one dialog. A waiting control on a field or on a whole child record lets them save before that information arrives. The list then shows the person under **Incomplete** until every waiting item is filled, when the person moves to **Complete**.

Issued visas, invitations, work permits, and applications stay out of this dialog. They remain read-only records and do not decide Complete vs Incomplete.

## Screens

| File | What it shows |
|------|----------------|
| [person-entry-wizard-01-employee-incomplete-list.jpg](./person-entry-wizard-01-employee-incomplete-list.jpg) | Employees list. **Incomplete** selected. **Waiting for** chips. **Open** returns to this dialog. |
| [person-entry-wizard-02-family-incomplete-list.jpg](./person-entry-wizard-02-family-incomplete-list.jpg) | Family members list. Same two tabs. Columns add Relationship and Sponsoring employee. |
| [person-entry-wizard-03-personal-step.jpg](./person-entry-wizard-03-personal-step.jpg) | New employee, Personal step. Each field has **Have it** or **Waiting**. Waiting dims the box. |
| [person-entry-wizard-04-passport-waiting.jpg](./person-entry-wizard-04-passport-waiting.jpg) | Passport step. Section switch **Not received yet** is on. The grid stays empty. |
| [person-entry-wizard-07-education-entered.jpg](./person-entry-wizard-07-education-entered.jpg) | Education step. Switch is off and one school row is entered. **Add education** is available. |
| [person-entry-wizard-06-family-link.jpg](./person-entry-wizard-06-family-link.jpg) | New family member, first step. Sponsoring employee and relationship are filled and cannot be left waiting. |
| [person-entry-wizard-05-review-update.jpg](./person-entry-wizard-05-review-update.jpg) | Later update of the same employee. Review lists what is still waiting. **Move to Complete** stays off. |
| [person-intake-wizard-child-records-edit.png](./person-intake-wizard-child-records-edit.png) ([HTML](./person-intake-wizard-child-records-edit.html)) | **Edit mode:** same wizard for Person + 1:N children. Passport step shows stacked record cards (expand/edit inline, Add, Remove) — no native Person DetailView. |

The **Complete** tab uses the same grid. The **Waiting for** column is hidden there, because those people have nothing left waiting.

## Steps

**Employee:** Personal, Photo, Passport, Education, Address, Medical, Work (position and CV), Review.

**Family member:** Link (sponsoring employee and relationship), Personal, Photo, Passport, Education, Address, Family docs, Review.

Work, salary, and CV stay on the employee path. Family relation documents stay on the family path.

## Waiting controls

| Control | Where | When it is on |
|---------|--------|----------------|
| **Have it / Waiting** pill | One property (foreign address, photo, and the other personal fields) | The box is left blank on purpose. The person stays Incomplete. |
| **Not received yet** switch | A whole child record (passport, education, address, medical, work, family docs) | No row is required. The person stays Incomplete. Turn the switch off when the document arrives, then add the row. |

**Next** on an empty required step asks the officer to enter the data or turn waiting on. A step is not skipped in silence.

These items are filled before save, with no waiting pill, so the list row can be found later:

- Employee: first name and last name
- Family member: first name, last name, sponsoring employee, and relationship

## Complete vs Incomplete

- **Save** with any waiting item keeps the person on **Incomplete**.
- **Move to Complete** is enabled only when every required step is filled.
- Filling the last waiting item and saving moves the person to **Complete**.
- **Open** on either tab opens this same dialog, on the first waiting step when one exists.

## What this leaves as it is

The current **Mark incomplete** popup and the Report Dashboard **Incomplete persons** category stay as they are until this dialog is implemented. This prototype is the officer screen that would take over create and update for Employee and Family member.