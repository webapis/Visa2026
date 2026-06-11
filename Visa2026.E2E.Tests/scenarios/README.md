# EasyTest E2E scenarios (Option A — YAML as spec)

Scenario **metadata** lives here; **execution** is matching C# in `Visa2026.E2E.Tests` (`SmokeTests`, `*Tests`).

| Layer | Location |
|-------|----------|
| Map + YAML | `scenarios/examples/` (draft) → **`scenarios/ready/`** when promoted |
| C# runner | `*Tests.cs` — method name references scenario id + `e2eId` in yaml |
| Constants | `Visa2026.Module/DatabaseUpdate/E2ETestDataSeed.cs` |

**Host:** `http://localhost:5050`, DB `Visa2026EasyTest`, build **EasyTest**.

## Workflow

```text
1. MAP   — write examples/<id>_map.md (caption inventory §3)
2. YAML  — write examples/<id>.yaml when map is Ready for YAML
3. C#    — implement [Fact] in *Tests.cs (keep in sync with yaml steps)
4. RUN   — dotnet test Visa2026.E2E.Tests -c EasyTest --filter ...
5. PROMOTE — move map + yaml to scenarios/ready/ when stable locally
```

Map contract: [`.cursor/skills/visa2026-easytest-e2e/reference-map-contract.md`](../../.cursor/skills/visa2026-easytest-e2e/reference-map-contract.md).

## Ready inventory (`scenarios/ready/`)

| Scenario id | E2E id | C# test |
|-------------|--------|---------|
| `login-smoke` | E2E-001 | `SmokeTests.LoginSmoke_AuthenticatedShellLoads` |
| `login-nav-employees` | E2E-001-nav | `SmokeTests.LoginNavEmployees_ListOpensWithNewAction` |
| `person-employee-create` | E2E-010 | `EmployeeTests.Employee_Create_RequiredFields_SavesAndAppearsInList` |

**Tier 0 run:**

```powershell
dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c EasyTest --filter "FullyQualifiedName~SmokeTests|FullyQualifiedName~EmployeeTests"
```

## Drafts (`scenarios/examples/`)

| File | Role |
|------|------|
| `_map_TEMPLATE.md` | Copy when starting a new scenario |
