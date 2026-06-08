# Debugging Docker deployments (Droplet vs local)

This document captures practices for when the **same application version** behaves differently on a DigitalOcean Droplet than in local Docker, and how to avoid relying on huge log copy-paste (including into AI tools).

Related docs:

- [PRODUCTION_DEPLOYMENT_RUNBOOK.md](./PRODUCTION_DEPLOYMENT_RUNBOOK.md) — deploy flow and safety.
- [ENVIRONMENTS.md](./ENVIRONMENTS.md) — environment configuration.
- [BLAZOR_SERVER_LOGGING.md](./BLAZOR_SERVER_LOGGING.md) — what the app logs at runtime, error categories, grep patterns.

---

## 1. Why the Droplet can differ from local (same image tag)

The **image tag** can match while **runtime** does not:

| Area | What to compare |
|------|------------------|
| **Data and schema** | Different DB content, migrations, or importer runs; stale or corrupt SQL volume on the server. |
| **Environment** | Connection strings, URLs, `ASPNETCORE_*`, DevExpress license, feature flags; outdated `.env` on the Droplet. |
| **Networking** | Reverse proxy (CORS, HTTPS, WebSockets, timeouts), internal DNS (`app` vs public hostname), firewall rules. |
| **Resources** | CPU/memory limits, OOM kills, slower disk on the Droplet. |
| **Startup timing** | SQL not ready on first request; local machine may mask race conditions. |
| **Clock / timezone** | Affects tokens, expiry, and scheduled logic. |

Treat “same version” as **same image digest**, **same environment contract**, and—when chasing data-dependent bugs—**similar or sanitized production data**.

---

## 2. Best practice: observable production, not “chat-debuggable” production

### 2.1 Central, searchable logs

- Keep the app logging to **stdout/stderr** (Docker-friendly).
- Add a **log pipeline** so you search and tail in a UI instead of SSH + copy-paste:
  - **Hosted:** Datadog, Grafana Cloud, Azure Monitor, etc.
  - **Self-hosted on Droplet:** Grafana **Loki** + Promtail, **Seq** (.NET-friendly structured logs).
  - **Lightweight Docker UI:** **Dozzle** or **Portainer** for container logs in the browser.

### 2.2 Structured logs and correlation IDs

- Prefer **JSON** or consistent key-value fields.
- Add a **request / correlation ID** on every log line for a request. Incidents become “search this id” instead of scrolling raw text.

### 2.3 Errors as first-class events

- Use **Sentry** (or Application Insights, etc.) for exceptions with stack traces, **release version**, and **environment**. Many issues are resolved from the dashboard without exporting logs.

### 2.4 Know exactly what is running

At startup (or in a health/details endpoint), log or expose:

- **Image digest** (not only tag).
- **Git SHA** (this repo’s Docker build can pass `GIT_SHA` as a build arg).
- `ASPNETCORE_ENVIRONMENT`, database name/host (never passwords).

This removes ambiguity about “old container” or “wrong tag”.

### 2.5 Staging Droplet

Mirror production compose, secrets handling, and proxy setup on a **smaller** Droplet with non-production data. Debug there first; avoid risky experiments on production.

---

## 3. Droplet workflow without living in copy-paste

1. **Reproduce with one identifier:** user report → find **correlation ID** or **time window** in Loki/Sentry → export only that slice if you still need offline or AI review.
2. **Parity check:** same **image ID** locally vs Droplet:

   ```bash
   docker inspect --format='{{.Image}}' <container_name_or_id>
   ```

   Diff **environment** via `docker inspect` (config section), not only “we both use `latest`”.
3. **`docker exec`** for quick checks (config present, `curl` from app network to SQL)—not as the primary log path.
4. **One-off capture** when needed: `docker logs <container> --since 1h > issue.txt` — still prefer central logging for day-to-day.

### Privacy and AI tools

- Do **not** paste production logs containing **PII, tokens, or connection strings** into third-party AI chats.
- Prefer **Sentry issue links**, **redacted** excerpts, or **filtered** log slices. Attach a file only when your tooling supports it and content is sanitized.

---

## 4. Local Docker from Docker Hub (“installer” stack)

Use local Docker to **match production shape**, not only to run latest code casually:

- Align **compose** layout and **environment variable names** with production (values differ).
- For elusive bugs, load a **sanitized** DB snapshot from staging/production into local Docker so behavior reflects **data**, not only the image tag.
- Keep **dev** and **prod-like** stacks separate (e.g. `docker-compose.dev.yml` vs `docker-compose.prod.yml`). When debugging Droplet issues, run **prod-like** locally.

---

## 5. Incident checklist: “production wrong, local fine”

1. Confirm same **image digest**, not only tag.
2. Diff **environment** and connection strings (host, database name, flags).
3. Compare **database** state (migrations, importer, persisted volume).
4. Check **reverse proxy** and **HTTPS** vs local HTTP.
5. Check **Droplet resources** and **OOM** (`docker events`, host metrics).
6. Longer term: **correlation ID + structured logging + error tracking** so the next incident is a **search**, not a log dump.

---

## 6. Optional: automating pull and restart on the Droplet

From the repository root (example for production compose + env file):

```powershell
docker compose --env-file .env.prod -f docker-compose.prod.yml pull
docker compose --env-file .env.prod -f docker-compose.prod.yml up -d
```

For Droplet-specific scripted updates, prefer the existing `droplet-scripts/` flow described in the production runbook.

---

## 7. Suggested next concrete improvement in this codebase

High leverage, in order of typical impact:

1. **Sentry** (or equivalent) + release/environment tags.
2. **Structured logging** + **correlation middleware** in the ASP.NET app.
3. **Loki** (self-hosted) or **Dozzle** on the Droplet for low-friction log access.

Pick hosted vs self-hosted based on budget and compliance; align any new service with `docker-compose.prod.yml` and secrets handling (no secrets committed to git).
