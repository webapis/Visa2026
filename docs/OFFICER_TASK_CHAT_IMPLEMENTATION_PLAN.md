# Officer Task Chat — Implementation Plan

Status: **Planned** (not started)  
Last updated: 2026-05-29  
Architecture: **Custom build** (no third-party chat app or OSS library)

---

## 1. Purpose

Visa officers need **task-scoped messaging** inside Visa2026: coordinate on an `Application` (and related records), share files, mark messages with operational meaning, and work in **1:1** or **group** threads — with everything **persisted in the project database**.

This plan is separate from:

| Surface | User job |
|---------|----------|
| **State notifications** ([`STATE_NOTIFICATIONS_IMPLEMENTATION_PLAN.md`](STATE_NOTIFICATIONS_IMPLEMENTATION_PLAN.md)) | System-generated alerts (validity, missing data) |
| **State change log** (`StateChangeLog`) | Automated audit when configured rules fire |
| **Application progress** (`ApplicationProgress`) | Formal workflow state history on an application |
| **Officer task chat** (this doc) | Human coordination, questions, decisions, attachments |

Officer chat may **reference** application state but must not replace `ApplicationProgress` or the state-evaluation engine.

---

## 2. Goals and non-goals

### 2.1 Goals

- Chat anchored to **work context** (primarily `Application`; optionally `Person`, `ApplicationItem`).
- **Direct (1:1)** and **group** threads with explicit participants (`ApplicationUser`).
- **Edit** and **delete** own messages (with clear UI history rules).
- **Upload images and files** (reuse XAF `FileData` / File Attachments module).
- **Mark messages** with a small, officer-facing **message state** (not BO validity state).
- **Unread** indicators and optional real-time delivery.
- **Security**: only participants (and configured admins) can read a thread.
- **Localization** (Layer A): chrome and enums via `VisaUiMessages` / `GenerateModelLocalization`.

### 2.2 Non-goals (initial releases)

- Chat with external persons (applicants, sponsors) — officers only.
- End-to-end encryption.
- Full Slack/Teams feature parity (threads-on-threads, reactions, bots, voice).
- Replacing email for official correspondence.
- Mobile-native app (Web API hooks may be added later; Blazor UI first).

---

## 3. UX principles

### 3.1 Where officers open chat

| Entry point | Behavior |
|-------------|----------|
| **Application detail view** | Primary: tab or side panel “Chat” scoped to that application |
| **Global header** | Icon + unread badge → inbox of threads (recent / unread first) |
| **Person detail view** (optional Phase 4) | Filter threads linked to that person’s applications |
| **Start direct message** | Pick colleague → create or reopen 1:1 thread |

### 3.2 Message state marks (conversation semantics)

Distinct from **BO state tracking** ([`BO_STATE_TRACKING.md`](BO_STATE_TRACKING.md)). These labels help officers scan a thread:

| `ChatMessageMark` (proposed) | Meaning |
|------------------------------|---------|
| `None` | Normal message (default) |
| `Question` | Needs an answer |
| `ActionRequired` | Someone must do something |
| `Decision` | Recorded decision |
| `Blocked` | Work is blocked |
| `Resolved` | Issue discussed is closed |
| `FYI` | Informational only |

**Rules (recommended):**

- Any participant can set/clear a mark on any message in the thread (or restrict to author + mark — **open decision §12**).
- Marks are **optional**; most messages stay `None`.
- Optional Phase 4: “Promote to Application progress” action copies mark + excerpt into `ApplicationProgress.Description` (manual, officer-confirmed) — does **not** auto-change `ApplicationState`.

### 3.3 Edit and delete behavior

| Action | Persisted behavior | UI |
|--------|-------------------|-----|
| **Edit** | Keep `Body`; set `EditedAt`, `IsEdited`; optional `EditHistory` child rows for audit | Show “(edited)” + timestamp |
| **Delete** | Soft delete: `IsDeleted`, `DeletedAt`, `DeletedBy`; body hidden from officers | “Message deleted” placeholder |
| **Admin** | Optional hard purge policy for retention (later) | — |

