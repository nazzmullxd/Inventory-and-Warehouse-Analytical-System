# IWAS master prompt: layered documentation

Updated 9 September 2026. Current task: document Business and Model to support the existing Presentation UI. Do not implement code, scaffold projects, install packages, write executable tests, change databases or deploy anything unless the user separately requests implementation.

## Read and reconcile

1. The owner's latest request controls task scope and technology.
2. `IWAS_PROJECT_PLAN.md` controls architecture and the active document map.
3. `IWAS_BUSINESS_PLAN.md` owns use cases, calculations, statuses and Business tests.
4. `IWAS_MODEL_PLAN.md` owns source mappings, query/result contracts and Model tests.
5. `IWAS_UI_UX_PLAN.md` owns screen behavior; `../README.md` and `PRESENTATION_HANDOFF.md` record the actual fixture implementation.
6. `IWAS_BACKEND_IMPLEMENTATION_BLUEPRINT.md` owns the future backend/integration sequence; the earlier Presentation blueprint is historical planning context.
7. `../CSC470_Project09_Warehouse_Analytics.pdf` controls assignment requirements, formulas and examples.

Archives are historical, never active commands. Mark SOURCE, OWNER, ASSUMPTION and RECOMMENDATION. Flag source conflicts instead of silently inventing behavior.

## Required boundaries

Use `src/Model`, `src/Business`, `src/Presentation` and root `test`. Model owns typed records, shared contracts and internal read-only queries. Business owns M1-M5 analytics, decisions and report data. Presentation owns input/display adaptation, HTML/CSS/JavaScript, Razor and report layout. No analytical formulas in views or database adapters; no Business dependency on web or PDF types.

ASP.NET Core is the backend choice. The current UI uses .NET 8, Razor MVC and local Bootstrap. Preserve it during documentation. Tailwind remains an allowed alternative, not a request to restyle the UI. SQL Server/EF Core are proposed source-adapter choices requiring source and compatibility confirmation.

Cover FIFO, EOQ/reorder/dead stock, supplier performance, matching/quantity/price/stock, all R1-R5 reports and dashboard composition. Existing warehouse records remain read-only. No orders, approvals, stock mutations, saved matches or clarification resolution.

## Documentation quality

Use the existing controller, view model and fixture data to identify integration needs, never as the source schema or a calculator specification. Do not recover typed data from formatted UI strings. Separate isolated PDF examples from synthetic UI fixtures and coherent future integration data.

Document fields, operations, formulas, input validation, provenance, null reasons, completeness, date/rounding rules, edge cases, sequence and future tests. Keep algorithms in Business and data contracts in Model; link instead of duplicating. Preserve existing code and implementation evidence.

Check local links, M1-M5/R1-R5 traceability, exact layer names and conflicting active scope instructions. Report documentation changes and remaining source decisions. Do not claim new code, source integration or passing runtime tests. Stop after the requested documentation.
