# Trigger Cursor Cloud Agent when a GitHub issue is created

Cursor Automations can run [cloud agents](https://cursor.com/docs/cloud-agent) from GitHub events, but there is **no built-in “Issue created” trigger**.

To start an agent when someone opens a new issue in this repository, use a small bridge:

**GitHub issue opened → GitHub Action → POST to Cursor webhook → cloud agent runs**

Official reference: [Cursor Automations](https://cursor.com/docs/cloud-agent/automations).

---

## What Cursor supports natively (GitHub)

| Trigger | Fires when | Good for |
|---------|------------|----------|
| **Issue comment** | Someone comments on an existing issue | Follow-up instructions on an open issue |
| **Issue label changed** | A label is added/removed on a non-PR issue | Near-auto: label new issues `cursor` / `ai` |
| **Comment `@cursor`** | Manual mention on an issue | One-off agent runs (no automation needed) |
| **Webhook Triggered** | Something POSTs to a private Cursor URL | True “on create” via GitHub Actions |

**Issue comment** does *not* run when the issue is first opened. Use the webhook path below for create-time automation.

---

## Setup: issue opened → Cursor agent

### 1. Create a Cursor automation (webhook)

1. Open [cursor.com/automations](https://cursor.com/automations) (or Automations in the Agents window).
2. Create a new automation.
3. Add trigger: **Webhook Triggered** (not GitHub → Issue comment).
4. Write a prompt that tells the agent what to do with the issue payload (triage, propose a fix, open a PR, comment, etc.).
5. Attach the repository (e.g. `webapis/Visa2026`) and branch if the agent should change code or open PRs.
6. Enable tools you need (e.g. pull request creation, comment on PR / GitHub tools, Memories).
7. **Save** the automation, then set it **Active**.

After save, Cursor shows:

- A private **webhook URL**, shaped like:
  `https://api2.cursor.sh/automations/webhook/<automation-id>`
- An **auth header / API key** (via **Generate auth header** / **Copy auth header**), shaped like:
  `Bearer crsr_...`

Opening the URL in a browser does nothing. Callers must **POST** with the Bearer token.

### 2. Store the auth token in GitHub

1. In the GitHub repo: **Settings → Secrets and variables → Actions**.
2. Create a secret, e.g. `CURSOR_WEBHOOK_AUTH`.
3. Value = the token only (`crsr_...`), **or** the full `Bearer crsr_...` string — match how your workflow uses it (examples below use the token only and add `Bearer ` in the header).

Never commit the webhook API key. Prefer not to paste full webhook URLs with live secrets into chat or public issues.

### 3. Add a GitHub Actions workflow

Also add repository secrets:

| Secret | Value |
|--------|--------|
| `CURSOR_WEBHOOK_URL` | `https://api2.cursor.sh/automations/webhook/<your-automation-id>` |
| `CURSOR_WEBHOOK_AUTH` | `crsr_...` (from Generate auth header) |

Create `.github/workflows/cursor-on-issue-opened.yml` in the repository.

**Critical — default branch:** GitHub only loads `issues:` workflows from the repository **default branch**. For this repo that is **`master`**, not `development`. Putting the file only on `development` or a feature branch means **new issues will not start the workflow**. Merge (or cherry-pick) the workflow file to `master` before testing.

Use `$GITHUB_EVENT_PATH` (the event JSON GitHub already writes on the runner) so issue titles/bodies with quotes or newlines do not break the request:

```yaml
name: Trigger Cursor on new issue

on:
  issues:
    types: [opened]

jobs:
  trigger-cursor:
    runs-on: ubuntu-latest
    steps:
      - name: Call Cursor automation webhook
        env:
          CURSOR_WEBHOOK_URL: ${{ secrets.CURSOR_WEBHOOK_URL }}
          CURSOR_WEBHOOK_AUTH: ${{ secrets.CURSOR_WEBHOOK_AUTH }}
        run: |
          PAYLOAD=$(jq '{
            title: .issue.title,
            body: (.issue.body // ""),
            url: .issue.html_url,
            number: .issue.number,
            author: .issue.user.login,
            labels: [.issue.labels[].name]
          }' "$GITHUB_EVENT_PATH")

          curl -fsS -X POST "${CURSOR_WEBHOOK_URL}" \
            -H "Authorization: Bearer ${CURSOR_WEBHOOK_AUTH}" \
            -H "Content-Type: application/json" \
            -d "${PAYLOAD}"
```

### 4. Verify

1. Confirm `.github/workflows/cursor-on-issue-opened.yml` exists on **`master`** (default branch).
2. Confirm repo secrets `CURSOR_WEBHOOK_URL` and `CURSOR_WEBHOOK_AUTH` are set (**Settings → Secrets and variables → Actions**). Secrets are shared across branches; the workflow file itself is not.
3. Confirm the Cursor automation is **Active**.
4. Open a **new** test issue (re-opening an old issue does not fire `types: [opened]`).
5. Check **Actions** for a run named **Trigger Cursor on new issue** — the curl step should succeed (HTTP 2xx).
6. In Cursor, open the automation → **Run History** and confirm a new cloud agent run started.
7. If you get **401**, regenerate the auth header in the automation UI and update `CURSOR_WEBHOOK_AUTH`.

---

## Troubleshooting: workflow did not run

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| No run appears under Actions at all | Workflow file not on **default branch** (`master`) | Merge/cherry-pick `.github/workflows/cursor-on-issue-opened.yml` to `master` |
| Workflow file present on `development` only | Same as above — `issues` events ignore non-default branches | Put the file on `master` |
| YAML invalid / workflow not listed | File pasted twice or bad indentation (e.g. `..."${PAYLOAD}"name: Trigger...`) | Keep a single valid workflow document; validate YAML |
| Run appears but curl fails | Missing/wrong secrets | Set `CURSOR_WEBHOOK_URL` and `CURSOR_WEBHOOK_AUTH` |
| `types: [opened]` but you edited/reopened | Wrong event | Create a brand-new issue, or also listen to `reopened` if needed |
| Actions disabled for the repo | Org/repo policy | Enable Actions for the repository |

Check whether GitHub knows the workflow on the default branch:

```bash
gh api repos/webapis/Visa2026/actions/workflows/cursor-on-issue-opened.yml
```

If that returns **404** (`not found on the default branch`), new issues cannot trigger it yet.

---

## Manual test with curl

```bash
curl -X POST "https://api2.cursor.sh/automations/webhook/<automation-id>" \
  -H "Authorization: Bearer crsr_YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{"test": true, "title": "Manual webhook test"}'
```

---

## Prompt tips for the automation

Tell the agent explicitly how to use the webhook JSON, for example:

```text
You were started because a GitHub issue was opened.
The HTTP body includes: title, body, url, number, author, labels.

1. Read the issue title and body.
2. Inspect the Visa2026 repository as needed.
3. If the issue is actionable, implement a minimal fix on a feature branch and open a draft PR that references the issue.
4. If the issue is unclear or not a code task, do not change code; summarize what is missing.
5. Prefer matching existing patterns in Visa2026.Module; do not widen scope.
```

Adjust for triage-only vs. auto-fix behavior.

---

## Alternatives (no webhook)

| Approach | When to use |
|----------|-------------|
| Comment **`@cursor`** on the issue | Manual, occasional runs |
| Automation trigger **Issue label changed** | Semi-auto: humans (or a bot) add a label like `cursor` after triage |
| Automation trigger **Issue comment** | Agent should react to discussion, not issue creation |
| Linear **Issue created** | If the team tracks work in Linear instead of GitHub Issues |

---

## Security checklist

- Keep `CURSOR_WEBHOOK_AUTH` in GitHub Actions secrets only.
- Prefer storing the webhook URL in a secret as well (`CURSOR_WEBHOOK_URL`).
- If the automation permission scope changes to **Team Owned**, regenerate the webhook API key.
- Restrict who can open issues if every new issue starts a billable cloud agent run (Automations bill as cloud agent usage).
- Consider filtering in the workflow (`if:` on labels, author, or title prefix) so spam or duplicate issues do not start agents.

---

## Related links

- [Cursor Automations docs](https://cursor.com/docs/cloud-agent/automations)
- [Cursor GitHub integration](https://cursor.com/docs/integrations/github)
- [Cursor Automations UI](https://cursor.com/automations)
- [Cloud agents overview](https://cursor.com/docs/cloud-agent)
