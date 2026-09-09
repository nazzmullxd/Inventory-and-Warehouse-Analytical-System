# IWAS detailed Business layer plan

Status: documentation baseline, 9 September 2026. This specifies future work in `src/Business` and `test/Business`; it does not implement it. The existing Presentation is a Razor MVC preview with labeled fixtures. Read alongside the [Model plan](IWAS_MODEL_PLAN.md), [UI/UX plan](IWAS_UI_UX_PLAN.md), and [backend delivery plan](IWAS_BACKEND_IMPLEMENTATION_BLUEPRINT.md).

## 1. Purpose, authority and boundaries

Business turns existing warehouse records into authoritative analytical results for the current screens. It owns stock chronology, valuation, demand, replenishment metrics, supplier performance, text matching, quantity interpretation, report data and completeness. It returns typed values and stable statuses, not formatted table strings.

SOURCE: requirements and formulas in the assignment PDF. OWNER: ASP.NET Core backend, three layers under `src/Model`, `src/Business`, `src/Presentation`, root `test`, and documentation-only work now. RECOMMENDATION: implementation design in this plan. ASSUMPTION: a stated default where the assignment is incomplete. Source gaps must remain visible in results and tests.

Dependency direction is Presentation -> Business -> Model. Business uses Model query abstractions and immutable data contracts; it knows nothing about Razor, HTML, Bootstrap, Tailwind, HTTP responses, cookies, SQL syntax or PDF libraries. All source access passes through Model. Business never creates, updates, deletes, approves, reserves or issues warehouse records. A clarification queue and an R3 draft are computed output only.

Authentication and endpoint permission enforcement belong to the future web integration boundary. Business receives already-authorized scope where appropriate; it must not broaden that scope. Browser-supplied role labels are not trusted authority.

## 2. Existing UI integration baseline

`WorkspaceController` currently validates GET inputs and chooses fixture views. `WorkspaceModel` stores display context, including strings for dates and sample state. `DisplayFixtures` contains preformatted rows and ten synthetic/source-inspired requisition outcomes. These are Presentation artifacts, not Business input entities.

Future integration must replace fixture selection with typed use-case results. Do not parse “BDT 49,050.00,” “≈ 382,” or “Order 45 now” to recover domain values. Presentation formats values only after Business computes them. The current date restrictions to 08 Jul 2026 and the `scenario` query parameter are preview behavior and must not become analytical rules.

The source examples and UI fixtures are not one coherent historical dataset. In particular, adding the M3 example's four-day delay to the M2 example's five-day lead time changes ROP from 60 to 108. Keep isolated source tests and separate integrated fixtures. Do not force every synthetic UI score to be reproduced by the real matcher.

## 3. Proposed organization

| Area beneath `src/Business` | Responsibility |
|---|---|
| `Abstractions` | Public use-case contracts and replaceable business strategies |
| `Common` | Business clock, date windows, numerical policy, issues, limits |
| `Stock` | Shared chronological stock ledger and conservation checks |
| `Valuation` | M1 layer valuation and warehouse valuation orchestration |
| `Suppliers` | M3 performance and selected-supplier delay resolution |
| `Reorder` | M2 demand, EOQ, lead time, ROP, projections and dead stock |
| `Matching` | M4 normalization, cosine, ranking, quantity/unit interpretation |
| `Reporting` | M5 typed R1-R5 data and formal completeness checks |
| `Dashboard` | Authorized overview composition and per-metric context |
| `Validation` | Cross-record and calculation validation |

Use-case contracts and implementations belong to Business; shared request/result shapes belong to Model. No additional top-level application layer is needed. Proposed areas describe future responsibilities, not files created now.

## 4. Use-case catalogue and flow

| ID | Use case | Inputs | Output/consumer |
|---|---|---|---|
| BUS-01 | Inspect stock position | Item, as-of date, read context | Quantity and remaining layers for shared consumers |
| BUS-02 | Value item/warehouse | Item or warehouse scope, date | M1 result, UX-03 and R2 |
| BUS-03 | Analyze suppliers | Inclusive period, optional supplier/standing | M3 list, summary, UX-05 and R4 |
| BUS-04 | Resolve item supplier delay | Item, supplier analysis window | Selected supplier, delay and provenance for M2 |
| BUS-05 | Analyze reorder | Date, item/category/status filters, paging | M2 list, details, UX-04 and R3 |
| BUS-06 | Match requisition | Existing requisition ID, stock date | Match and enrichment for UX-06 |
| BUS-07 | Analyze requisition period | Period, stock date, status/reason filters | Matching/clarification list, UX-07 and R5 |
| BUS-08 | Build report data | R1-R5 identifier, validated report scope | Complete typed report data for rendering |
| BUS-09 | Compose dashboard | Date and authorized module scope | Per-metric values, periods, issues, UX-02 |

