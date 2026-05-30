# Learnings (append-only): Visa2026 unit and integration tests

Purpose: this skill **gets smarter over time**. Capture **verified** outcomes from writing, running, and fixing tests in `Visa2026.Module.Tests` (and related `dotnet test` / CI work). Agents **read before** similar work; **append after** a lesson is confirmed — successes **and** failures.

Keep **`SKILL.md`** stable; **promote** repeated lessons into `SKILL.md` or [reference.md](./reference.md) per the ladder in **`SKILL.md`** (Continuous improvement).

**Do not** delete or rewrite old entries — **append only**.

---

## How to use

**Before** scaffolding tests, debugging flaky `dotnet test`, evaluator assertions, or integration DB setup: skim **## Entries** (newest last).

**After** the lesson is verified (green test run, confirmed root cause, or reproducible failure):

1. Append one entry using the template below.
2. Tag **Outcome** as `positive`, `negative`, or `anti-pattern`.
3. If the same root cause appears twice, note **Promote: pending** and update **`SKILL.md`** on the second hit.

---

## Entry template

```markdown
### YYYY-MM-DD — [+/−] <short title> (<UT-010 | IT-010 | scaffold | dotnet | CI>)

- **Outcome**: positive | negative | anti-pattern
- **Layer**: unit | integration | tooling | CI
- **Context**: (evaluator name, test class, OS, config Debug/EasyTest)
- **Goal** (positive) / **Symptom** (negative):
- **Try**:
- **Test** (how verified):
- **Result**:
- **Reuse** (positive — do this again) / **Avoid** (negative — do not repeat):
- **Promote**: none | pending → SKILL.md | done YYYY-MM-DD
```

**Positive (+):** a pattern that worked and should be copied (factory helper, filter command, fixture layout).

**Negative (−):** time lost on a dead end — document so the next run skips it.

**Anti-pattern:** wrong layer (e.g. unit test for Blazor UI) or harmful habit (shared DB with E2E, asserting `DateTime.Today` without fixed dates).

---

## Promotion ladder (skill maturity)

| Signal | Action |
|--------|--------|
| **1** verified entry | Append **`learnings.md` only** |
| **2** chats or PRs, same root cause | Add row to **Known pitfalls** in **`SKILL.md`** or **Troubleshooting** in [reference.md](./reference.md) |
| **3+** or blocks every new test author | Add **Agent workflow** bullet or **Scenarios** table in **`SKILL.md`** |
| Stable for months | Optional one line in [`docs/TESTING_PLAN.md`](../../../docs/TESTING_PLAN.md) §9–§10 |

**Cross-skill:** Blazor / EasyTest lessons belong in E2E README or a future E2E skill — link from here, do not duplicate full EasyTest steps.

**Developer review:** promoted **`SKILL.md`** text stays short; long logs stay in **`learnings.md`**.

---

## Entries

### 2026-05-30 — [−] Do not unit-test custom Blazor editors (anti-pattern)

- **Outcome**: anti-pattern
- **Layer**: unit (wrong) — use E2E or host testability hooks
- **Context**: `ApplicationTypeQuickCodeComponent` + EasyTest could not find control until `InputId` / `aria-label` on host
- **Symptom**: Attempting to cover ministry quick code **101** via unit test on Razor component or XAF model alone.
- **Try**: Unit test project referencing Blazor.Server or testing component render tree.
- **Test**: E2E still flaky without host `InputId`; unit tests cannot replace UI discovery.
- **Result**: **Unit:** test static evaluators and parsers in Module. **E2E:** `FillForm("Application Type Code", …)` + host `InputId="ApplicationTypeQuickCode"`. **Not** component unit tests unless project adopts bUnit.
- **Avoid**: Adding `Visa2026.Blazor.Server` reference to `Visa2026.Module.Tests` for UI fields.
- **Promote**: done 2026-05-30 (see **When to use which layer** in SKILL.md)

---
