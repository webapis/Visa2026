# EasyTest E2E scenarios (Option A — YAML as spec)

Scenario **metadata** lives here; **execution** is C# in `Visa2026.E2E.Tests`.

| Layer | Location |
|-------|----------|
| Map + YAML | `scenarios/ready/` |
| C# runner | `PersonOfficerJourneyTests.cs` |
| Constants | `Visa2026.Module/DatabaseUpdate/E2ETestDataSeed.cs` |

**Host:** `http://localhost:5050`, DB `Visa2026EasyTest`, build **EasyTest**.

**Suite:** one `[Fact]` journey (`StopOnFail` in `e2e.runsettings` — failure ends the run).

## Ready inventory (`scenarios/ready/`)

| Scenario id | E2E id | C# test |
|-------------|--------|---------|
| `person-officer-journey` | E2E-001 | `PersonOfficerJourneyTests.PersonOfficerJourney_LoginCreateEmployeeAddPassport` |

**Run:**

```powershell
dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c EasyTest
```

## Workflow (new steps in the same journey)

```text
1. MAP   — update scenarios/ready/person-officer-journey_map.md (§3 captions)
2. YAML  — update person-officer-journey.yaml steps
3. C#    — extend PersonOfficerJourneyTests + E2ETestBase helpers
4. RUN   — dotnet test Visa2026.E2E.Tests -c EasyTest
```

Map contract: [`.cursor/skills/visa2026-easytest-e2e/reference-map-contract.md`](../../.cursor/skills/visa2026-easytest-e2e/reference-map-contract.md).

## Drafts (`scenarios/examples/`)

| File | Role |
|------|------|
| `_map_TEMPLATE.md` | Copy when documenting a new caption block before adding to the journey |
