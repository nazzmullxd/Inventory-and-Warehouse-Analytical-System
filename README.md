# IWAS Presentation

Implemented UI/UX preview for the Inventory and Warehouse Analytics System. Uses ASP.NET Core 8, Razor MVC, local Bootstrap 5.3.8, and plain CSS/JavaScript.

The owner's implementation request supersedes the earlier documentation-only milestone in the planning files. This deliverable is **Presentation with labeled display fixtures**, not an integrated analytics system. The original plans and assignment remain preserved.

## Run

Requires the .NET 8 SDK. From the repository root:

```powershell
dotnet run --project src/Presentation/Iwas.Presentation.csproj --urls http://127.0.0.1:5080
```

Open http://127.0.0.1:5080. No frontend build, database, account, or external asset connection is required.

## Organization

```text
src/Presentation/
  Controllers/           Read-only view routing and input validation
  Fixtures/              Display-ready examples; no calculations
  ViewModels/            Presentation contracts and date formatting
  Views/
    Shared/              Shell, navigation, tables, feedback, state controls
    Dashboard/           Warehouse overview
    StockValuation/      FIFO presentation
    Reorder/             Reorder, EOQ and dead-stock presentation
    Suppliers/           Delivery reliability presentation
    Requisitions/        Matching and clarification views
    Reports/             Report selection and R1–R5 HTML previews
    Errors/              Missing, forbidden and unavailable views
  wwwroot/
    css/                 Tokens, base/shell, components, pages, responsive, print
    js/shared/           Navigation, validation and URL-context restoration
    js/pages/            Report printing interaction
    vendor/bootstrap/    Pinned local CSS and upstream license
test/Presentation/       Browser, accessibility, viewport and print checks
docs/                    Planning records and implementation handoff
```

`src/Model` and `src/Business` remain future layers. No backend domain code, persistence, analytical algorithms, authentication service, or PDF engine was added. The minimal web host serves the Razor UI.

## Preview data

Use **08 Jul 2026** for analysis and requisitions; suppliers use **01 Jan–30 Jun 2026**. Other scopes display unavailable results instead of relabeling the sample data. A4 Paper uses `IT-1108`; matching supports `RQ-0871` through `RQ-0880`.

“Preview display states” exposes empty, partial and unavailable examples. The matching scenarios include low similarity, tied candidates, Unicode, empty text, missing quantity/price, zero stock and insufficient stock. Report previews explicitly identify their scope and sample status. “Print sample” uses browser printing; PDF download remains a future integration.

## Verify

Keep the application running, then:

```powershell
dotnet build src/Presentation/Iwas.Presentation.csproj
cd test/Presentation
npm ci
npx playwright install chromium
npm test
```

The runner writes screenshots, browser-generated print samples and results to ignored `test/Presentation/artifacts/`. Set `IWAS_URL` to test another local port. Node packages are test-only; the UI itself requires no npm dependencies.

See [Presentation handoff](docs/PRESENTATION_HANDOFF.md) for evidence, design decisions and integration boundaries. The detailed design baseline is [IWAS_UI_UX_PLAN.md](IWAS_UI_UX_PLAN.md).
