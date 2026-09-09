# IWAS detailed UI/UX plan

Status: Presentation design baseline from 5 September 2026. The fixture UI was subsequently implemented; see README and the Presentation handoff. As of 9 September, current work is Business/Model documentation only. Read with the [project scope](IWAS_PROJECT_PLAN.md) and [delivery blueprint](IWAS_IMPLEMENTATION_BLUEPRINT.md).

## 1. Experience goals and authority

Help a warehouse manager answer: What is stock worth? What needs reordering? Which suppliers need attention? What does a requisition refer to? Which report should be printed? Each journey exposes the selected date, relevant evidence, limitations, and a clear next inspection step.

SOURCE: assignment PDF pages 1-7 defines M1-M5, read-only source data, and five A4 reports. OWNER: current work is UI/UX documentation only, within `src/Model`, `src/Business`, `src/Presentation`, and root `test`. Unless marked SOURCE or OWNER, detailed layout and interaction choices below are RECOMMENDATIONS. Unresolved business inputs remain ASSUMPTIONS, never frontend calculations.

Priorities: trustworthy results, understandable context, fast scanning, keyboard/mobile usability, consistent print output. Use a practical analytical workspace with tables as the main evidence surface. Avoid marketing sections, decorative charts, unnecessary animation, and nested cards. A dashboard is a navigation convenience, not a source-required module.

## 2. Users and access variations

| User | Main task | Primary path |
|---|---|---|
| Warehouse manager | Review stock, exceptions and reports | Dashboard -> analysis -> report |
| Storekeeper | Inspect requisition interpretation and stock | Matching -> clarification -> detail |
| Purchase officer | Inspect reorder recommendations and supplier evidence | Reorder -> supplier -> R3/R4 |
| Report viewer | Read authorized reports | Reports -> preview -> PDF |

The manager is the source's primary user. Separate authenticated roles are an ASSUMPTION inherited from earlier planning. Baseline documentation includes a manager with all analytical pages; role-specific views remain conditional until the deployment access model is chosen.

Proposed optional visibility: manager all pages/reports; storekeeper M1/M4 and R1/R2/R5; purchase officer M2/M3 and R3/R4; viewer reports only. Dashboard content follows permitted modules; viewers get report links rather than raw analytical data. Hiding navigation does not enforce authorization; actual authorization is a future backend dependency.

If authentication is omitted for an approved local demonstration, omit Login and user-menu flows without changing analytical pages. No user or role management interface is planned.

## 3. Information architecture and journeys

| Screen ID | Label | Proposed location | Purpose |
|---|---|---|---|
| UX-01 | Sign in | `/account/login` | Conditional access entry |
| UX-02 | Dashboard | `/` | Date-context overview and attention links |
| UX-03 | Stock Valuation | `/stock-valuation` | M1 selected item and FIFO layers |
| UX-04 | Reorder and EOQ | `/reorder` | M2 recommendations and dead stock |
| UX-05 | Supplier Performance | `/suppliers/performance` | M3 delivery reliability |
| UX-06 | Requisition Matching | `/requisitions/matching` | M4 source text and computed candidates |
| UX-07 | Clarification Needed | `/requisitions/clarifications` | Derived M4 exception list |
| UX-08 | Reports | `/reports` | R1-R5 selection and parameters |
| UX-09 | Report Preview | `/reports/{reportId}/preview` | Review before PDF/print |
| UX-10 | Access/error pages | Context-specific | Forbidden, missing page, unavailable |

Locations are proposed UI routes, not implemented endpoints. Sidebar order follows the table; Clarification Needed sits beneath Requisition Matching. Indicate current page by text/shape and an accessible current-page state. Keep navigation labels consistent with headings. Deep links offer Back to results and preserve originating filters where safe.

Primary journeys:

