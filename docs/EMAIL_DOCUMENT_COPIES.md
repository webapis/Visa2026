# Email document copies

Officers can email the scanned copies of a person's documents (Passport, Visa, Work permit, Invitation, Education, Family proof, Medical record, Address of residence) straight from Visa2026. They can send for one person or for a group. By default the email goes out from **one shared SMTP account** (an Office 365 mailbox). The officer is set as **Reply-To** and gets a **Bcc copy**. Recipients are stored in an address book, so officers do not retype them.

| | |
|---|---|
| **Status** | Version 1 implemented (Person popup + split) |
| **Entry points** | Toolbar action **Email copies** on Employee and Family member Person lists (multi-select) and the matching Person detail views; **Resend** on Sent emails |
| **UI host** | Centered popup (`EmailDocumentCopiesDialogHost` in `_Host.cshtml`) |
| **Transport** | **SMTP** (default, `OfficerMail:Provider = Smtp`): shared account, officer as Reply-To + Bcc. Settings live on **Configuration → Mail server** (`EmailSmtpSettings`); appsettings / `SmtpMail__Password` are a fallback. **Microsoft Graph** (secondary, `OfficerMail:Provider = MicrosoftGraph`): each officer's own mailbox; hidden unless switched on |
| **Attachments** | One merged PDF per person; a person over the limit is split by document type (`_part1`, `_part2`) |
| **Prototypes** | See §10 |

---

## 1. Officer workflow

1. Select one or more people on the **Employee** or **Family member** list (or open one Person) and click **Email copies**.
2. **Step 1, Documents.** Tick document **types** once for everyone (Passport and Visa are on by default). Only the **current** record of each type is used, unless **Include all records** is on. Family proof always includes every file. The right side shows one PDF card per person, or **Will be sent as N emails** when the pack is over the chosen max (10 / 15 / 20 MB). Records without a scan show as **No file** and are skipped.
3. **Step 2, Compose.**
   - **SMTP (default):** the From line shows the shared account. **Your email (replies and copy)** is pre-filled from the officer's user record (`ApplicationUser.Email`). If it is empty, the officer types it once and it is remembered after the first send.
   - **Microsoft Graph only, first time:** click **Connect my Outlook**. A Microsoft sign-in opens in a new tab. When the officer accepts, the panel detects the connection on its own (it checks every 3 seconds for 5 minutes; **Check again** forces a check).
   - **To / Cc:** start typing a name, organization or address. Suggestions come from the shared address book and the officer's personal contacts, most used first. Press Enter, comma or semicolon to add an address you typed.
   - **Template:** the default template fills in the subject and body. Placeholders are replaced when the template is chosen, and you can edit the result.
   - **Attachments:** one `{Person_Name}_documents.pdf` per person (or `_part1` / `_part2` when a person was split).
   - **Save new addresses to my address book** (on by default) adds unknown addresses as personal contacts. The chosen max size is remembered on those contacts and can lower the next send.
4. **Send.** One email, or N emails when split. The result shows sent / failed / partial, with **View in Sent emails** or **Try again**.

**Sent history:** **Email > Sent emails** lists every attempt, both sent and failed (failed rows are red). A split send is several rows with the same batch and a **Part** column (`2 / 3`). **Resend** reopens the popup on Compose with the same people, records, recipients, subject and body.

**Per-person log:** Person detail **Emailed documents** tab (`SentEmailPerson`) answers "what was emailed for this person, when, and to whom".

---

## 2. Decisions

| Topic | Decision |
|-------|----------|
| Main transport | **SMTP** through one shared Office 365 mailbox (`smtp.office365.com:587`, STARTTLS). Microsoft Entra app registration was not possible. Settings live in appsettings; the password only in the `SmtpMail__Password` environment variable. |
| Officer identity with SMTP | From = shared account; **Reply-To** = officer; **Bcc** = officer (`SmtpMail:BccOfficer`), so the email is also in the officer's own mailbox. The officer address is stored on `ApplicationUser.Email`. |
| Secondary transport | **Microsoft Graph** `/me/sendMail` from each officer's own mailbox (delegated, per-officer consent). The code is kept; switched on only by `OfficerMail:Provider = MicrosoftGraph`. No automatic fallback between the two (risk of duplicate emails). |
| SMTP end of life | Exchange Online disables SMTP AUTH basic (username + password) by default after **December 2026**. Plan the move to Graph (or a company relay) before then; see §6.1. |
| Family proof | `PersonFamilyRelationDocument` |
| Entry | Employee and Family member Person lists (multi-select) and those Person detail views. |
| Scope | Document types ticked once for all selected people; current record only unless "include all" |
| Attachment format | One merged PDF per person (`PersonDocumentCopyPdfMerger`); split by type when one person exceeds the limit |
| Address book | Shared contacts plus each officer's personal contacts |
| Version 1 extras | Templates, Sent history with Resend, per-person log |
| Not in version 1 | Contact groups, director approval, record preview inside the panel, "Open in Outlook" draft fallback |