Common flow: validate request -> establish consistent read context -> retrieve bounded canonical data -> validate needed source invariants -> calculate -> determine statuses -> apply computed filters/sort -> page where requested -> return result and context. Model owns actual reads and snapshot mechanics; Business determines the complete analytical operation that shares them. Nested module calls reuse the outer context.

## 5. Common policies

### 5.1 Dates and identities

Use date-only values for business dates. Ranges are inclusive. ASSUMPTION: reporting timezone Asia/Dhaka, with an injectable business clock. Reject future analysis dates by default. Do not hardcode the UI sample date. A 365-day demand window is analysis date minus 364 days through analysis date; the 180-day issue window starts 179 days before it. Leap years do not change the source divisor of 365.

M4 stock analysis date must be on/after requisition date. Period matching requires stock date on/after the period end to avoid mixing invalid historical enrichment. Supplier period membership uses actual delivery date, an explicit assumption.

IDs are opaque strings. Trim request-edge whitespace, preserve display identity, and use the agreed canonical comparison in Model. No numeric parsing or hidden ID-based business classification.

### 5.2 Numbers and availability

Use decimal quantities, costs, monetary results and delay values. Calculate with unrounded values; Presentation rounds only for display. Suggested whole-unit EOQ is a separate derived field, rounded to nearest whole unit with midpoint away from zero. This rounding is an assumption, not a unit-pack purchasing rule.

EOQ square root must use a documented numerical approach with checked conversion and tolerance tests; protect intermediates against overflow. Similarity may expose a floating-point display value, but integer term counts determine ranking and threshold decisions exactly. Source quantity precision is proposed at four decimals; validate actual source precision before mapping.

Every unavailable field carries a reason. Zero means a known numerical zero. No delivery sample differs from zero delay. Unknown quantity differs from zero requested quantity. No unit conversion is inferred from matching names.

### 5.3 Result and failure semantics

| Severity | Meaning | Interactive effect | Formal report effect |
|---|---|---|---|
| Blocking | Data cannot support the requested result | Item failure; bulk row issue | Block affected report, no authoritative partial total |
| Warning | Valid result uses explicit assumption/limitation | Values plus reason | Values plus note |
| Informational | Expected absence or optional unavailable field | Neutral state | Not available/not applicable |

Business issues contain stable code, safe entity identity, affected field, severity and retryability. They do not contain HTTP status, SQL, secrets or full requisition text. Presentation maps them to user messages/HTTP behavior. See Model for the shared vocabulary.

Cancelled work produces no fabricated error result. Timeouts/unavailable sources are distinct from invalid source data. Retrying an invalid input or inconsistent ledger automatically will not fix it.

## 6. Shared stock ledger, BUS-01

All consumers use one chronological stock calculation: M1, M2 stock, M4 stock and R1-R4. Do not derive stock independently from UI rows or duplicate FIFO in reporting.

Required input: item identity, complete trustworthy movement history through the selected date, explicit same-day chronology, and source coverage metadata. Opening stock must have real opening receipt layers with quantities/prices where valuation needs them. A truncated recent history is not a complete ledger.

Processing:

1. Exclude movements after the as-of date.
2. Validate unique movement IDs, item references, positive quantities, known movement types and chronology.
3. Traverse by movement date and trusted source sequence. Multiple same-day movements without proven order block authoritative stock analysis. Do not sort receipts before issues to hide an underflow.
4. Append receipts as FIFO layers. Consume issues from oldest nonempty layers, splitting when necessary.
5. If an issue exceeds available stock at that point, return `stock_history_insufficient`. Later receipts cannot repair earlier negative history.
6. Return receipt/issue totals, closing quantity and remaining layers oldest-first.