1. Valuation: choose item/date -> Compute Valuation -> inspect quantity/value/layers -> open R2 with explicit scope -> preview -> PDF.
2. Reorder: choose date -> inspect Order now rows -> inspect supplier context -> R3 preview and printable draft. No purchase is submitted.
3. Supplier: choose inclusive period -> inspect delivery evidence -> R4 with a separate dead-stock date.
4. Matching: retrieve existing requisition -> read text -> Run AI Matching -> inspect item, score, quantity, price and stock -> R5.
5. Clarification: choose period/reason -> inspect requisition -> review explanation. Follow-up happens outside IWAS; there is no Resolve control.
6. Reporting: choose R1-R5 -> supply dates -> Preview -> check scope/completeness -> Open PDF or Download PDF -> print.

## 4. Presentation organization and ownership

These are proposed locations only. Do not create source files or directories during documentation work.

| Location beneath `src/Presentation` | Future responsibility |
|---|---|
| `Views/Shared` | Shell, navigation, filters, status fragments and errors |
| `Views/Account`, `Views/Dashboard` | Conditional login and overview |
| `Views/StockValuation`, `Views/Reorder`, `Views/Suppliers` | M1-M3 views |
| `Views/Requisitions` | Matching and clarification |
| `Views/Reports` | Report selection and HTML previews |
| `ViewModels` | Display-ready page models; no domain calculations |
| `wwwroot/css` | Tokens, base styles, components, page exceptions and print |
| `wwwroot/js/shared` | Requests, formatting, query state, tables and lookup behavior |
| `wwwroot/js/pages` | One interaction coordinator per page |
| `wwwroot/vendor`, `wwwroot/fonts`, `wwwroot/images` | Local framework, icons and necessary assets |
| `Controllers` | Future thin web adapters, outside current implementation scope |

Under `test/Presentation`, plan rendering, browser interaction, accessibility, visual and print verification with shared display fixtures. Fixtures are expected inputs, not alternative Business implementations. Do not place tests under `src` or create a competing `tests` root.

Presentation formats supplied dates, currency and percentages; validates required inputs; manages disclosures; and requests filtered results. Business owns formulas, threshold decisions, parsing, ranking, source validity, totals and report completeness. Never determine a status from rounded UI values. View models only adapt supplied results.

## 5. Design system

### 5.1 Framework policy

