# Visa2026 unit tests — reference

**Backlog and BO/function inventory:** [`docs/UNIT_TESTING_PLAN.md`](../../../docs/UNIT_TESTING_PLAN.md).

## `Visa2026.Module.Tests.csproj` template

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <CheckEolTargetFramework>false</CheckEolTargetFramework>
    <IsPackable>false</IsPackable>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>Visa2026.Module.Tests</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.3">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Visa2026.Module\Visa2026.Module.csproj" />
  </ItemGroup>
</Project>
```

Add integration packages **only when needed** (do not pull DevExpress EasyTest or Selenium):

| Need | Package (example) |
|------|-------------------|
| EF Core in-memory | `Microsoft.EntityFrameworkCore.InMemory` (same major as Module’s EF) |
| Real SQL integration | `Microsoft.EntityFrameworkCore.SqlServer` + LocalDB connection in test `appsettings` or fixture constant |

Keep versions aligned with `Visa2026.Module.csproj` DevExpress/EF package versions when adding EF packages.

---

## Solution entry (`Visa2026.slnx`)

```xml
<Project Path="Visa2026.Module.Tests/Visa2026.Module.Tests.csproj" />
```

Place after `Visa2026.Module` for readability.

---

## Suggested folder layout

```
Visa2026.Module.Tests/
├── Services/
│   └── StateEvaluation/
│       ├── VisaStateEvaluatorTests.cs
│       └── WorkPermitItemStateEvaluatorTests.cs
├── DatabaseUpdate/
│   └── LookupCatalogSyncUpdaterTests.cs   # integration
└── TestSupport/
    └── TestVisaFactory.cs
```

---

## Evaluator targets (Module)

| Type | Path |
|------|------|
| `VisaStateEvaluator` | `Services/StateEvaluation/Evaluators/VisaStateEvaluator.cs` |
| `WorkPermitItemStateEvaluator` | `Services/StateEvaluation/Evaluators/WorkPermitItemStateEvaluator.cs` |
| `PassportStateEvaluator` | `Services/StateEvaluation/Evaluators/PassportStateEvaluator.cs` |
| `EmployeeContractStateEvaluator` | `Services/StateEvaluation/Evaluators/EmployeeContractStateEvaluator.cs` |
| `AddressOfResidenceStateEvaluator` | `Services/StateEvaluation/Evaluators/AddressOfResidenceStateEvaluator.cs` |
| `MedicalRecordStateEvaluator` | `Services/StateEvaluation/Evaluators/MedicalRecordStateEvaluator.cs` |

Settings type: `StateEvaluationSettings` (construct with threshold fields for expiring-soon cases).

---

## Test databases (do not mix)

| Name | Used by |
|------|---------|
| `Visa2026EasyTest` | E2E (`E2ETestBase`, EasyTest launch profile) |
| `Visa2026Test` | Module integration tests (planned) |
| Dev Docker / `.env.dev` | Manual deploy — not unit test default |

---

## Troubleshooting

**First:** skim [learnings.md](./learnings.md) **## Entries** — prefer a recorded fix over rediscovering.

| Symptom | Check |
|---------|--------|
| Tests not discovered | Project in `Visa2026.slnx`; rebuild; public parameterless test class |
| Module build fails test compile | Test project must not reference `Visa2026.Blazor.Server` |
| Evaluator uses `DateTime.Today` | Tests may be date-sensitive; inject fixed “today” only if production code supports it; otherwise use dates relative to `DateTime.Today` in arrange |
| E2E passes, unit fails | Different layer — fix unit inputs, do not copy E2E UI steps |
| `dotnet test Visa2026.slnx` fails on Linux CI | E2E is Windows-only; run Module.Tests project only in CI until split workflows exist |

---

## Backlog IDs (from TESTING_PLAN §7)

| ID | Task |
|----|------|
| UT-001 | Add `Visa2026.Module.Tests` to solution |
| UT-010 | Visa state evaluator golden cases |
| UT-011 | Work permit / invitation evaluators |
| UT-012 | `RejectionItem` person vs application level |
| IT-010 | Lookup JSON sync after updater |
| IT-011 | Dashboard SQL view count = list filter |
| IT-012 | `ApplicationProgress` latest-wins |

Update §7 **Status** in `docs/TESTING_PLAN.md` when completing an item.