Invariants: receipts minus issues equals closing quantity; remaining quantities sum to closing quantity; quantities never become negative; consumed layers disappear; repeated calculation over identical input gives identical result.

No movements is valid zero only if source coverage establishes a trustworthy zero opening position. Missing history is unavailable, not zero. Missing receipt price need not block quantity-only consumers, but valuation applies stricter checks. Decimal fractions are supported.

## 7. M1 FIFO valuation, BUS-02

SOURCE: PDF page 2. Closing stock is receipts minus issues. FIFO value is the sum of each remaining receipt quantity multiplied by its purchase price.

Load stock input and use BUS-01. Validate prices before emitting valuation. Proposed strict policy: missing/negative prices on receipts used in the requested ledger block M1, even if a malformed receipt has been fully consumed. This extends the mathematical requirement and is labeled ASSUMPTION for source-integrity consistency. Zero receipt price is valid with a warning. Never substitute catalogue price, latest price or weighted-average cost.

Return item identity/unit, as-of date, history coverage, received/issued quantities, closing quantity, FIFO value and receipt layers with layer values. Calculate all totals in Business. R2 uses exactly this service's valuation logic.

Warehouse valuation may return interactive row failures, but its total is absent when any included item fails. A full R2 fails rather than omitting bad items. Totals cannot sum unlike item quantities; only monetary values in the same currency may aggregate.

Acceptance: receipts 100 at 520, 80 at 535, 60 at 550 with chronological issues 120 then 30 yield 90 reams, layers 30 at 535 and 60 at 550, BDT 49,050. Include zero, partial-layer consumption, fractional quantities, same-day ambiguity, unknown item, missing prices, prefix underflow and overflow.

## 8. M3 supplier performance, BUS-03/04

SOURCE: PDF pages 3-4. On-time delivery means actual date on/before promised date. On-time ratio is on-time orders divided by delivered orders. Average late delay is the sum of late days divided by late orders, excluding on-time/early orders. Watch list means ratio strictly below 0.80.

ASSUMPTION: select deliveries whose actual date falls within the inclusive period. Open rows with no actual date are excluded. A separate optional count of open rows promised within the period is explanatory only. A supplier with no delivered orders is absent from the list or returns explicit no-sample detail, never 0% performance.

Delivery identity is a source prerequisite: metrics count purchase orders, not arbitrary item lines. Model must supply proven order identity/granularity. Multi-item or split-delivery semantics require an agreed canonical adapter before M3 is authoritative. Never manufacture one order per database row without evidence.

Standing: below 80% Watch list is SOURCE; 80% to below 100% Good and exactly 100% Excellent are ASSUMPTIONS consistent with examples. No late orders means average late delay is null with `no_late_deliveries`; M2 uses a zero delay contribution, without turning the displayed average into a sample of zero days.

Aggregate on-time ratio uses total on-time orders / total delivered orders, not an average of percentages. Group by supplier ID, not name. Conflicting names under the same ID and duplicate order keys are integrity issues, not silent merges.

Supplier choice for M2 is ASSUMPTION: among suppliers delivering the item in the trailing 365-day window, choose greatest distinct delivered-order count, then latest actual delivery, then canonical supplier ID. Compute delay over that selected supplier's item-specific orders in that window. Expose that scope; its delay may differ from an all-item supplier screen. No history contributes zero with `no_supplier_history` warning. This default is replaceable if a preferred-supplier source exists.

Acceptance: 17 of 20 on time -> 85%; late delays 2, 4, 6 -> 4 days. Test exactly 80%, below threshold, early deliveries, null actual dates, no late sample, duplicate IDs, multi-item granularity, weighted aggregate and deterministic selection ties.

## 9. M2 reorder, EOQ and dead stock, BUS-05

SOURCE: PDF page 3. Let D be annual demand, So ordering cost, H annual holding cost per unit, L effective lead time and S closing stock.

| Metric | Rule | Source status |
|---|---|---|
| Annual demand D | Sum issue quantities in the inclusive rolling 365-day window | ASSUMPTION extraction window |
| Daily demand d | D / 365 | SOURCE |
| Raw EOQ | Square root of (2 × D × So / H) | SOURCE |
| Effective lead time | Base lead time + selected supplier average late delay | SOURCE example; selection/base source are assumptions |
| Lead demand | L × d | SOURCE |
| Safety stock | 0.20 × L × d | SOURCE |
| Reorder point | Lead demand + safety stock | SOURCE |
| Reorder now | S <= ROP using unrounded values | SOURCE |
| Days to ROP | 0 when already at/below ROP; otherwise ceiling((S - ROP) / d) if d > 0 | ASSUMPTION whole-day projection |
| Dead stock | No issue in the inclusive last 180 days | SOURCE |