Only the **author** may edit/delete (enforced in service layer + XAF security).

### 3.4 Attachments

- Reuse **`FileData`** (same as `DocumentBase`).
- **Images**: inline thumbnail in chat; click to open/download.
- **Documents**: icon + filename + size; reuse content validation patterns from [`DocumentFileUploadConstraints.cs`](../Visa2026.Module/Services/DocumentFileUploadConstraints.cs).
- Chat may allow **Office types** (`.docx`, `.xlsx`) in addition to scan/PDF types — define `ChatFileUploadConstraints` so visa document rules stay unchanged.

---

## 4. Architecture decision

### 4.1 Custom domain model + custom Blazor UI (**chosen**)

Third-party open-source chat apps and embeddable libraries were considered and **rejected** (2026-05-29): they do not align with XAF security, task-scoped `Application` context, SQL Server in the same schema, DevExpress Blazor UI, or custom message marks without heavy integration cost.

DevExpress XAF provides **Notifications**, **File Attachments**, and **Audit Trail**, but **no first-class chat module**. The proven pattern in this repo is:

1. **EF entities** in `Visa2026.Module/BusinessObjects/Chat/`
2. **Services** in `Visa2026.Module/Services/Chat/`
3. **Custom property editor + Blazor component** in `Visa2026.Blazor.Server` (same approach as `BoStateNotificationInboxComponent`)
4. **XAF security** on types and object-level criteria (participant membership)
5. **SignalR hub** for push (host already registers SignalR for XAF Blazor — extend with an app-specific hub)

**Why not only standard ListView/DetailView?** Officer chat needs a fixed conversation layout, inline compose, attachments, and live updates — poor fit for generic XAF grids.

**Why not external chat (Teams/Slack)?** Task context (`Application`, person, deep links) and compliance retention stay inside Visa2026’s DB and security model.

### 4.2 Real-time strategy

| Phase | Delivery |
|-------|----------|
| MVP | Poll on focus / manual refresh + send via service |
| Phase 3 | `ChatHub` (SignalR): `MessageSent`, `MessageEdited`, `MessageDeleted`, `MarkChanged`, `ParticipantAdded` |
| Fallback | Polling interval when SignalR disconnected |

Blazor Server circuits alone do not broadcast across users; **SignalR is required** for multi-user live threads.

### 4.3 API surface

Initial implementation: **in-process services** called from Blazor components via `IObjectSpaceFactory` / scoped services.

Optional later: OData or minimal REST under existing Web API (`Visa2026.Blazor.Server/WebApi/`) for automation or a future mobile client — not Phase 1.

---

## 5. Data model (proposed)

Namespace: `Visa2026.Module.BusinessObjects.Chat`

### 5.1 Entities

```
ChatThread
├── Title (optional; auto for task threads)
├── ThreadType: Task | Direct | Group
├── Application (nullable for pure Direct messages; required for Task)
├── Person (optional shortcut link)
├── CreatedAt, CreatedBy (ApplicationUser)
├── LastMessageAt (denormalized for inbox sort)
├── IsArchived
└── Participants (ChatParticipant, aggregated)

ChatParticipant
├── Thread (ChatThread)
├── User (ApplicationUser)
├── JoinedAt
├── LastReadAt (nullable → unread count)
├── Role: Member | Admin (group admin can add/remove)
└── IsActive (left thread)

ChatMessage
├── Thread (ChatThread)
├── Author (ApplicationUser)
├── Body (text, MaxLength TBD e.g. 8000)
├── Mark (ChatMessageMark enum)
├── SentAt
├── EditedAt, IsEdited
├── IsDeleted, DeletedAt, DeletedBy
└── Attachments (ChatMessageAttachment, aggregated)

ChatMessageAttachment
├── Message (ChatMessage)
├── File (FileData, aggregated)
├── UploadedAt
└── ContentKind: Image | Document | Other (derived from extension)

ChatMessageEditHistory (optional Phase 2)
├── Message, PreviousBody, EditedAt
```

