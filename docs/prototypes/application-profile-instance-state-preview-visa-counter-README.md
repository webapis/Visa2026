# Ýagdaý syny — visa issue counter on Entäk berilmedi

Proposed. Not implemented. Image text can be misread; this file is the source of truth.

| File | What it shows |
|------|----------------|
| [application-profile-instance-state-preview-visa-counter.jpg](./application-profile-instance-state-preview-visa-counter.jpg) | Left: passports still waited. Right: every visa issued. |

## Locked

| Decision | Choice |
|----------|--------|
| Profile | Wiza we Iş Rugsatnamasyny Uzaltmak only |
| When | After every expected work permit is issued. Same clock as the list row (WorkPermit.IssuedDate, Configuration → Wiza berilmeli möhlet, default 45 / warning 5) |
| Other Entäk berilmedi lists | Plain names. No counter |
| Waiting | Issued visas stay in the Berlen wizalar table. The name list heading is **Entäk berilmedi · Pasportyna garaşylýar** (localized). One chip on the right of that heading: teal **32 gün** while more than 5 days remain, amber for the last 5 days and day 0 |
| Every visa issued | The name list is gone. Under the issued-visa table: **Wizalar berildi** and the frozen chip (teal **36 gün** in the mock). Same frozen number as the list |
| Sheet background | Unchanged. The chip does not recolor the sheet |

Sample numbers and names are layout only.