Use stock ledger quantities, canonical issue history and BUS-04 delay resolution. Do not require FIFO valuation prices merely to calculate quantities or reorder metrics. Require reliable coverage of the entire demand/dead-stock windows; unknown history is not zero demand.

Base lead time is absent from the listed datasets. Proposed demonstration default: configured five days, with explicit provenance and item overrides if supplied. Treat it as an assumption requiring production agreement. Return base, delay and effective lead time separately.

Validate H > 0 before division, including when D=0. Negative costs fail. D=0 with valid H produces EOQ=0, daily demand=0 and ROP=0. So=0 produces EOQ=0 and a review warning. For positive raw EOQ that rounds below one, proposed suggestion is one whole unit; this is an assumption. Fractional-unit purchasing rules require a later decision.

Keep the literal `ReorderNow` flag separate from recommendation copy. Proposed action precedence:

1. Blocking input/history/calculation error -> row issue, no fabricated result.
2. No demand -> ReviewNoDemand.
3. Dead stock with positive stock -> ReviewDeadStock.
4. Zero suggested order quantity -> ReviewOrderQuantity, never “Order 0 now.”
5. At/below ROP -> OrderNow with suggested quantity.
6. Projectable future ROP -> OrderInDays.
7. Otherwise -> OK with reason.

Steps 2-4 are ASSUMPTION safety messaging and must not erase the literal source flag. An item may be dead under the source rule even at zero stock. Disposal candidates require positive stock, a separate assumption used by R4. An issue exactly 179 days before analysis is inside the window; exactly 180 days before is outside.

Return all formula inputs, window dates, stock, raw/suggested EOQ, lead-time provenance, safety stock, ROP, raw flag, projected days, last issue, dead-stock reason, action and warnings. UI filters use supplied status codes, not translated labels.

Acceptance: D=3650, So=800, H=40, L=5, S=90 -> raw EOQ approximately 382.09946349, suggestion 382, daily demand 10, ROP 60, reorder false, days 3. Separate integration case: adding delay 4 gives L=9, safety stock 18, ROP 108 and reorder true. Test equality at ROP, leap days, incomplete history, 179/180-day boundaries, zero costs/demand and very large values.

## 10. M4 requisition matching, BUS-06/07

### 10.1 Scope and pipeline

SOURCE: PDF pages 4-5 specifies binary term-vector cosine matching, at least 80% for automatic matching, quantity interpretation including one dozen, catalogue pricing and stock evidence. TF-IDF is optional and deferred. Baseline needs no external AI service.

Retrieve an existing requisition and catalogue descriptions in one consistent source context. Preserve source text unchanged. Derive separate normalized terms; parse quantity spans; compare catalogue descriptions; rank; decide match/clarification; enrich a unique match with price/stock. Never store a match or change a requisition.

Catalogue similarity uses description only. Adding item name/category could alter source scores and is not the baseline. Display fields remain available separately.

### 10.2 Normalization policy

The PDF-provided term sets retain `office` in catalogue text but omit it from requisition text. A single symmetric stop-word list does not reproduce that example. Preserve a pure cosine test over the provided sets; mark field-aware preprocessing as an ASSUMPTION, not a source rule.

Proposed versioned pipeline: Unicode NFKC, invariant lowercase, identify quantity spans without changing source text, remove recognized request context such as “for the accounts office” using department metadata, replace punctuation with boundaries, tokenize Unicode letters/digits, remove stop words, apply conservative English plural normalization, deduplicate terms. Preserve Bengali terms as normalized exact tokens; no transliteration, translation, embeddings or automatic synonyms.

Initial stop-word proposal: a, an, and, are, as, at, be, by, for, from, in, into, is, it, of, on, or, that, the, this, to, with. Proposed plural policy: ies -> y for words longer than four letters; remove es only after s/x/z/ch/sh; remove final s for words longer than three except ss/us/is endings. These heuristics require regression tests and can be revised with a normalizer version change.