### 5.2 Indexes (EF / SQL)

- `ChatMessage (ThreadId, SentAt)` — conversation load
- `ChatThread (ApplicationId, LastMessageAt DESC)` — application tab
- `ChatParticipant (UserId, LastReadAt)` — unread queries

### 5.3 DbContext

Register sets in `Visa2026DbContext` alongside existing entities; migration via normal XAF EF update pipeline.

---

## 6. Security and permissions

### 6.1 Type permissions (`Updater.cs`)

| Role | ChatThread | ChatMessage | Notes |
|------|------------|-------------|-------|
| **Administrator** | Full | Full | Support / moderation |
| **User** (officers) | Read/create per object criteria | Create; edit/delete own | No global list of all threads unless participant |

### 6.2 Object-level criteria (example)

- **Read thread/message:** current user is active `ChatParticipant` for the thread **OR** user has admin chat permission.
- **Create message:** participant + thread not archived.
- **Edit/delete message:** `Author.ID == CurrentUserId()` and not deleted.

Use `CurrentUserIdOperator` (same pattern as audit trail read filter in `Updater.cs`).

### 6.3 Retention

- Soft-deleted messages remain for audit unless policy says otherwise.
- Align max upload size with `FileUpload:MaxRequestBodyBytes` in host config (already 10 MB default in `Startup.cs`).

---

## 7. Services

| Service | Responsibility |
|---------|----------------|
| `IChatThreadService` | Create task/direct/group thread; add/remove participants; archive |
| `IChatMessageService` | Send, edit, soft-delete; set mark; load page of messages |
| `IChatInboxService` | Threads for current user, unread counts, last preview text |
| `IChatAttachmentService` | Validate file, attach to message, serve download URL |
| `IChatNotificationBridge` | On new message → XAF `NotificationsModule` row + header badge refresh |
| `ChatHub` (Blazor.Server) | Push events to thread groups |

All write paths run in a **single ObjectSpace commit** per user action (message + attachments together).

---

## 8. UI implementation map

Follow the **State notifications** split:

```
Visa2026.Module/
  BusinessObjects/Chat/           # entities + enums
  Controllers/                    # navigation, hide Save on host shells
  DatabaseUpdate/                 # nav items, detail views, security seed
  Editors/ChatEditorAliases.cs
  Services/Chat/                  # interfaces + implementations

Visa2026.Blazor.Server/
  Editors/ChatPanelPropertyEditor.cs
  Editors/ChatPanelModel.cs
  Components/Chat/
    ChatPanelComponent.razor      # embedded on Application
    ChatInboxComponent.razor      # global inbox host
    ChatMessageList.razor
    ChatCompose.razor
    ChatAttachmentPreview.razor
  Components/ChatHeaderBadge.razor
  Hubs/ChatHub.cs
  Services/ChatNavigationHelper.cs
  wwwroot/css/site.css            # chat-* classes
```

### 8.1 Application detail integration

- Non-persistent **`ChatPanelHost`** BO (like `BoStateNotificationInboxHost`) with `[EditorAlias]` → `ChatPanelComponent`.
- `Application` detail layout: new tab **Chat** bound to host; host carries `Application` key passed into component.
- Controller opens existing thread for application or creates **Task** thread on first message.

### 8.2 Localization

Add keys under `Chat.*` in `tools/GenerateModelLocalization/UiStrings.messages.json`; regenerate catalog. User-authored message bodies are **not** localized.

---

## 9. Phased delivery

### Phase 0 — Foundation (Module only)

- [ ] EF entities + `DbContext` registration
- [ ] Security rules in `Updater.cs`
- [ ] `IChatThreadService` / `IChatMessageService` with unit-testable logic (no UI)
- [ ] Dev-only smoke: create thread + messages via test or minimal ListView