---

## 3. Document types

The officer ticks types once. Keys come from `PersonLinkedDocumentsResolver` (same catalog as Person document copies).

| Type | Record keys | Current record |
|------|-------------|----------------|
| Passport | `Passport:{id}` | `IsCurrent`, else latest |
| Visa | `Passport:{pid}/Visa:{vid}` | `IsCurrent`, else latest |
| Work permit | `WorkPermitItem:{id}` (employees) | `IsCurrent`, else latest |
| Invitation | `InvitationItem:{id}` (employees) | `IsCurrent`, else latest |
| Address of residence | `AddressOfResidence:{id}` (+ lodging nested key) | `IsCurrent` address, plus its lodging |
| Education | `Education:{id}` | `IsCurrent`, else latest |
| Family proof | `FamilyRelationDocument:{id}` (non-employees) | all records with a file |
| Medical record | `MedicalRecord:{id}` | `IsCurrent`, else latest |

A type with no record, or whose chosen record has no scan, is shown as **No file** and skipped.

---

## 4. Architecture and files

```
Person list / detail ──► EmailDocumentCopiesController (Module)
                           │  selected Person ids (Resend: fixed record keys)
                           ▼
                    IEmailDocumentCopiesDialogService.Open
                           ▼
                    EmailDocumentCopiesDialogHost.razor (centered popup)
                           │  step 1: EmailDocumentCopiesPlanner (types, current, split)
                           │  step 2: EmailContactDirectory, EmailTemplateRenderer
                           ▼
                    EmailDocumentCopiesSendService (Module)
                           │  AttachmentBuilder → per-record PDFs → split by real size → send each part
                           │  IOfficerMailSender ──► SmtpOfficerMailSender (default) or Graph
                           ▼
                    SentEmail (+ BatchId / Part) + SentEmailPerson, contact usage + MaxEmailSizeMb
```

### Module (`Visa2026.Module`)

| File | Purpose |
|------|---------|
| `BusinessObjects/Email/EmailContact.cs` | Address book entry (shared or personal; usage counters) |
| `BusinessObjects/Email/EmailTemplate.cs` | Subject/body templates with placeholders |
| `BusinessObjects/Email/SentEmail.cs` | Sent history (status Sent/Failed, recipients, body, attachment summary) |
| `BusinessObjects/Email/SentEmailPerson.cs` | Per-person line: records included, attachment file name |
| `BusinessObjects/Email/OfficerMailConnection.cs` | One row per connected officer: account address and encrypted refresh token |
| `DatabaseUpdate/EmailDocumentCopiesSchemaSql.cs` | Idempotent PostgreSQL `CREATE TABLE IF NOT EXISTS` (run at host start) |
| `DatabaseUpdate/EmailDocumentCopiesSchemaUpdater.cs` | Same SQL from the XAF updater, plus seeding the default "Document copies" template |
| `DatabaseUpdate/EmailDocumentCopiesOfficerPermissions.cs` | Users role grants (§5) |
| `Controllers/EmailDocumentCopiesController.cs` | **Email copies** on Person lists/details and **Resend** |
| `Services/EmailDocumentCopies/EmailDocumentCopiesPlanner.cs` | Document-type catalog, current-record pick, split into emails |
| `Services/EmailDocumentCopies/EmailDocumentCopiesDialogService.cs` | Circuit-wide open/close for the popup |
| `Services/EmailDocumentCopies/EmailDocumentCopiesAttachmentBuilder.cs` | Per-record PDFs, then one merged file per planned part |
| `Services/EmailDocumentCopies/EmailDocumentCopiesSendService.cs` | Validate, build, split by real size, send each part, record history |
| `Services/EmailDocumentCopies/EmailContactDirectory.cs` | Visible contacts, search, usage and auto-save |
| `Services/EmailDocumentCopies/EmailTemplateRenderer.cs` | Placeholder rendering |
| `Services/EmailDocumentCopies/IOfficerMailSender.cs` | Transport contract (implemented in the host) |
| `Person.EmailedDocuments` | Inverse collection of `SentEmailPerson` (Issued documents tab) |

### Blazor host (`Visa2026.Blazor.Server`)