Do not remove arbitrary numbers blindly: model identifiers such as A4 can be meaningful. Recognized quantity spans are excluded from matching; ambiguous numeric spans need explicit parser outcomes. Excessively long text/term counts cause a safe limit issue rather than silent truncation. Exact limits are implementation configuration reviewed against real data.

### 10.3 Similarity, rank and ties

For distinct requisition terms A and catalogue terms C, similarity = common term count / square root(|A| × |C|). Empty sets produce zero similarity, with a reason. Duplicate words do not increase binary weight.

Let k be common count, a request count and c candidate count. Exact 80% test: 25 × k² >= 16 × a × c, only for nonempty sets. Compare candidates by k1² × c2 versus k2² × c1; use checked sufficiently wide integer arithmetic. This avoids rounded display values changing threshold decisions.

Unique top qualifying score -> AutoMatched. Below threshold -> ClarificationRequired/BelowThreshold. Exact top tie above threshold -> ClarificationRequired/AmbiguousTopScore, an ASSUMPTION. Identity ordering stabilizes candidate display; it never resolves the semantic ambiguity. No candidates, no usable descriptions and empty requisition terms have distinct reasons. Return up to three candidates as a recommendation.

### 10.4 Quantity and unit interpretation

SOURCE minimum: one dozen -> 12. Proposed baseline additionally recognizes integer quantities with adjacent unit/item noun, number words one through twelve, and one/two dozen. Reject ambiguous multiple quantities, ranges, dimensions, dates, unknown packaging conversions and unsupported number phrases with nullable quantity and a specific reason. Do not default to one.

Use versioned unit aliases, initially piece/pieces/pc/pcs/each/unit/units -> each, ream/reams -> ream, box/boxes -> box. Dozen conversion yields countable base units only; it does not establish that one dozen boxes equals twelve pieces. Compare interpretation to matched item's unit. Known mismatch blocks pricing and sufficiency; unknown compatibility preserves lexical quantity but marks pricing/sufficiency unavailable until a safe base-unit interpretation exists. Bare count or an item noun may be treated as the matched base unit only under an explicit, warning-bearing assumption. This stricter enrichment policy supersedes the archive's looser treatment of unknown units.

The UI's synthetic “five hundred” -> 500 example is not a mandatory parser feature. Either extend and test the grammar in a separately recorded decision or return unsupported quantity while retaining a valid semantic match. Never hardcode RQ-0880's displayed outcome.

### 10.5 Enrichment and clarification

For a unique automatic match, catalogue price comes only from the mapped catalogue price. Missing price leaves the match intact; total is unavailable. Total = validated base-unit quantity × catalogue price when both exist and units are compatible. Stock uses BUS-01 at the explicit date. Sufficiency compares stock to the same base-unit quantity; stock-on-hand may remain available when quantity is unknown.

If stock history fails, retain the semantic result with stock unavailable and a material enrichment issue. R5 may include that explicit unavailable stock state because it does not aggregate stock value; a report requiring those stock values must block. Distinguish semantic completeness from optional enrichment completeness.

For clarification, show candidates and reason without presenting one candidate's price/stock as an accepted match. The current tied-candidate fixture shows candidate stock; future integrated display must label it candidate-only or omit it. Clarification is recomputed and never assigned, approved, dismissed or resolved in IWAS.

Acceptance: source term sets each contain five terms, intersection four -> exactly 0.80; RQ-0871 matches IT-3320, quantity 12, price 15, total 180, stock 430 when those source inputs exist. Test exact/near threshold, exact ties, empty sets, repeated words, Unicode, unit mismatch, absent price, unsafe stock history, supported dozen parsing and unsupported phrases independently of synthetic display fixtures.

## 11. M5 report data, BUS-08

Business builds typed report content; Presentation owns HTML/PDF layout, A4 sizing, fonts, page numbers and file delivery. Both consume the same Business outputs. Business never returns PDF bytes or formatting fragments.

