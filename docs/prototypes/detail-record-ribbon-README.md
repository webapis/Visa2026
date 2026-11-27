# Detail record ribbon

**Status:** implemented. The live ribbon is `DetailRecordRibbon` on the case workspace and the person intake wizard.

Clickable prototype for the record commands on an open detail view.

| Screen | What it stands in for |
|--------|------------------------|
| Application Profile Instance | Case workspace, Application tab |
| Employee | Person intake, Identity |
| Family member | Person intake, Identity |

Open [detail-record-ribbon-preview.html](./detail-record-ribbon-preview.html) in a browser. Switch screens at the top. This file does not change the application.

## What to try

1. Change a field. **Save** lights up and the ribbon shows **Unsaved changes**.
2. **Save**. The record stays open and returns to **Saved**.
3. **Save and Close**. You land on that screen’s list. The record ribbon is gone.
4. Click the row. The detail opens again with the ribbon.
5. Change a field, then **Close** or **New**. Choose save, discard, or cancel.
6. On the case, click **People & docs** or **Invitation**. Those stay on the case and are not ribbon commands.
7. On Employee or Family member, **Next** stays in the footer.

## Commands

| Button | Does |
|--------|------|
| **New** | Creates another record of the same kind. Asks first when the open record is dirty. |
| **Save** | Updates the open record and stays. Disabled when nothing has changed. |
| **Save and Close** | Updates, then returns to the list. When nothing has changed, it only closes. |
| **Close** | Returns to the list. Asks first when the open record is dirty. |
| **Delete** | Asks, then deletes the open record and returns to the list. A new person who was never saved is closed without writing a row. A locked case keeps Delete disabled. |

**Update** is **Save** on the record already open. There is no second Update button and no read-only mode.

## Assumed for this prototype

- Record commands are New, Save, Save and Close, Delete, and Close. The rest of the VISA2014 Home ribbon is not here: Refresh, Show in Report, Previous, Next, Save and New.
- The ribbon is shown only while one of these three details is open. The list keeps a plain **New**.
- Person intake keeps **Back** and **Next** in the footer. **Save** and **Save and Close** move from the footer into the ribbon.
- The case keeps People & docs, Invitation, Rejection, Issued docs, Link existing, and Preview document copies under the ribbon.
- Captions are English.
- Other business objects are unchanged.
- Family member **Merjen Atayeva** is sample data. The case and Fatih DANA match the current screens.

## Correct before implementation

- Add Save and New, Previous, or Next on the ribbon.
- Keep Save and Save and Close in the person-intake footer as well as the ribbon.
- Use a separate Update button that turns a read-only detail into an editable one.
- Hide New on the case, because a new Application Profile Instance is started from the list.