| File | Purpose |
|------|---------|
| `Components/EmailDocumentCopiesDialogHost.razor` | Centered two-step popup + result |
| `wwwroot/css/email-document-copies.css` | Popup styles (`email-doc-copies__*`) |
| `Mail/SmtpMailOptions.cs` | `SmtpMail` config section |
| `Mail/SmtpOfficerMailSender.cs` | SMTP sender (MailKit): shared From, officer Reply-To + Bcc |
| `Mail/MicrosoftGraphMailOptions.cs` | `MicrosoftGraphMail` config section |
| `Mail/OfficerMailEndpoints.cs` | `/visa-mail/connect` and `/visa-mail/callback` (OAuth code flow with PKCE) |
| `Mail/OfficerMailTokenService.cs` | Redeem code, store encrypted refresh token, cache access tokens |
| `Mail/GraphOfficerMailSender.cs` | Graph `sendMail`, or a draft with upload session for large attachments |
| `Mail/OfficerMailServiceCollectionExtensions.cs` | DI registration (`AddVisaOfficerMail`); `OfficerMail:Provider` picks SMTP or Graph |
| `Startup.cs` | `AddVisaOfficerMail`, `MapVisaOfficerMail`, `EmailDocumentCopiesSchemaSql.ApplyIfMissing` |

### Template placeholders

| Placeholder | Value |
|-------------|-------|
| `{PersonNames}` | Comma-separated names of the people attached |
| `{PersonCount}` | Number of people |
| `{DocumentList}` | One line per person: `- Name: Passport, Visa, …` |
| `{OfficerName}` | Sending officer's display name |
| `{Date}` | Today, `dd.MM.yyyy` |

---

## 5. Data and permissions

Tables (PostgreSQL, created by `EmailDocumentCopiesSchemaSql`, no EF migrations): `EmailContacts`, `EmailTemplates`, `SentEmails`, `SentEmailPersons` (FK to `SentEmails`, cascade delete), `OfficerMailConnections`.

**Users role** (`EmailDocumentCopiesOfficerPermissions.Ensure`, called from `Updater.CreateUserRole`):

| Type | Access |
|------|--------|
| `EmailContact` | Create. Read/Write/Delete only for shared contacts or contacts the officer owns. |
| `EmailTemplate` | Full access |
| `SentEmail`, `SentEmailPerson` | Read and Create; no edit or delete (audit history) |
| `OfficerMailConnection` | Denied (accessed only through the non-secured object space in the host) |
| Navigation | `Email` group: Address book, Templates, Sent emails |

The **Email copies** action is shown only when the user may create `SentEmail`.

---

## 6. Setup

### 6.1 SMTP (default)

1. **Pick one mailbox** to send from, for example `cetrm.vize@calikenerji.com`. A licensed user mailbox works; a shared mailbox also works if you log in with an account that has **Send As** on it.
2. **Enable Authenticated SMTP for that mailbox.** In Microsoft 365 admin center: **Users > Active users > (mailbox) > Mail > Manage email apps > Authenticated SMTP**. It is off by default in most tenants, and only an Exchange or Microsoft 365 admin can turn it on.
3. **No MFA prompt for that account.** SMTP cannot answer an MFA challenge. Security defaults or a Conditional Access policy that forces MFA will reject the login (`535 5.7.139`).
4. **Enter the mailbox in the app** (preferred): sign in as **Admin** or **VisaOffice** → **Configuration → Mail server**. Defaults are `smtp.office365.com`, port **587**, STARTTLS. Fill:
   - **From address** — the mailbox, e.g. `cetrm.vize@calikenerji.com`
   - **User name** — Office 365 usually needs that **same full address**, not only a short account such as `G31207`. If the full address fails, try `G31207@calikenerji.com`, then the short name.
   - **New password** — the mailbox password (saved encrypted; leave blank later to keep it)
   - Click **Save**, then **Test connection**
5. **Optional fallback** (empty Mail server row only): `SmtpMail` in appsettings and password in env `SmtpMail__Password`. Never commit a password.

- `FromAddress` must be an address the login may send as, otherwise Office 365 answers `554 5.2.252 SendAsDenied`.
- While Host or From address is empty the compose dialog tells the officer to ask an administrator to open **Configuration → Mail server**.
- Outbound TCP **587** to `smtp.office365.com` must be open from the server.

| Error shown to the officer | Usual cause |
|----------------------------|-------------|
| "rejected the username or password" | Wrong password, Authenticated SMTP off for the mailbox, MFA enforced, or tenant-wide SMTP AUTH disabled |
| "refused the message: … SendAsDenied" | `FromAddress` differs from the login and Send As is not granted |
| "Could not reach the mail server" | Port 587 blocked by firewall/proxy, wrong `Host` |