| Report | Business content and scope |
|---|---|
| R1 | Opening stock through previous day; receipts/issues on selected day; closing stock; movement counts, not quantity counts |
| R2 | Complete scoped FIFO valuations and warehouse monetary total where applicable |
| R3 | Reorder results, actionable count, and draft lines from supplied recommendation policy |
| R4 | Supplier metrics for period plus positive-stock dead-stock lines valued at explicit stock date; disposal recommendation data |
| R5 | Requisition matching results for period, stock context where relevant, matched/clarification counts |

R1 default includes all catalogue items with any opening/closing stock or daily movement; this inclusion rule is an ASSUMPTION. R2 warehouse scope includes catalogue items, including valid zero rows. R3 default includes the full evaluated catalogue so review states remain visible. R4 dead-stock value uses FIFO, an ASSUMPTION. R5 is recomputed, not a persisted audit log.

Formal output uses the full declared result set, never current UI paging. Any failure preventing required rows/counts/financial totals blocks the report. Optional missing catalogue price in a valid matching row can be shown as unavailable. Never produce a misleading “complete” monetary total from partial rows.

Report metadata contains report ID, parameters, business timezone, source snapshot/read context, generation time, counts, assumptions and completeness. One generation uses one consistent source version. Across later HTML/PDF requests, same parameters alone do not guarantee identical source data; use reproducible snapshot/version capability or clearly separate generation times and regenerate preview. Do not hold a database transaction open while a user reviews a page.

## 12. Dashboard and list semantics

Dashboard delegates to existing use cases, never duplicates formulas. Proposed periods: stock/reorder/dead stock at selected date; suppliers trailing 365 days; requisitions on selected date. These are ASSUMPTIONS, shown per metric. A failure makes only affected metrics unavailable; access-limited summaries never expose hidden raw modules.

Computed filtering and ordering happen after calculating the bounded candidate set. Do not page source items first and then filter by OrderNow or WatchList. Default stable ties use canonical identity. Define counts over full filtered scope, not page length.

A bulk envelope includes evaluated candidate count, successful matches to filters, failed candidates with unknown computed membership, page data/issues, completeness and nullable summary. Failed candidates cannot be silently excluded by a filter whose outcome cannot be calculated. UI explains “successful matching rows plus items that could not be evaluated”; authoritative totals remain absent. Model defines the exact count relationships.

## 13. Consistency, limits and diagnostics

Count/bound source candidate sets before loading expensive histories. Exceeding an agreed cap returns an explicit limit issue, never truncated stock history. Avoid N+1 queries and repeated stock calculations within a request. Share immutable in-request results by scope and date.

Optional normalization caches key by source text/content and normalization version, not item ID alone. No cross-request cached catalogue membership is authoritative without source versioning. Cache optimization must produce identical results when disabled. Defer it until measurement justifies it.

Diagnostics record module, safe error code, duration and counts. Do not log requisition text, credentials or full query bodies. Timeouts and cancellation propagate through Model. Numeric/date overflow and unavailable dependencies produce explicit outcomes, not false empty success.

## 14. Decisions and future tests

| Decision | Default/status | Required before authoritative integration |
|---|---|---|
| B01 | Rolling 365-day demand | Confirm history coverage and demand interpretation |
| B02 | Configured base lead time 5 for demo | Agree production basis and overrides |
| B03 | Item-specific selected-supplier delay | Confirm preferred-supplier/granularity policy |
| B04 | Tie -> clarification | Record accepted tie rule |
| B05 | Field-aware normalization and limited quantity grammar | Version grammar and golden normalization cases |
| B06 | FIFO dead-stock value | Confirm report valuation assumption |
| B07 | Recommendation precedence separate from raw reorder flag | Review no-demand/dead-stock/zero-order messaging |
| B08 | Strict M1 price integrity | Confirm treatment of consumed malformed receipt prices |

Proposed `test/Business` areas: Stock, Valuation, Suppliers, Reorder, Matching, Reporting and Dashboard. Test pure formulas separately from repository orchestration. Use fixed clocks and typed fixtures. Include conservation/property checks, deterministic ordering, exact threshold boundaries, window boundaries, unit safety, incomplete bulk counts and full-report completeness. No executable tests are created now.

Completion later requires source examples plus edge tests, no formulas in Presentation/Model adapters, no writes, agreement on source-gap decisions, typed UI mapping and evidence distinguishing isolated examples from integrated data. The [delivery plan](IWAS_BACKEND_IMPLEMENTATION_BLUEPRINT.md) sequences that future work.
