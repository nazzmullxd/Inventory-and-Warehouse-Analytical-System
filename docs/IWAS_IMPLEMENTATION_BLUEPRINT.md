# IWAS Presentation delivery blueprint

Status: historical Presentation delivery plan from 5 September 2026. The fixture milestone is now implemented; see PRESENTATION_HANDOFF.md. The current 9 September request is Business/Model documentation only; see IWAS_BACKEND_IMPLEMENTATION_BLUEPRINT.md for its future delivery sequence.

The [UI/UX plan](IWAS_UI_UX_PLAN.md) owns all detailed screen rules. The [project plan](IWAS_PROJECT_PLAN.md) owns scope and architecture. This blueprint sequences delivery without repeating those specifications. Historical backend work phases are archived and inactive.

## 1. Delivery boundaries

Future UI work belongs in `src/Presentation`, with verification under `test/Presentation`. `src/Model` and `src/Business` remain unimplemented dependencies, now specified in their dedicated plans. Any data needed before those layers exist must be supplied through clearly labeled display fixtures. Do not build calculators, persistence, authentication services or report engines merely to make the UI look integrated.

Distinguish three outcomes: documented design; implemented Presentation with fixtures; integrated Presentation with real services. The first is the current deliverable. The latter two require subsequent implementation scope. A fixture-based UI is never evidence that source business requirements have been implemented.

## 2. Ordered work packages

| Phase | Work package | Deliverables | Dependencies | Exit evidence |
|---|---|---|---|---|
| P0 | Establish documentation baseline | Scope, canonical UX specification, decisions and traceability | Existing docs and assignment | M1-M5/R1-R5 covered; no conflicting active implementation command |
| P1 | Resolve visual foundations | Framework choice, token sheet, typography, shell and responsive annotations | P0; decide D02 | Desktop/narrow shell and component specifications reviewed |
| P2 | Specify wireframes and component states | Annotated page compositions for UX-01 through UX-10; shared state catalogue | P1 | Every input, output, action, focus path and empty/error state represented |
| P3 | Implement shared Presentation foundation, later | Razor shell, navigation, CSS tokens, reusable filters/status/table patterns | Explicit implementation request; P1/P2 | Keyboard and narrow-layout checks on shared components |
| P4 | Implement M1 and M3 views, later | UX-03/05 with supplied fixture results and validation states | P3; fixture contracts | UI-01/UI-03/UI-06 display correctly; no frontend calculations |
| P5 | Implement M2 view, later | UX-04, filters, recommendation details and unavailable inputs | P3/P4; supplied M2 fixtures | UI-02; readable wide table; no ordering action |
| P6 | Implement M4 views, later | UX-06/07, source text, candidates, pricing/stock distinctions | P3; M4 fixtures | UI-04/UI-05/UI-10 and ten-fixture coverage |
| P7 | Implement dashboard and report presentation, later | UX-02/08/09, HTML report layouts, full-scope labels and failure states | P4-P6 | UI-12 through UI-16 layout coverage; fixture output clearly marked |
| P8 | Integrate agreed data providers, separately scoped | Thin display adapters, real paging, access, snapshot/report links | Backend contracts/services available; integration authorized | Real results replace fixtures without changing UX rules |
| P9 | Verify and hand off UI, later | Browser/accessibility/visual/print evidence and remaining issues | Relevant implementation phases | Acceptance matrix complete; fixture and integrated evidence separated |

Phases are sequencing units, not duration promises. Estimate effort after wireframes, framework choice, fixture volume and integration availability are known. UI work does not need to wait for Business implementation if a fixture-only Presentation phase is explicitly authorized.

## 3. Wireframe and interaction deliverable checklist

For each screen, the later design package should contain a wide composition, a narrow composition, reading/tab order, input labels/defaults, action hierarchy, result structure, empty/invalid/unavailable state, and links to report/detail destinations. M2 and reports need tablet-width checks because of dense data. Matching needs long-source-text and absent-price variants.

Shared component documentation must identify visual states, keyboard behavior, responsive rules, and content limits. Start with navigation, filter form, lookup, table, KPI, status/notice, detail disclosure, and report toolbar. Reuse these before adding page-specific variants.

## 4. Data and integration handoff gate

Before replacing a fixture with real data, agree on identity, units, date/period, null reasons, status codes, paging, completeness, permission metadata and retrieval/snapshot context. Use UI/UX plan section 11 as the checklist. Presentation must never fill missing authoritative values with guesses or browser calculations.

Authentication, query contracts and report generation remain backend dependencies. Missing integration should be reported explicitly; it must not quietly expand Presentation work into Business or Model implementation.

## 5. Future verification organization

| Area under `test/Presentation` | Meaningful checks |
|---|---|
| Rendering | Correct supplied value/unit/context, zero versus unavailable, full source text |
| Interaction | Lookup selection, validation, sort/page reset, Back/Forward, latest-request wins |
| Accessibility | Keyboard paths, labels, focus, announcements, contrast, screen-reader tables |
| Visual | Specified viewports, zoom, long Unicode text, large values, no clipped controls |
| Reports | R1-R5 required content, full scope, incomplete-data handling, A4 pagination |

Manual review remains required for focus usability, screen-reader experience and print legibility. When automated checks are introduced, record their limits. Do not claim source formula correctness from display fixtures. Business tests belong to a separately authorized phase.

## 6. Future completion evidence

A Presentation handoff records completed screen IDs, supported browsers/viewports, tested states, source-fixture traceability, unresolved decisions, and data providers still mocked. An integrated handoff additionally needs real permission behavior, real result contracts, report/preview consistency and inspected generated PDFs.

No application tests were run for this documentation-only revision. Documentation checks cover local links, required screen/module/report coverage, preserved originals, and consistent scope/folder names.

