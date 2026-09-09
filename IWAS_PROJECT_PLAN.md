# IWAS project plan: layered system documentation

Status: updated 9 September 2026. Presentation already exists as a fixture-based preview. The current request extends documentation to Business and Model; it does not authorize their implementation.

## 1. Purpose and document map

IWAS helps warehouse staff inspect existing stock, supplier, and requisition data and print analytical reports. Current work defines Business and Model based on the existing Presentation UI/UX.

| Document | Responsibility |
|---|---|
| [Business plan](IWAS_BUSINESS_PLAN.md) | Canonical use cases, calculations, decisions and Business tests |
| [Model plan](IWAS_MODEL_PLAN.md) | Canonical source data, query/result contracts and Model tests |
| [Backend delivery plan](IWAS_BACKEND_IMPLEMENTATION_BLUEPRINT.md) | Future Business/Model work and existing UI integration |
| [UI/UX plan](IWAS_UI_UX_PLAN.md) | Canonical screens, components, interactions, reports, and acceptance scenarios |
| [Delivery blueprint](IWAS_IMPLEMENTATION_BLUEPRINT.md) | Future Presentation work packages and gates |
| [Master prompt](Codex%20Master%20Prompt%20%E2%80%94%20IWAS.md) | Concise documentation-only instructions |
| [Review record](docs/DOCUMENTATION_REVIEW.md) | Cuts, corrections, retained requirements, and decisions |
| [Assignment](CSC470_Project09_Warehouse_Analytics.pdf) | Functional requirements and worked examples |
| [Original documents](docs/archive/2026-09-05/) | Preserved historical full-system plans, inactive |

The owner's current instructions govern scope and stack; the PDF governs business requirements. The UI/UX plan supersedes archived frontend sections. SOURCE means an assignment requirement; OWNER an explicit owner constraint; ASSUMPTION a provisional source-gap decision; RECOMMENDATION a proposed design choice.

## 2. Current scope

Document Business use cases and algorithms, Model source/query/result contracts, source-gap decisions, read-only guarantees, future tests, and the integration path from current display fixtures to real results. Preserve the existing UI design and implementation.

Defer application code, scaffolding, packages, executable tests, database changes and deployment. Business calculations and Model internals are now documented, but remain unimplemented. Authentication and PDF engine implementation remain later work.

## 3. Layered architecture

| Proposed location | Responsibility | Current work |
|---|---|---|
| `src/Model` | Source/read records, query/result contracts, internal read-only adapter | Detailed Model documentation |
| `src/Business` | Use cases and authoritative analytical decisions | Detailed Business documentation |
| `src/Presentation` | ASP.NET Core web presentation, views, assets, display adaptation | Existing fixture UI; design retained |
| `test/Presentation` | Existing Presentation verification; future integration regression | No new test execution in this revision |
| `test/Business`, `test/Model` | Future Model/Business verification | Detailed scenarios, no executable tests |
| `docs` | Review records and supporting documentation | Active documentation |

Dependency direction: Presentation -> Business -> Model. Presentation may consume agreed Model result contracts, but must not query stores or bypass Business. View models belong to Presentation and adapt computed results for display. No extra frontend application layer is required. Assembly names remain an implementation decision; folder names above are fixed. The existing Presentation source/test folders are retained; no new code is scaffolded now.

## 4. Technology decisions

OWNER: ASP.NET Core backend, HTML/CSS/JavaScript presentation, layered architecture. RECOMMENDATION: Razor MVC views with progressive JavaScript enhancement, Bootstrap as primary UI framework, and a small custom stylesheet. Razor supports server-rendered MVC views. [Microsoft MVC views documentation](https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview?view=aspnetcore-10.0).

Tailwind CSS is an allowed alternative. Bootstrap is the planning default because it preserves the existing component direction. If Tailwind is selected, translate the same tokens and components before implementation. Combining both is not the default: Tailwind Preflight changes base styles, so coexistence would require a reset, prefix, stylesheet-order, and visual-validation plan. This integration concern is a design inference from [Tailwind Preflight documentation](https://tailwindcss.com/docs/preflight).

Additional UI kits and JavaScript frameworks need a concrete product reason. Version pinning and any asset build tool are implementation-time decisions. This work installs no application dependencies.

## 5. Product boundaries

SOURCE: M1 FIFO valuation; M2 reorder/EOQ/dead stock; M3 supplier performance; M4 requisition matching; M5 reports R1-R5. Source records are read-only. A purchase requisition draft is a report, not an order submission. Clarification is a computed list for external follow-up.

RECOMMENDATION: dashboard and shared analytical shell. ASSUMPTION: login and multiple roles may be needed for deployment; they are optional UI variations, not assignment requirements. No administration, catalogue maintenance, imports, approvals, notification center, or operational warehouse screens.

## 6. Documentation completion

Every module/report must map from Model data through a Business use case to an existing UI consumer and future acceptance checks. Source assumptions, calculations, failure states and layer ownership must be explicit. The active prompt remains documentation-only for the current request. The backend delivery plan separates pure calculations, verified source access and integrated UI completion.

## 7. Current implementation and document authority

The README and Presentation handoff describe the existing .NET 8 Razor/Bootstrap fixture UI. The 5 September UI plan is its design baseline, not a claim that no code exists today. Business and Model documentation now supersede earlier statements that those layers must only be mentioned as boundaries. Their detailed rules supersede conflicting archived recommendations; the UI plan still controls display behavior. Source business conflicts remain explicit decisions, never silently changed requirements.

