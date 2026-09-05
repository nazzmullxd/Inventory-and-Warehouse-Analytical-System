# Presentation implementation handoff

Date: 5 September 2026. Scope: the owner's request to implement UI/UX only. This records the implemented fixture milestone after the earlier documentation-only phase; it does not claim Business or Model integration.

## Delivered screens

| Screen | Implementation |
|---|---|
| UX-02 Dashboard | Date context, supplied KPIs, attention links, report navigation, explicit independent metric periods |
| UX-03 Stock Valuation | Exact-ID enquiry, idle/result/zero/partial/unavailable states, FIFO layers, receipt/issue context, warehouse-scoped R2 link |
| UX-04 Reorder and EOQ | Recommendation/dead-stock filters, full evidence columns, supplied priority order, demand/lead-time disclosure |
| UX-05 Supplier Performance | Inclusive period validation, standing filter, delivery evidence, missing sample/count distinctions |
| UX-06 Requisition Matching | Existing-ID lookup, uneditable full source text, ten display scenarios, supplied similarity, pricing/stock distinctions and candidates |
| UX-07 Clarification Needed | Date/reason filters, read-only exception list, inspect/return with preserved filters, Unicode source text |
| UX-08 Reports | R1–R5 selection, report-specific parameters, explicit full sample scope, empty/partial/unavailable states |
| UX-09 Report Preview | Five HTML reports, A4 print CSS, R3 draft, both R4 sections, R5 supplied counts, blocked incomplete output |
| UX-10 Error views | 404, unavailable and illustrative forbidden states, safe return navigation |
| UX-01 Conditional sign-in | Omitted for this local, unauthenticated preview; no simulated credential collection |

## Implementation decisions

- Followed the prescribed `src/Presentation` and root `test/Presentation` organization. CSS is separated into tokens, base/shell, components, report/page details, responsive rules and print. Shared fragments live in `Views/Shared`.
- Used the installed .NET 8 SDK and Razor MVC with Bootstrap 5.3.8 CSS, vendored locally with its license. No second CSS framework, frontend runtime, custom asset pipeline or extra UI kit.
- The single thin controller handles GET navigation and input validation. `Fixtures/DisplayFixtures.cs` supplies display-ready strings. No FIFO, EOQ, standing, ranking, parsing, price multiplication or analytical total calculation exists in Presentation.
- Used native exact-ID fields; the small ten-record requisition fixture offers a native datalist. A remote catalogue lookup and server paging remain integration work. Small complete lists show all matching sample rows, without unnecessary pagination controls.
- Used normal GET navigation rather than asynchronous analysis requests. This provides no-JavaScript fallbacks and native cancellation of superseded navigations. JavaScript adds validation summaries, immediately hides results when parameters change, and restores canonical URL values after Back/Forward.
- Preserved the source worked examples as separate module cases. Added IDs and supplementary scenario details are illustrative. R5 uses only the source's three-row example; the clarification UI additionally demonstrates synthetic exception states.
- Used labeled sample company branding and explicit Asia/Dhaka date context. Display values use BDT and do not depend on browser timezone.

## Verification evidence

`dotnet build` passed with zero warnings and zero errors. All 15 browser checks passed using Chromium 140.0.7339.16 with Playwright and axe-core; exact run evidence is in `test/Presentation/artifacts/results.json`.

Checks cover all main routes and five report previews, automated WCAG A/AA rules, supplied zero versus unavailable values, withheld partial output, changed-parameter invalidation, Back restoration, linked validation and focus, ten requisition scenarios, clarification return context, report scope, safe errors and return URLs, keyboard navigation, and the no-JavaScript valuation-to-report journey.

The viewport matrix is 1440×900, 1024×768, 768×1024, 390×844 and 320×800. The suite checks page reflow while allowing labeled evidence regions to scroll. It also checks 200% text enlargement and scrollable sidebar navigation. Full-page screenshots are saved for desktop and 320px views, with dashboard captures at every matrix size.

Browser-printed R1–R5 samples were rendered with Poppler and visually inspected. Each standard sample occupies one A4 page with legible tables and required sections. A separate long-name/Bengali/large-value layout fixture exercises multi-page printing and preserves the final row. These are verification artifacts, not an application PDF export feature. The test runner supplies deterministic page numbering to its own PDFs; actual browser printing depends on the user's headers/footers settings.

Automated scans and visual checks are not a complete WCAG compliance assessment. Real screen-reader testing and Edge/Firefox/Safari coverage remain unverified. No business-formula correctness or real ten-requisition AI demonstration is claimed.

## Backend handoff

Replace display fixtures only after agreeing on the result contracts in section 11 of the UI/UX plan: identities and units; date/period/timezone; authoritative statuses, values, reasons and completeness; permissions; paging; snapshot/retrieval context; and full-scope report contracts.

Real authentication, role enforcement, data access, analytical calculations, transient-service retry/stale states, remote lookup, report generation and formal PDF pagination remain separate integration work. Do not infer source algorithms or totals from the display strings. Keep all source records read-only; no orders, approvals or clarification resolutions are submitted.