**Exit:** officers can persist chat in DB via standard XAF views (developer validation).

### Phase 1 — Task chat UI prototype

- [ ] `ChatPanelHost` + `ChatPanelComponent` on **Application** detail
- [ ] Send text messages; list history (newest at bottom)
- [ ] Manual refresh
- [ ] Basic styling consistent with state inbox

**Exit:** two officers can coordinate on an application in the UI (refresh to see new messages).

### Phase 2 — Direct, group, edit/delete

- [ ] Thread types: Direct (2 users), Group (invite list)
- [ ] Global **Chat inbox** navigation + header badge (unread count)
- [ ] Edit/delete own messages with placeholders and `(edited)`
- [ ] `LastReadAt` / unread per thread

**Exit:** full participant model and inbox usable daily.

### Phase 3 — Attachments + real-time

- [ ] `ChatMessageAttachment` + upload in compose area
- [ ] `ChatFileUploadConstraints` + size limits
- [ ] `ChatHub` + live append/edit/delete for connected clients
- [ ] Bridge to XAF notifications for offline users

**Exit:** file sharing and live updates without constant polling.

### Phase 4 — Message marks + workflow hooks

- [ ] `ChatMessageMark` UI (picker on message or context menu)
- [ ] Filter thread by mark (e.g. show open `Question` / `ActionRequired`)
- [ ] Optional: “Copy to Application progress” (manual)
- [ ] Optional: link from chat message to `Person` / `ApplicationItem`

**Exit:** officers can tag conversation state and optionally record progress.

### Phase 5 — Hardening

- [ ] Edit history (if required by compliance)
- [ ] Archive threads; search messages (full-text or simple `Contains`)
- [ ] E2E smoke test (EasyTest or Selenium): login as two users, send message
- [ ] Performance: paginate messages (e.g. 50 per load, “load older”)

---

## 10. Developer feedback from officers (recommendation)

**Keep developer feedback separate from officer task chat.** They serve different audiences and lifecycles:

| | Officer task chat | Developer / product feedback |
|--|-------------------|------------------------------|
| **Audience** | Officers ↔ officers | Officers → development team |
| **Content** | Case coordination | Bugs, UX pain, ideas |
| **Visibility** | Participants only | Dev/admin triage queue |
| **Lifecycle** | Tied to application work | Triage → fix → release |

### 10.1 Recommended approach: in-app **Feedback** module (lightweight)

Add a small, dedicated feature (can be Phase 6 or parallel track):

**`UserFeedback`** (or `ProductFeedback`) entity:

| Field | Purpose |
|-------|---------|
| `Type` | Bug / Idea / Question / Praise |
| `Severity` | Low / Medium / High (bugs) |
| `Summary` | One line |
| `Description` | Details |
| `PageUrl` / `ViewId` | Auto-captured from Blazor navigation |
| `ContextBoType` / `ContextBoId` | Optional link to open record |
| `Screenshot` | Optional `FileData` |
| `SubmittedBy`, `SubmittedAt` | Officer |
| `Status` | New → Triaged → InProgress → Done / Won’t fix |
| `DevNotes` | Internal |
| `ExternalIssueUrl` | Optional link to GitHub Issue |

**UI:** Header menu **“Send feedback”** (always available) → simple dialog; **not** mixed into application chat threads.

**Why this over chat?**

- Feedback stays **searchable and triageable** without noise in operational threads.
- Auto-context (view, record, app version from `AssemblyVersion`) reduces “what were you doing?” back-and-forth.
- Developers work in XAF ListView filters (`Status = New`) or sync to GitHub — officers never need to learn issue trackers.

### 10.2 Optional GitHub integration

If the team uses GitHub:

- On submit (or on “Promote to GitHub”), create an issue via **`gh` / GitHub API** with labels `user-feedback`, `from:visa2026`.
- Store `ExternalIssueUrl` on the feedback row.
- **Do not** require GitHub for MVP — manual export is fine for small teams.

### 10.3 Complementary practices