Bootstrap is the recommended default; custom CSS defines IWAS tokens and restrained adjustments. Tailwind is an allowed alternative. Do not style the same component with both by default. If coexistence is selected later, define component ownership, reset isolation, utility prefixes where supported, cascade order, and visual checks for controls/tables/print. This is a project integration policy, not a claim that coexistence is impossible. Tailwind applies base resets through Preflight. [Tailwind documentation](https://tailwindcss.com/docs/preflight).

Use native HTML controls first. JavaScript enhances interaction; navigation, essential filter submission and report access need server-rendered fallbacks once backend integration exists. Razor MVC views are the recommended rendering approach.

### 5.2 Visual tokens

| Token | Proposed value/use |
|---|---|
| Canvas | `#F5F7F8` |
| Surface | `#FFFFFF` |
| Text / navigation surface | `#202A2F` |
| Secondary text | `#5B6670` |
| Decorative separator | `#D6DCE1`; do not assume adequate control contrast |
| Primary action | `#146C43`, white label |
| Link/focus | `#0B63CE` |
| Success | `#176B3A` on `#E7F6EC` |
| Warning | `#7A4D00` on `#FFF4CE` |
| Critical | `#9F1C14` on `#FDECEC` |
| Information | `#0B4F9C` on `#EAF2FF` |
| Spacing | 4, 8, 12, 16, 24, 32 px equivalents |
| Radius | 4 px controls, 6 px panels |
| Typography | System sans-serif; readable Bengali fallback for source content |
| Type scale | 16 px base, 14 px dense table, 18 px section title, 24 px page title; relative units later |
| Controls | 40 px normal minimum height; 44 px primary touch target |
| Focus | Visible 3 px outline with offset; never obscured |

These are proposals, not tested contrast results. Verify actual foreground/background and interaction states during implementation. Numeric values use tabular figures. Use one consistent local icon family with visible action labels; no emoji status language. Structure with spacing and borders, with shadows reserved for transient elevated surfaces.

### 5.3 Layout and responsiveness

At 992 px and above: approximately 240 px sidebar, 56 px header and 24 px content gutters. Below 992 px: labeled navigation disclosure above content, 16 px gutters and stacked filters. Use 12 px gutters when necessary on very narrow screens. Disclosure navigation must work by keyboard without trapping focus.

KPIs use one column below 768 px, two from 768 px, and up to five only when labels and values fit. Let grids wrap under enlarged text. The chosen tiers align with Bootstrap's documented mobile-first breakpoints. [Bootstrap breakpoints](https://getbootstrap.com/docs/5.3/layout/breakpoints/).

Tables retain authoritative columns inside a labeled horizontal-scroll region. At 320 px, the rest of the page reflows without horizontal scrolling. Never shrink text to force a wide table onto a phone. Offer a scroll hint when needed. Names and narrative text wrap; essential evidence never depends on hover. Read-only secondary detail can expand inline, but must remain accessible without JavaScript after integration.

## 6. Shared components

| Component | Content and behavior | Variations |
|---|---|---|
| Shell | Brand, navigation, title, optional user menu, skip link | Wide, narrow, restricted |
| Page header | One H1, short purpose, date/period, report link | Long title, unavailable report |
| Filter band | Persistent labels, helper text, primary action, Reset | Default, populated, invalid, busy |
| Lookup | Stable ID plus recognizable name/department | Searching, selected, empty, failed |
| KPI | Label, supplied value, unit, context | Loading, unavailable, actual zero |
| Status | Text plus optional icon; noninteractive | Good, watch list, order now, clarification |
| Table | Caption, headers, data, supplied totals and paging | Empty, partial, long text, narrow |
| Detail | Identity, source context, result and explanation | Complete, missing optional data, invalid source |
| Notice | What happened, effect on results, recovery | Validation, warning, partial, unavailable |
| Report toolbar | Preview, Open PDF, Download PDF, scope | Generating, failed, incomplete |

Lookup recommendation: exact-ID input works without JavaScript. Enhanced search starts at two characters with about 300 ms debounce, cancels obsolete requests, supports arrows/Enter/Escape, and exposes selected identity. Editing selected label text invalidates its old ID. Avoid enormous catalogue dropdowns. Never edit requisition source text through lookup.

Tables start at a proposed 25 rows/page, with 50/100 options only if supported. Server paging/sorting is the default; changes reset page to 1. Require stable secondary identity sorting. Local sorting/filtering is allowed only when the complete error-free dataset is loaded. Announce sort direction. No row-selection checkboxes, inline edits or bulk mutations.

Forms distinguish required and optional fields, explain allowed dates, preserve input on failure, and provide linked validation summaries plus inline errors. An unavailable action has a nearby explanation, not just a disabled button.

## 7. Screen specifications

### UX-01: Sign in, conditional

Center a form up to 420 px wide with IWAS name, username, password and Sign in. Support password managers and paste. No registration, user maintenance or role selector.

States: initial, submitting, generic invalid credentials, unavailable service, expired session. Keep username on failure; clear password; focus error summary. Return to a validated local destination after authentication. Backend authentication is deferred; a future mock identifies simulated access.

Acceptance: keyboard completion, labeled fields, generic failure text, no password in URL/UI, usable narrow and zoomed layout.

### UX-02: Dashboard

Layout: title and analysis date -> Apply -> KPIs -> completeness notice -> attention table -> retrieval context.

Proposed KPIs: warehouse FIFO value, reorder-now item count, dead-stock count, watch-list supplier count and clarification count. Values are supplied and access-limited. Failed modules read Unavailable, never 0. No grand total from incomplete data.

Attention columns: Type, Item/Supplier/Requisition, Condition, Status, View analysis. Links carry identity/date. Complete empty state: “No attention items for this analysis.” Unloaded data is separate.

ASSUMPTION: supplier and requisition metrics may use different windows. Require supplied period labels beside affected metrics. Until their windows are agreed, show links or unavailable states rather than invented counts.

Acceptance: each metric's scope is understandable; links open the correct context; partial data differs from no exceptions.

### UX-03: Stock Valuation, M1

SOURCE: PDF page 2. Required item and as-of date. Primary: Compute Valuation. Secondary after success: View valuation report. Idle copy: “Select an item and date to view its closing stock and FIFO layers.”

Order: identity/unit/date -> closing quantity and FIFO value -> receipt/issue context -> remaining layers -> report/retrieval context. Columns: receipt date, movement ID if supplied, remaining quantity, unit purchase price, layer value. Footer displays supplied quantity/value totals, never browser sums. Receipt/issue totals must state coverage; do not call them monthly unless the data defines a month.

Explain FIFO briefly: remaining stock is shown by its unconsumed receipt layers. Zero-stock success shows 0, BDT 0.00, and “No remaining FIFO layers.” Invalid or missing history is an error, not zero stock. Preserve selected item/date.

If item-scoped R2 is unsupported, label the report link “Warehouse valuation report for this date.” Never imply a warehouse total describes only the selected item.

Acceptance fixture: 08 Jul 2026, A4 paper, 90 reams, BDT 49,050.00; layers 30 at 535 and 60 at 550. Values come from PDF page 2; the UI displays them.

### UX-04: Reorder and EOQ, M2

SOURCE: PDF page 3. Filters: analysis date, optional item/category, recommendation status, dead-stock filter. Primary: Analyze reorder needs. Secondary: View R3. Reset restores visible documented defaults.

Initial table: Item/ID, Unit, Stock, Annual demand, Daily demand, EOQ, Effective lead time, ROP, Days to ROP, Dead stock, Recommendation. Inline read-only details expose supplied base lead time, supplier delay, selected supplier, ordering/holding costs, safety-stock contribution and demand window. This reduces the earlier initial column count while preserving evidence.

Prioritize supplied Order now, projected order, review/unavailable, then OK statuses. Use supplied sort keys rather than deriving decisions from colors or rounded values. “Order 45 now” is recommendation text, not a purchase button. Display units and days explicitly.

Explain dead stock as no issue in the last 180 days. Missing demand/lead time produces its supplied limitation, not 0. Assumption details identify the demand window and lead-time basis. No disposal controls.

Acceptance fixture: annual demand 3,650; daily demand 10; EOQ about 382 reams; effective lead time 5 days; stock 90; ROP 60; order in 3 days. This is the standalone PDF example. Combining its lead time with the separate supplier-delay example would change the expected result.

### UX-05: Supplier Performance, M3

SOURCE: PDF pages 3-4. Required inclusive start/end dates; optional supplier/standing. Primary: Analyze suppliers. Secondary: View R4. Show period above results.

Columns: Supplier ID/name, Delivered orders, On-time orders, On-time percentage, Late orders, Average delay of late orders, Standing. Optional read-only delivery evidence may show promised/actual dates when supplied; no delivery editing.

Proposed supplied summary: delivered orders, weighted on-time percentage, watch-list count, excluded open deliveries. Suppress incomplete totals. Never average displayed supplier percentages locally.

No deliveries reads “No delivered orders in this period,” not a fabricated 0% row. Distinguish zero delay from no late-delivery sample using availability/reason. Below-80% watch-list behavior is source-defined but rendered from Business status. Excellent/Good distinctions beyond source examples remain a handoff decision.

Acceptance fixture: SUP-07 Meghna Traders, 20 delivered, 17 on time, 85%, 3 late, 4-day average late delay, good standing. Do not imply M2 uses this same supplier period unless confirmed in the data.

### UX-06: Requisition Matching, M4

SOURCE: PDF pages 4-5. Required existing requisition ID/lookup. Display department, date and full read-only text. Include explicit stock as-of date. Primary: Run AI Matching.

Layout: lookup/date -> source text -> status/item -> similarity/explanation -> quantity/price/stock -> candidates -> related report/clarification links. Source text wraps, preserves Unicode and is not editable.

Facts: matched item ID/name, text similarity, parsed quantity/unit, catalogue unit price, total price, stock on hand, requested-quantity sufficiency when known. Similarity is not probability of correctness. Explain the source threshold of at least 80%, but use supplied status rather than rounding and comparing scores in JavaScript.

Recommended candidate table: up to three supplied candidates; rank, item ID/name, similarity and common-term count when available. Explain with supplied terms only; no frontend tokenization or ranking.

Clarification shows reason and best candidate/score if present, with “Needs clarification outside IWAS.” No Approve, Override, Edit or Resolve. Missing quantity or catalogue price does not erase a valid semantic match. Show “Price unavailable” or “Quantity not identified.” Stock-on-hand remains distinct from sufficiency.

Acceptance fixture: RQ-0871 -> IT-3320 Gel Pen, Black; 80%; 12 units; BDT 15.00/unit; BDT 180.00; stock 430. Also specify below threshold, tied candidates with supplied status, missing quantity, absent price, empty text and zero stock.

### UX-07: Clarification Needed

SOURCE: M4 queue; dedicated screen is recommended. Filters: requisition period, stock date, optional reason/department. Primary: View clarifications. Columns: ID, Department, Date, Text excerpt, Best candidate, Similarity, Reason, Inspect.

Excerpts may be limited to 160 grapheme clusters with ellipsis. Inspect opens UX-06 with full text and selected analysis date. Preserve filters on return. Empty copy: “No requisitions need clarification for these filters.” Unavailable analysis is not successful no-clarification output.

The list is computed. No assignees, completion checkboxes, saved resolutions or history. Acceptance: inspect/return preserves context; English/Bengali excerpts wrap; no action changes source data or marks a record resolved.

### UX-08: Reports hub

Desktop: compact report list left; selected report purpose, parameters and actions right. Below 768 px: stacked links or labeled select above the form. Always show full title, not just R1-R5.

Show only applicable parameters. Primary: Preview. Secondary: Open PDF and Download PDF for the same validated scope. Distinguish analysis date, supplier period, requisition period and generation time. Defaults are visible. Switching report type clears incompatible fields and stale previews.

No saved history, report builder, email or scheduled export. Any future UI-only mock must label output “Illustrative sample.”

### UX-09: Report preview

Show report title, full scope, generation/retrieval context and Back to reports, followed by formal content. Toolbar: Open PDF, Download PDF and print guidance. Prefer generated PDF for final A4 printing and deterministic page numbering. HTML preview does not prove PDF fidelity.

Reports cover all rows in declared scope, not the current screen page. Complete empty scope may yield an explicit empty report. Required-row failures block authoritative output; do not omit failed financial rows silently.

Reuse supplied snapshot identity when available. Otherwise generation timestamps and source-change possibilities must be clear; refresh/reconcile before comparing. Snapshot consistency is a future backend requirement, not a browser guarantee.

### UX-10: Access and error pages

Forbidden: “You do not have access to this page,” with permitted navigation. Missing page/entity: retain safe context and offer return/correction. Unavailable: explain retrieval failure, Retry and safe support reference. Expired session: explain sign-in and preserve a safe local return destination.

Never display SQL, stack traces, secrets or raw server HTML. Access failures clear confidential stale results. Mock permission screens do not enforce security.

## 8. Report specifications

SOURCE: A4, company header, report title, date/period and page number, PDF pages 5-7. Padma Trading Ltd is sample branding; actual company identity is open. Mark sample branding until confirmed.

| Report | Visible parameters | Required body | Footer/additional content |
|---|---|---|---|
| R1 Daily Stock Movement | Selected day | Item, Unit, Opening, Received, Issued, Closing | Receipt/issue movement counts, not quantity sums |
| R2 Stock Valuation (FIFO) | As-of date, explicit item/warehouse scope | Item, Closing quantity, FIFO layers, Unit prices, Value | Supplied warehouse total only for complete warehouse scope |
| R3 Reorder List | Analysis date | Item, Stock, ROP, EOQ, Days to ROP, Recommendation | Items to order today; printable requisition draft |
| R4 Supplier Performance and Dead Stock | Supplier period and dead-stock date | Supplier, Orders, On-time %, Average delay, Standing | Dead-stock item, units, value and disposal recommendation |
| R5 AI Requisition Matching Log | Requisition day/period; stock date where relevant | ID, Department, Matched item/best candidate, Similarity, Status | Total, auto-matched and clarification counts |

R3 draft proposal: “Draft - analytical recommendation”; analysis date; item identity; recommended quantity/unit; note that no order has been placed. No fictitious order number or approved signature state. These details beyond the required draft are recommendations.

R4 always includes supplier and dead-stock sections, even if one is empty. Separate period from stock date. Dead-stock valuation method is unspecified in the source; FIFO remains a labeled inherited assumption, with the supplied method visible.

Print proposal: portrait by default; landscape for genuinely wide R2/R3 after review; 12-15 mm margins; at least 10 pt body text. Repeat column headers. Keep headings with content and avoid splitting ordinary rows. Long text may wrap/split where necessary; never shrink a whole report to one page. Formal output has Page X of Y. Hide navigation, filters and buttons; disable sticky positioning; ensure grayscale meaning.

Later acceptance evidence: short, empty, multi-page, long-name, Bengali, large-value and unavailable-value variants as applicable. Inspect actual generated PDFs. No PDF generation is part of current documentation work.

## 9. Interaction, state and recovery

A result belongs to a parameter key: screen, identities, dates/periods, filters, page, size, sort and direction. Changing any parameter invalidates current results or replaces them with “Apply filters to view updated results.” Disable report actions until scope is validated.

| State | Treatment | Recovery/focus |
|---|---|---|
| Idle | Specific input prompt | Normal tab order |
| Invalid | Linked summary and inline errors; retain input | Focus summary, links reach fields |
| Loading | Busy status, stable placeholder, no duplicate submit | Announce once; retain initiating focus |
| Success | Results, context, retrieval time | Announce completion/count |
| Empty | No records for explicit scope | Change/reset filters |
| Partial | Good rows plus issue counts; suppress unsafe totals | Inspect reasons/retry |
| Refreshing | Same-key result labeled Updating | Only newest request completes |
| Stale | Same-key result with time/warning after transient failure | Retry; no authoritative export |
| Unavailable | No safe current result | Retry/change scope |
| Superseded | No error toast | Ignore old response |
| Forbidden/expired | Clear restricted data | Allowed page/sign-in |

Stale results are allowed only for identical-key transient refresh failure. Integrity/access failures and changed keys clear unsafe data. Partial counts distinguish successes from failures. Null/unavailable totals are not zero.

Future mapping: invalid input -> validation summary; missing entity -> correct ID; source-integrity issue -> safe explanation; throttling -> supplied retry delay; network/unavailable -> Retry; forbidden -> access page; expired session -> one reauthentication path. Avoid loops and repeated automatic expensive analysis.

URLs contain safe IDs, dates, filters and paging only. Never source text, credentials or computed results. Back/Forward restores canonical context and requests matching data. Do not persist confidential results in local storage. Prefer explicit Apply over recalculation per keystroke.

## 10. Accessibility, language and copy

RECOMMENDATION: WCAG 2.2 AA is a future target, not a compliance claim. Use landmarks, skip link, logical headings, persistent labels, table captions/header relationships, and accessible status messages. Keyboard users complete every journey. [W3C WCAG guidance](https://www.w3.org/WAI/WCAG22/Understanding/).

Plan at least 4.5:1 normal-text contrast and 3:1 for large text/meaningful UI boundaries. Check 200% text enlargement and 320 CSS-pixel reflow; two-dimensional tables may scroll in labeled regions. Follow the 24 CSS-pixel minimum target criterion and its exceptions; prefer 44 px primary touch controls. [W3C target-size guidance](https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum).

No color-, hover- or animation-only meaning. Respect reduced motion. Disclosures expose state; any later modal requires focus management, Escape and return to trigger. Prefer inline analytical details.

English UI is an assumption. Preserve Bengali/Unicode source content without transliteration. Full detail text is never truncated. Add language/direction metadata where known. Display dates as 08 Jul 2026; query dates use unambiguous year-month-day. Asia/Dhaka is the inherited business-timezone assumption, supplied by the backend; browser timezone must not silently choose analysis dates.

Display BDT and consistent two-decimal monetary values unless agreed precision requires otherwise. Preserve meaningful quantity fractions. Include units. Distinguish 0, Not available, Not applicable and No records. Stock on hand does not imply sufficient stock when quantity is unknown.

Preferred copy: Compute valuation; Analyze reorder needs; Run AI matching; View report; Price unavailable; No delivered orders in this period; Some items could not be analyzed. Avoid backend jargon and unsupported AI-confidence claims.

## 11. Future data handoff

This describes display needs, not classes, API implementation or database schemas.

| Consumer | Supplied information needed |
|---|---|
| Shared | Validated context, timezone/date, retrieval time, permitted actions, completeness/issues |
| Lookup | Stable ID, label, unit/department, paging/failure state |
| M1 | Identity/unit, stock, value, layers, receipt/issue coverage/totals |
| M2 | Demand window, stock, EOQ, lead-time basis, ROP, days, status, dead-stock flag/reasons |
| M3 | Identity/period, delivered/on-time/late counts, percentage, delay, standing/exclusions |
| M4 | Source text, item/score/status, candidates, quantity, price, stock and reasons |
| Clarification | Derived rows/reasons, counts, context and detail identity |
| Dashboard | Authorized metrics, per-metric period, links and partial state |
| Reports | Full-scope rows/totals, branding, parameters, completeness, snapshot/time |

Nullable values need reasons. Paged results need count and stable sorting metadata. Permissions and allowed date/range limits are supplied rather than guessed. Future Presentation-only prototypes use labeled fixtures or unavailable states for absent data; do not implement Business logic to unblock a screen.

## 12. Fixtures and source traceability

These are future display inputs, not proof of analytical correctness. PDF examples remain separate illustrative cases unless the Business team supplies a coherent combined dataset.

| Scenario | Source/screen | Visible evidence |
|---|---|---|
| UI-01 | PDF p2 / UX-03 | 90 reams, BDT 49,050, two layers |
| UI-02 | PDF p3 / UX-04 | EOQ about 382, ROP 60, order in 3 days |
| UI-03 | PDF p4 / UX-05 | 20 orders, 85%, 4-day late delay |
| UI-04 | PDF p5 / UX-06 | RQ-0871 -> IT-3320, 80%, 12 units, BDT 180, stock 430 |
| UI-05 | M4 / UX-06/07 | Low similarity -> clarification, no resolution action |
| UI-06 | M1 | True zero differs from invalid history |
| UI-07 | Shared | Partial data suppresses unsafe totals/export |
| UI-08 | Shared | Changed date cannot show old result as current |
| UI-09 | Shared | Obsolete response cannot overwrite new data |
| UI-10 | M4 | Missing price/quantity preserves valid match |
| UI-11 | PDF p1/p7 | No mutation or approval controls |
| UI-12 | R1, p5 | Movement values and counts |
| UI-13 | R2, p6 | Layers, values, full-scope total |
| UI-14 | R3, p6 | Reorder table and draft; no submitted order |
| UI-15 | R4, p6 | Supplier and dead-stock sections/disposal text |
| UI-16 | R5, p6 | Match rows and summary counts |
| UI-17 | All | Keyboard, narrow layout, Unicode, visible focus |
| UI-18 | Conditional access | Navigation, expired and forbidden variants |

For later M4 UI demonstrations, plan at least ten supplied fixtures: source 80% match, higher-score match, below-threshold, tie, no candidates, empty text, unparsed quantity, absent price, zero stock and insufficient stock. The PDF's ten-requisition demonstration eventually needs real computed results; mock screens alone do not satisfy it.

## 13. Decisions and remaining inputs

| ID | Status | Decision/input | Treatment until resolved |
|---|---|---|---|
| D01 | OWNER | Exact src layers and test root | Singular names throughout |
| D02 | RECOMMENDATION | Bootstrap default, Tailwind allowed alternative | One coherent component system |
| D03 | RECOMMENDATION | Razor MVC | Progressive enhancement planned |
| D04 | ASSUMPTION | Authentication/roles | Conditional screens, manager-first baseline |
| D05 | OPEN | Real company name/logo | Labeled sample branding |
| D06 | ASSUMPTION | English UI, Unicode data, Dhaka dates | Explicit labels/date context |
| D07 | OPEN | Catalogue price source | Price unavailable; no receipt-price substitution |
| D08 | OPEN | Demand window, lead time, supplier selection | Supplied basis/assumption labels |
| D09 | OPEN | Tie policy and detailed standing | Supplied status/reason |
| D10 | OPEN | Report snapshot and allowed filters | Explicit scope/timestamps |
| D11 | ASSUMPTION | Dead-stock FIFO value | Expose supplied method |

These inputs do not block documentation. Resolve D02 before styling, D04 before access integration, D05 before final branding, and D07-D11 before integrated correctness claims. No approval is inferred from this plan.

## 14. Future review and acceptance

Browser matrix: Edge, Chrome, Firefox and Safari where available; record versions at implementation time. Viewports: 1440x900, 1024x768, 768x1024, 390x844 and 320x800. Check keyboard, screen-reader forms/tables, zoom, reduced motion, long text and error/loading states. Automated accessibility checks supplement manual inspection.

Meaningful future tests cover zero/null rendering, filters/URL restoration, request races, partial data, accessible lookup, role variants, report scope and print overflow. Do not recalculate business formulas in UI tests or merely mirror markup. Verify essential no-JavaScript journeys when real server rendering is integrated.

Future UI completion requires applicable UX-01 through UX-10, all M1-M5/R1-R5 coverage, reviewed journeys, no clipped controls outside allowed table scrolling, resolved critical accessibility issues, and inspected A4 output. Separate fixture-only from integrated evidence. Current completion is documentation, not these future implementation claims.

## 15. Business and Model documentation handoff, 9 September 2026

The [Business plan](IWAS_BUSINESS_PLAN.md) now owns analytical behavior and defaults behind the screens. The [Model plan](IWAS_MODEL_PLAN.md) owns typed source/query/result contracts. The [backend delivery plan](IWAS_BACKEND_IMPLEMENTATION_BLUEPRINT.md) maps them to the existing fixture implementation. Earlier prohibitions on designing Business/Model internals applied only to the UI documentation milestone and are superseded by the current request. No UI code changes are made in this documentation revision.

Section 13 source gaps now have explicit proposed defaults in those plans. Unknown unit compatibility must not produce authoritative pricing/sufficiency. Candidate stock in a tied result must be labeled candidate-only or omitted. Synthetic display scores and number phrases are not mandatory algorithm results; retain honest unavailable states when real interpretation differs.