**End of 2026.** Microsoft disables SMTP AUTH basic authentication in Exchange Online by default after December 2026. When that happens, either an admin re-enables it for the tenant (if still allowed), or switch to Microsoft Graph (§6.2) or a company SMTP relay (same `SmtpMail` settings, different `Host`).

### 6.2 Microsoft Graph (secondary, switched off by default)

Switch on with `"OfficerMail": { "Provider": "MicrosoftGraph" }`. The steps below are done **once by the Visa2026 developer**, in any Entra tenant they control (it can be the company tenant). Officers then consent individually.

#### Register the app

1. Go to [Entra admin center](https://entra.microsoft.com) > **App registrations** > **New registration**.
   - Name: `Visa2026 Mail`
   - Supported account types: **Accounts in any organizational directory** (multi-tenant). Single tenant is fine if the app lives in the company tenant; then set `Tenant` to that tenant id.
   - Redirect URI: platform **Web**, `https://<slot-host>/visa-mail/callback`
2. Add one redirect URI per slot (Production, Staging, Demo, local `https://localhost:<port>/visa-mail/callback`). It must match exactly, including port.
3. **Certificates & secrets** > **New client secret**. Copy the value.
4. **API permissions** > Microsoft Graph > **Delegated**: `offline_access`, `openid`, `profile`, `User.Read`, `Mail.Send`, `Mail.ReadWrite`. Do **not** add Application permissions; those always need admin consent.

`Mail.ReadWrite` is used only to create a draft and upload attachments larger than 3 MB. Smaller messages use `Mail.Send` alone.

#### Configure each slot

```json
"MicrosoftGraphMail": {
  "ClientId": "<application (client) id>",
  "ClientSecret": "",
  "Tenant": "organizations",
  "PublicBaseUrl": "https://visa.example.local",
  "MaxTotalAttachmentBytes": 20971520
}
```

- Put the **secret in an environment variable only**: `MicrosoftGraphMail__ClientSecret` (IIS slot env file or app pool environment). Never commit it.
- `PublicBaseUrl` builds the redirect URI. Set it when the app runs behind a proxy or the request host differs from the registered URI. Leave empty to use the request's scheme and host.
- `Tenant`: `organizations` (any work account) or the company tenant id or domain.
- When `ClientId` or `ClientSecret` is missing, the panel shows "Email sending is not set up on this server yet" and Send stays disabled.

**Data Protection keys.** Refresh tokens are encrypted with ASP.NET Data Protection. If the key ring is lost (for example a container restart without a persisted key folder), officers just reconnect once. To persist keys, set `DataProtection:KeysPath` (env `DataProtection__KeysPath`) to a folder the app pool can write. Moving or changing the key ring also ends existing Blazor login cookies once.

**Network.** The server needs outbound HTTPS to `login.microsoftonline.com` and `graph.microsoft.com`.

#### When the company blocks user consent

If the tenant disables user consent, the callback page shows "Your company requires an administrator to approve Visa2026" (detected from `AADSTS65001`, `AADSTS90094` and admin-approval errors). A Microsoft 365 admin approves the app once, through **Enterprise applications > Visa2026 Mail > Permissions > Grant admin consent**, or through the admin consent workflow if it is enabled. After that, officers connect as normal.

---

## 7. How sending works

**SMTP:** one connection per send (MailKit): connect with `Security`, log in, send a single MIME message (plain-text body, PDFs attached, From = shared account, Reply-To and Bcc = officer), disconnect. The officer's address is saved on their user record after the first send. Failures are recorded in Sent emails with the server's error text.

**Microsoft Graph:**

1. **Connect.** The panel mints a time-limited (10 minute) Data Protection token holding the officer's user id and opens `/visa-mail/connect?t=…` in a new tab. The endpoint creates OAuth `state` and a PKCE verifier, stores them in an HttpOnly cookie (`visa_mail_oauth`, path `/visa-mail`, 15 minutes), and redirects to Microsoft with `prompt=select_account`.
2. **Callback.** It validates the cookie and state, redeems the code, reads `/me` for the mailbox address, and saves `OfficerMailConnection` with the refresh token encrypted. Then it shows a small localized result page.
3. **Send.** Access tokens are cached in memory and refreshed from the stored refresh token. If the encoded attachments are under about 3 MB, the message goes in one `POST /me/sendMail`. Otherwise a draft is created, each attachment is uploaded in chunks through `createUploadSession`, and the draft is sent. If sending fails, the draft is deleted.
4. **Revoked or expired consent** (`invalid_grant`, `interaction_required`, Graph 401): the connection row is deleted and the panel asks the officer to connect again. The failure is still recorded in Sent emails.

**Size limits.** Exchange Online accepts up to about 35 MB for a whole message after encoding. Visa2026 lets the officer pick **10 / 15 / 20 MB** per email (never above `MaxTotalAttachmentBytes`). Over the limit, the pack is split into several emails (see §10.1) instead of being blocked.

---

## 8. Deferred and follow-ups

- **Preview inside the panel**: opening a record preview would replace the email panel in the single preview slot. Needs a nested preview or a split view.
- **Open in Outlook fallback**: create the draft only and give the officer a link to finish in Outlook (useful while consent is pending).
- **Contact groups** (for example "Migration service – all inspectors").
- **Director approval** before sending.
- **Person entry point**: shipped — **Email copies** on Employee / Family member lists and Person detail.
## 9. Related

- [`PERSON_DOCUMENT_COPIES.md`](PERSON_DOCUMENT_COPIES.md): record catalog and PDF merge that this feature reuses
- [`PERSON_DETAIL_NESTED_COLLECTION_TABS.md`](PERSON_DETAIL_NESTED_COLLECTION_TABS.md): **Emailed documents** issued tab
- [`ROLE_PERMISSIONS_GUIDE.md`](ROLE_PERMISSIONS_GUIDE.md): role grants
- [Microsoft Graph `sendMail`](https://learn.microsoft.com/graph/api/user-sendmail), [large attachments](https://learn.microsoft.com/graph/outlook-large-attachments)

## 10. UI prototypes

Target design (supersedes the first per-record panel): start from the **Employee** or **Family member** list, pick document **types** once for all selected people (current record only by default), one PDF per person, centered popup.

| Image | Screen |
|-------|--------|
| [04](prototypes/email-document-copies-04-person-list-select.jpg) | Employee list, several people ticked, **Email copies (n)** toolbar action |
| [05](prototypes/email-document-copies-05-select-documents-dialog.jpg) | Popup step 1: document types, "Current only", one PDF per person with gaps and size meter |
| [06](prototypes/email-document-copies-06-compose.jpg) | Popup step 2: From (shared SMTP), your email, To/Cc with address-book suggestions, template, subject, message, attachments |
| [07](prototypes/email-document-copies-07-result.jpg) | Result: sent (with skipped documents) and failed (Try again) |
| [08](prototypes/email-document-copies-08-sent-history.jpg) | Email > Sent emails: list + detail with people/documents and **Resend** |
| [09](prototypes/email-document-copies-09-address-book.jpg) | Email > Address book: shared / personal contacts, edit card |
| [10](prototypes/email-document-copies-10-templates.jpg) | Email > Templates: editor, insert placeholders, live preview |
| [11](prototypes/email-document-copies-11-person-emailed-tab.jpg) | Person detail: **Emailed documents** tab (per-person log) |
| [12](prototypes/email-document-copies-12-warning-states.jpg) | Warnings: person without files, no recipient, SMTP not set up (over-size is the split preview in 13) |
| [13](prototypes/email-document-copies-13-split-preview.jpg) | Over the limit: "Will be sent as 3 emails", per-email groups, max-per-email picker, one person split by document |

### 10.1 Splitting into several emails

When the selected PDFs are larger than the per-email limit, the send is split instead of blocked.

| Rule | Decision |
|------|----------|
| Trigger | Officer sees "Will be sent as N emails" with the grouping in step 1 **before** sending; the Send button reads **Send N emails**. Never silent. |
| Grouping | Whole person PDFs, filled in selection order up to the limit. One person's PDF is never split across emails unless it alone is over the limit. |
| One person over the limit | That person's PDF is split **by document type** into parts (`Name_documents_part1.pdf`: Passport + Visa; `…_part2.pdf`: Work permit + Address), each part placed in its own email as needed. A single document file larger than the limit cannot be sent and is shown as a warning. |
| Limit | Officer picks **10 / 15 / 20 MB** per send (default 20 MB, never above `SmtpMail:MaxTotalAttachmentBytes`). Remembered per recipient contact, so government addresses with small mailboxes keep 10 MB. |
| Message | Same To / Cc / template / body in every part; subject suffix `(1 of 3)`; one body line "Part 1 of 3 – contains: …". |
| Sending | Parts sent one after another; a failed part does not stop the others. Result: "2 of 3 emails sent" with the failed part listed. |
| History | One `SentEmail` row per part, linked by a batch id and part number ("Batch … – 3 parts"); **Resend** on a failed part resends only that part. The person's Emailed documents tab shows which part carried their PDF. |
