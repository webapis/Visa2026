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

Keep the instruction field **once** (do not paste the same block twice). Prefer **two automations** over one agent that “waits forever” for CI (see [CI fix loop](#recommended-ci-fix-loop-two-automations)).

### Automation A — issue opened (implement)

```text
You were started because a GitHub issue was opened.
The HTTP body includes: title, body, url, number, author, labels.

Goals:
1. Read the issue title and body.
2. If unclear or not a code task: do not change code; comment what is missing; stop.
3. Implement a minimal fix on a feature branch named cursor/<short-slug>-####.
4. Prefer matching existing patterns in Visa2026.Module; do not widen scope.
5. BEFORE opening a PR, verify locally:
   - dotnet build Visa2026.slnx -c Debug
   - dotnet test Visa2026.Module.Tests/Visa2026.Module.Tests.csproj -c Debug --no-build
   Fix compile/test failures from that run (up to 3 local attempts). Do not open a PR while local build/tests fail.
6. Open a draft PR that references the issue (Fixes #N). Target the repo default branch unless the issue says otherwise.
7. Do NOT stop only because the PR was opened. After push:
   - Use gh to watch checks on the PR (gh pr checks --watch, or poll gh pr checks).
   - Wait for Build & unit tests (and EasyTest if it started). Timeout ~25 minutes.
   - If checks fail: fetch failed logs (gh run view --log-failed), fix on the same branch, push, increment attempt.
   - Retry CI fix at most 5 times total. After 5 failures, comment on the PR with root cause + what you tried, leave draft open.
8. Do not skip or delete tests to force green unless the issue explicitly asks for that.
```

### Automation B — CI failed on PR (recommended second automation)

Relying only on Automation A to stay alive through CI is fragile (long E2E, agent timeout). Add a second automation:

| Setting | Value |
|---------|--------|
| Trigger | GitHub → **Checks completed** (on failure) **or** **Workflow run completed** (failure) |
| Scope | PRs / this repo; prefer **Anyone** (not only “Me”) so `github-actions` / bot check actors still match |
| Tools | Open Pull Request (push to existing branch), Memories |

**Caveat:** Cursor’s “Checks completed” trigger has been reported to **skip PRs created/pushed by `cursor[bot]`**. If Run History stays empty for agent PRs, use **Workflow run completed**, or keep the `gh pr checks --watch` loop in Automation A, or comment `@cursor` on the failed PR.

Paste-ready prompt for Automation B (max 5 attempts via Memories):

```text
You fix CI failures on an open Visa2026 pull request.

Attempt limit:
1. Memory file name: ci-retry-pr-<PR_NUMBER>
2. Read that memory. If attempts >= 5, comment on the PR that the retry budget is exhausted and stop.
3. Else set attempts = (previous or 0) + 1 and write the memory.

Then:
1. Identify failing checks; pull failed logs with gh (gh pr checks, gh run view --log-failed).
2. Reproduce locally when possible: dotnet build Visa2026.slnx -c Debug and the failing test project.
3. Apply a minimal fix on the EXISTING PR branch (do not open a duplicate PR).
4. Push and summarize what failed and what you changed.
5. Do not widen scope. Do not skip tests unless clearly flaky and justified in the PR comment.
```

Marketplace starting point: [Fix CI failures](https://cursor.com/marketplace/automations/ci-autofix) (adapt to push onto the existing PR branch, not always open a new PR).

---

## Recommended CI fix loop (two automations)

```text
Issue opened
    → Automation A: implement + local verify + draft PR
         → (optional) A watches gh checks and may fix up to 5 times
CI fails on PR
    → Automation B: read logs + fix + push (attempts tracked in Memories, max 5)
Green checks
    → human reviews / marks ready
```

**Why not only “retry 5 times” inside the first prompt?**  
One cloud-agent run can end after opening the PR. Waiting for Build + EasyTest (~10+ minutes) × 5 attempts is slow, expensive, and easy to cut off. A **CI-failed trigger** starts a fresh agent with the failure context.

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
