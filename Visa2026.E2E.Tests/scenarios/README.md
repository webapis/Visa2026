# EasyTest E2E scenarios (Option A — YAML as spec)

Scenario **metadata** lives here; **execution** is C# in `Visa2026.E2E.Tests`.

| Layer | Location |
|-------|----------|
| Map + YAML | `scenarios/ready/` (promoted) / `scenarios/examples/` (draft) |
| C# runner | `PersonOfficerJourneyTests.cs` |
| Constants | `Visa2026.Module/DatabaseUpdate/E2ETestDataSeed.cs` |

**Host:** `http://localhost:5050`, DB `Visa2026EasyTest`, build **EasyTest**.

**Suite:** one `[Fact]` master-data journey (`StopOnFail` in `e2e.runsettings` — failure ends the run).

## Ready inventory (`scenarios/ready/`)

| Scenario id | E2E id | C# test |
|-------------|--------|---------|
| `person-officer-journey` | E2E-001 (legacy map) | superseded — see examples master-data map |

## Draft / pending promote (`scenarios/examples/`)

| Scenario id | E2E id | C# test |
|-------------|--------|---------|
| `person-master-data-crud` | E2E-001…008 | `PersonOfficerJourneyTests.PersonOfficerJourney_LoginCreateEmployeeMasterDataCrud` |

**Run:**

```powershell
dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c EasyTest
```

## Workflow (new steps in the same journey)

```text
1. MAP   — update scenarios/examples/person-master-data-crud_map.md (§3 captions)
2. YAML  — update person-master-data-crud.yaml steps
3. C#    — extend PersonOfficerJourneyTests + E2ETestBase.PersonMasterData helpers
4. RUN   — dotnet test Visa2026.E2E.Tests -c EasyTest
5. PROMOTE — move map + yaml to ready/ after GHA green
```

Map contract: [`.cursor/skills/visa2026-easytest-e2e/reference-map-contract.md`](../../.cursor/skills/visa2026-easytest-e2e/reference-map-contract.md).

## Drafts (`scenarios/examples/`)

| File | Role |
|------|------|
| `_map_TEMPLATE.md` | Copy when documenting a new caption block before adding to the journey |
| `person-master-data-crud_map.md` | Person record CRUD caption inventory |
| `person-master-data-crud.yaml` | Spec mirroring the C# Fact |
