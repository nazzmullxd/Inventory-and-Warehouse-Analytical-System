# IWAS — Inventory and Warehouse Analytics System

ASP.NET Core 8 / Razor MVC application with Model, Business and Presentation layers. Analytics now read a persistent **XAMPP MySQL** database through a read-only account. The initial database contains the existing demonstration dataset.

## Database setup

Start **MySQL** in XAMPP Control Panel, then run from the repository root:

```powershell
./database/setup.ps1
dotnet run --project src/Presentation/Iwas.Presentation.csproj --urls http://127.0.0.1:5080
```

Requires the .NET 8 SDK. This workspace also has a local SDK: replace `dotnet` with `./.tools/dotnet/dotnet.exe` if needed. Open http://127.0.0.1:5080. ASP.NET Core runs independently of XAMPP Apache.

Setup creates database `iwas`, five InnoDB tables, the demonstration records, and a SELECT-only `iwas_reader` account. Its generated password is stored in ignored `src/Presentation/appsettings.Local.json`. Rerunning setup preserves existing data and configuration. See [database setup](docs/DATABASE_SETUP.md) for credentials, schema, verification and limitations.

## Features and sample scopes

The Business layer computes FIFO stock valuation, supplier performance, reorder/EOQ/dead stock, requisition matching, warehouse dashboard and R1–R5 report data. Razor renders typed results; browser printing provides report previews.

Use **8 July 2026**, item **IT-1108**, requisition **RQ-0871**, and supplier period **1 January–30 June 2026**. The expanded seed contains 25 items, 287 movements, 165 delivery records across 9 suppliers, and 52 requisitions. To upgrade an original demo database without replacing its records, run `./database/setup.ps1 -AddDemoData`; reruns do not duplicate rows. The UI retains its demonstration date limit. Real operational data, verified coverage, authentication and a dedicated server-side PDF engine remain future work.

## Organization

- `src/Model`: source/result contracts, validation and database read adapter.
- `src/Business`: analytical calculations and report orchestration.
- `src/Presentation`: MVC routes, Razor views, CSS and JavaScript.
- `database`: schema and repeatable administrative setup tool.
- `test/Model`, `test/Business`, `test/Database`: acceptance and database checks.
- `test/Presentation`: browser/accessibility checks; some legacy fixture assertions predate the computed analytics integration.
- `docs`: plans and implementation handoffs.

## Verify

```powershell
dotnet build src/Presentation/Iwas.Presentation.csproj
dotnet run --project test/Model/Iwas.Model.Tests.csproj
dotnet run --project test/Business/Iwas.Business.Tests.csproj
dotnet run --project test/Database/Iwas.Database.Tests.csproj
```

Database checks require the unchanged local seed. For current UI checks, run the app, then `npm ci` and `npm run test:ui` inside `test/Presentation` (uses installed Microsoft Edge). See [UI refresh](docs/UI_REFRESH.md) for browser settings and verification. Artifacts are ignored. The historical `npm test` runner retains obsolete fixture expectations. See [backend handoff](docs/BACKEND_HANDOFF.md), [Model plan](docs/IWAS_MODEL_PLAN.md), and [Presentation handoff](docs/PRESENTATION_HANDOFF.md) for the earlier implementation baseline.
