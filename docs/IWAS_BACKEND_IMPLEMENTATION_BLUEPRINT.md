# IWAS Business and Model delivery blueprint

Status: future implementation plan, 9 September 2026. The current request authorizes documentation only. The existing UI fixture preview remains intact.

The [Business plan](IWAS_BUSINESS_PLAN.md) owns algorithms and use cases. The [Model plan](IWAS_MODEL_PLAN.md) owns source/query/result contracts. The [UI/UX plan](IWAS_UI_UX_PLAN.md) owns presentation behavior; the [Presentation handoff](PRESENTATION_HANDOFF.md) records actual implementation. This document sequences future work and integration.

## 1. Architecture and completion levels

Use `src/Model`, `src/Business`, `src/Presentation` and `test/Model`, `test/Business`, `test/Presentation`. No new top-level application layer. Application flow is web input -> Business use case -> Model read abstraction -> immutable result -> Presentation formatting.

Separate milestones: documentation complete; Business calculations verified with typed inputs; Model verified against a real source adapter; existing UI integrated; complete reports verified. A rendered fixture or a pure formula test alone does not establish full-system correctness.

## 2. Ordered work packages

| Phase | Work | Deliverables | Depends on | Exit evidence |
|---|---|---|---|---|
| BM-0 | Documentation and decision review | Layer specs, source gaps, contract map | Current request | No unresolved ownership conflict; M1-M5 and R1-R5 coverage |
| BM-1 | Source discovery | Actual mapping, coverage, chronology, order granularity and read permissions | Source access/owner inputs | Model S01-S08 documented, optional gaps isolated |
| BM-2 | Contract and layer foundation | Typed records, query interfaces, result/issue shapes; project references | Explicit implementation request | Architecture checks, no dependency cycles or public write APIs |
| BM-3 | Read-only Model adapter | Canonical mappings, bounded queries, consistent reads | BM-1/2 | Model mapping/read-only/consistency tests |
| BM-4 | Shared stock and M1 | Chronological ledger and valuation | BM-2; typed data; BM-3 for integration | Source 90/49,050 example, invariants and bad-history tests |
| BM-5 | M3 | Supplier metrics and item delay selection | Order granularity agreed | Source 85%/4 days and deduplication/boundary tests |
| BM-6 | M2 | Demand, EOQ, ROP, dead stock and recommendation policy | BM-4/5 | Standalone ROP 60 plus combined-delay ROP 108 cases |
| BM-7 | M4 | Versioned normalizer, cosine, quantity/unit parser, enrichment | BM-2/4; catalogue price/units | Exact 80% source case and ten real computed demonstrations |
| BM-8 | M5 and dashboard | Full-scope report datasets and delegated metrics | BM-4 through BM-7 | Complete counts, cross-module consistency and failure gating |
| BM-9 | UI integration | Replace fixture selection with typed adapters | Relevant services and new implementation scope | Existing route/form behavior preserved; real dates/statuses/values |
| BM-10 | Reports and final verification | HTML/PDF rendering integration, regression and handoff evidence | BM-8/9; renderer choice | Actual A4 reports, complete source traceability and declared limits |

Pure Business implementation can proceed against typed test data before production mapping exists, once implementation is requested. Authoritative integrated completion still requires verified source gates. Do not estimate fixed dates until source quality, framework compatibility and report engine are known.

## 3. Existing UI-to-service integration map

| Existing UI | Future Business result | Presentation adaptation |
|---|---|---|
| Dashboard fixture metrics | BUS-09 per-metric context/results | Format values; preserve independent period labels and partial states |
| StockValuation view/layer table | BUS-02 StockValuation | Typed quantity/money to table strings; explicit warehouse R2 scope |
| Reorder fixture/filter | BUS-05 ReorderAnalysis batch | Filter by stable codes; show provenance and action text |
| Suppliers fixture/filter | BUS-03 performance batch | Ratio formatting; null-delay explanation; weighted summary |
| Requisitions Matching | BUS-06 typed semantic/enrichment result | Separate candidate from match; quantity/price/stock availability |
| Clarifications | BUS-07 derived batch | Reason filters and safe return context; no resolve operation |
| Report previews | BUS-08 R1-R5 data | Full declared scope, report layout and safe export |

Keep current GET routes unless a concrete integration need requires change. Map current `date`, `start`, `end`, `item`, `requisition`, `filter` and `report` fields at the web boundary. `apply` is a UI interaction flag, not a Business requirement. Remove preview-only `scenario` behavior from integrated production requests or isolate it in an explicit demo mode. Do not treat `returnUrl` as a domain field.

Replace fixed sample-date availability with real validation and coverage outcomes. Preserve `WorkspaceModel` as display context if useful, but add typed result adaptation instead of making it the database schema. Existing `DataTable` may remain a view helper; Business cannot return preformatted string arrays.

## 4. Important fixture differences

- Current stock/supplier/reorder samples are separate examples, not one database to reverse-engineer.
- Current R5 shows three source sample rows, while the clarification UI demonstrates additional synthetic records. Integrated counts must come from one declared scope.
- Synthetic similarities such as 84%, 86% or 90% are not hardcoded acceptance values for their sample text.
- The current “five hundred” quantity example requires a parser extension or an honest unsupported-quantity outcome.
- Candidate stock in a tied-match fixture must be labeled candidate-specific or omitted from integrated output.
- Browser Print sample is implemented; formal PDF generation is not. Preserve that distinction until real export exists.

## 5. Test and traceability matrix

| Source requirement | Business ownership | Model prerequisite | UI/report | Future evidence |
|---|---|---|---|---|
| FIFO, PDF p2 | BUS-01/02 | MOD-01/02; S03/04 | UX-03, R2 | Exact layers/value and prefix-underflow tests |
| Reorder/EOQ, p3 | BUS-05 | Costs, issue coverage, lead-time provenance | UX-04, R3 | EOQ/ROP and equality/window cases |
| Supplier metrics, p3-4 | BUS-03/04 | MOD-03, distinct-order mapping | UX-05, R4 | 17/20, late-only average, line deduplication |
| Matching, p4-5 | BUS-06/07 | MOD-01/04, price/unit metadata | UX-06/07, R5 | Pure cosine plus normalization/parser tests |
| Reports, p5-7 | BUS-08 | Consistent full-scope reads | UX-08/09, R1-R5 | Totals/counts, blocked partial reports, actual print inspection |
| Read-only, p1/7 | All use cases | MOD-05/06 | All screens | No mutation route/contract; disposable DB permission checks |
| Ten requisitions, p6 | BUS-06/07 | Coherent typed source fixtures | UX-06 and demonstration | Computed outcomes, not display-only assertions |

Run relevant Model, Business and existing Presentation checks after later integration. Do not rewrite UI regression expectations merely to hide an analytical discrepancy. Document accepted source-gap decisions and fixture differences first. This revision creates no executable tests and makes no new test-pass claim.

## 6. Decision gates and handoff

Before adapter integration: source provider/schema, identity rules, opening history, chronological ordering, order granularity and consistency. Before authoritative M2: demand period, lead-time source and supplier selection. Before M4 pricing: actual catalogue price and unit interpretation. Before reporting: scope inclusion, dead-stock valuation, snapshot policy, branding and renderer.

Framework compatibility is an implementation gate: the current host targets .NET 8; select compatible Business/Model targets and packages or propose a coordinated upgrade separately. Documentation does not upgrade the host or install dependencies.

Final handoff records implemented phases, source/mapping/normalizer versions, accepted assumptions, verification results, fixture-only areas and unresolved risks. Never claim integrated source correctness from the existing Presentation handoff's browser results.
