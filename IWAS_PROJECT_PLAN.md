# IWAS project plan: Presentation documentation

Status: documentation baseline, 5 September 2026. This document does not authorize implementation.

## 1. Purpose and document map

IWAS helps warehouse staff inspect existing stock, supplier, and requisition data and print analytical reports. Current work defines only the Presentation UI/UX.

| Document | Responsibility |
|---|---|
| [UI/UX plan](IWAS_UI_UX_PLAN.md) | Canonical screens, components, interactions, reports, and acceptance scenarios |
| [Delivery blueprint](IWAS_IMPLEMENTATION_BLUEPRINT.md) | Future Presentation work packages and gates |
| [Master prompt](Codex%20Master%20Prompt%20%E2%80%94%20IWAS.md) | Concise documentation-only instructions |
| [Review record](docs/DOCUMENTATION_REVIEW.md) | Cuts, corrections, retained requirements, and decisions |
| [Assignment](CSC470_Project09_Warehouse_Analytics.pdf) | Functional requirements and worked examples |
| [Original documents](docs/archive/2026-09-05/) | Preserved historical full-system plans, inactive |

The owner's current instructions govern scope and stack; the PDF governs business requirements. The UI/UX plan supersedes archived frontend sections. SOURCE means an assignment requirement; OWNER an explicit owner constraint; ASSUMPTION a provisional source-gap decision; RECOMMENDATION a proposed design choice.

## 2. Current scope

Document user journeys, navigation, screen and component specifications, responsive layouts, accessibility, UI copy, Presentation folder responsibilities, illustrative display fixtures, report layouts, and future UI checks.

Defer application code, scaffolding, packages, executable tests, Model internals, Business calculations, persistence, APIs, authentication services, PDF engine integration, CI/CD, and deployment. Future endpoint and data requirements are handoff notes only.

## 3. Layered architecture

| Proposed location | Responsibility | Current work |
|---|---|---|
| `src/Model` | Future shared domain/data contracts; persistence details deferred | Boundary only |
| `src/Business` | Future use cases and authoritative analytical decisions | Boundary only |
| `src/Presentation` | ASP.NET Core web presentation, views, assets, display adaptation | Detailed UI/UX planning |
| `test/Presentation` | Future rendering, interaction and accessibility verification | Scenarios only |
| `test/Business`, `test/Model` | Future verification areas | Reserved, not designed |
| `docs` | Review records and supporting documentation | Active documentation |

Dependency direction: Presentation -> Business -> Model. Presentation may consume agreed Model result contracts, but must not query stores or bypass Business. View models belong to Presentation and adapt computed results for display. No extra frontend application layer is required. Assembly names remain an implementation decision; folder names above are fixed. No source/test folders are scaffolded now.

## 4. Technology decisions

OWNER: ASP.NET Core backend, HTML/CSS/JavaScript presentation, layered architecture. RECOMMENDATION: Razor MVC views with progressive JavaScript enhancement, Bootstrap as primary UI framework, and a small custom stylesheet. Razor supports server-rendered MVC views. [Microsoft MVC views documentation](https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview?view=aspnetcore-10.0).

Tailwind CSS is an allowed alternative. Bootstrap is the planning default because it preserves the existing component direction. If Tailwind is selected, translate the same tokens and components before implementation. Combining both is not the default: Tailwind Preflight changes base styles, so coexistence would require a reset, prefix, stylesheet-order, and visual-validation plan. This integration concern is a design inference from [Tailwind Preflight documentation](https://tailwindcss.com/docs/preflight).

Additional UI kits and JavaScript frameworks need a concrete product reason. Version pinning and any asset build tool are implementation-time decisions. This work installs no application dependencies.

## 5. Product boundaries

SOURCE: M1 FIFO valuation; M2 reorder/EOQ/dead stock; M3 supplier performance; M4 requisition matching; M5 reports R1-R5. Source records are read-only. A purchase requisition draft is a report, not an order submission. Clarification is a computed list for external follow-up.

RECOMMENDATION: dashboard and shared analytical shell. ASSUMPTION: login and multiple roles may be needed for deployment; they are optional UI variations, not assignment requirements. No administration, catalogue maintenance, imports, approvals, notification center, or operational warehouse screens.

## 6. Documentation completion

Each module and report must have a screen/layout specification, responsive behavior, states, and traceable acceptance scenarios. Architecture names must agree. Source gaps must be visible. The active prompt must remain documentation-only. Future implementation completion is a separate milestone in the delivery blueprint.