- **Micro-prompts** on new UI (e.g. first week after chat launch): “Was this helpful? 👍 / 👎” → writes lightweight `UserFeedback` with `Type = Praise` and rating — cheap signal without long forms.
- **Quarterly 5-question survey** (external form) for satisfaction — supplements, not replaces, in-app bugs.
- **Avoid** using officer chat channels as the primary feedback pipe — messages get buried and lack structured fields.

### 10.4 What to document when feedback ships

- [`docs/USER_FEEDBACK.md`](USER_FEEDBACK.md) — short officer-facing help (optional, when implemented).
- Link from this plan’s Phase 6 or a separate **`USER_FEEDBACK_IMPLEMENTATION_PLAN.md`** if the feedback scope grows (GitHub sync, email alerts, etc.).

---

## 11. Consistency with existing features

| Existing feature | Relationship |
|------------------|--------------|
| **State notifications** | Complementary; link from notification card to application chat optional later |
| **ApplicationProgress** | Formal state; chat may inform but must not auto-write progress without officer action |
| **Audit trail** | BO changes audited; chat uses own edit/delete history for messages |
| **Document uploads** | Same `FileData` storage; chat-specific allowed extensions |
| **Notifications module** | Use for “new chat message” when user is not in thread |

---

## 12. Open decisions

| Topic | Options | Recommendation |
|-------|---------|----------------|
| Task thread cardinality | One thread per `Application` vs multiple (e.g. per topic) | **One default thread per application**; add “topic” later if needed |
| Who can set message marks | Author only vs any participant | **Any participant** (coordination tool) |
| Group creation | Any officer vs role-gated | Any officer can create; **Admin** role for org-wide policy if needed |
| Message length | 4K / 8K / unlimited | **8000** chars with validation |
| Direct thread dedup | One row per user pair vs per “conversation” | **Reuse** existing Direct thread for same two users |
| Search | SQL `LIKE` vs full-text index | **`LIKE`/`Contains` Phase 5**; full-text if volume demands |
| Feedback vs chat | Shared vs separate | **Separate Feedback module** (§10) |

---

## 13. Implementation order (summary)

1. Phase 0 — entities, security, services  
2. Phase 1 — Application chat panel (text only)  
3. Phase 2 — inbox, 1:1/group, edit/delete, unread  
4. Phase 3 — attachments + SignalR  
5. Phase 4 — message marks (+ optional progress copy)  
6. Phase 5 — hardening + E2E  
7. **Parallel (recommended):** Feedback module (§10) — can start after Phase 1 UI patterns exist  

---

## 14. Smoke test checklist (when Phase 2+ complete)

1. Officer A opens **Application** → Chat tab → sends message.  
2. Officer B opens same application → sees message (live or after refresh).  
3. B replies in **Direct** thread with A from global inbox.  
4. A creates **Group**, invites B and C; all three send messages.  
5. A edits own message → “(edited)” visible; deletes own message → placeholder shown.  
6. Upload PNG + PDF → preview/download works; disallowed type rejected.  
7. Set mark `Question` on a message → filter shows it.  
8. Non-participant cannot open thread (security denial).  
9. Submit **Feedback** from header → row appears in admin feedback list with page context.  

---

## 15. Related documents

| Document | Role |
|----------|------|
| [`STATE_NOTIFICATIONS_IMPLEMENTATION_PLAN.md`](STATE_NOTIFICATIONS_IMPLEMENTATION_PLAN.md) | System inbox pattern to mirror for header badge + custom Blazor editor |
| [`STATE_TRACKING_IMPLEMENTATION_PLAN.md`](STATE_TRACKING_IMPLEMENTATION_PLAN.md) | BO state engine — do not conflate with chat message marks |
| [`AGENTS.md`](../AGENTS.md) | Module vs Blazor.Server boundaries |
| [`docs/ENVIRONMENTS.md`](ENVIRONMENTS.md) | DB migrations / `FORCE_XAF_DB_UPDATE` on deploy |
