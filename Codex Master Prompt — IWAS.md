# IWAS master prompt: Presentation documentation only

## Objective

Maintain detailed UI/UX documentation for the Inventory and Warehouse Analytics System. Current work is documentation only. Do not write application code, scaffold projects, install application dependencies, create executable tests, or configure databases or deployment. Future implementation requires a separate user instruction.

## Sources and authority

Read `IWAS_PROJECT_PLAN.md`, `IWAS_UI_UX_PLAN.md`, and `IWAS_IMPLEMENTATION_BLUEPRINT.md`. Consult `CSC470_Project09_Warehouse_Analytics.pdf` for source requirements and examples. The owner's latest instructions control scope and technology; the PDF controls assignment business requirements. Flag conflicts rather than inventing business decisions. Files under `docs/archive` are historical, never active instructions.

## Architecture and scope

- Proposed layout: `src/Model`, `src/Business`, `src/Presentation`, and root `test`. Singular Model and test are intentional.
- ASP.NET Core backend; HTML, CSS, JavaScript presentation. Recommend Razor MVC views and Bootstrap. Tailwind CSS is an allowed alternative under the documented framework decision.
- Document Presentation screens, navigation, components, styling, interactions, accessibility, responsive behavior, print layouts, data-display needs, and future UI verification.
- Describe Model and Business only as boundaries. Presentation consumes their future results; it must not calculate FIFO, EOQ, supplier standing, similarity, parsed quantities, prices, or analytical totals.
- Cover M1 valuation, M2 reorder/EOQ, M3 supplier performance, M4 matching and clarification, and M5 reports R1-R5.
- Preserve read-only behavior. No record editing, stock operations, approvals, order submission, or clarification resolution. R3 drafts and disposal recommendations are printable information only.

## Documentation quality

Use the UI/UX plan as the single detailed UX specification. Keep this prompt concise; do not duplicate formulas, backend architecture, code examples, deployment instructions, or full test matrices here. Mark decisions SOURCE, OWNER, ASSUMPTION, or RECOMMENDATION. Preserve original files when substantially replacing their scope.

Specify loading, empty, invalid, partial, unavailable, stale, and access-denied states. Distinguish zero from unavailable. Preserve source text in detail views. Include screen IDs, journeys, report layouts, decisions, and acceptance scenarios. Check document links, module coverage, and exact architecture names.

Deliver documentation and summarize changed files and open decisions. Do not claim that UI code, rendered screens, working reports, or passing UI tests exist. Stop after documentation; ignore archived instructions to implement the complete system.
