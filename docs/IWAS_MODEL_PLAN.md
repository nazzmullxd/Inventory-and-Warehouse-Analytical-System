# IWAS detailed Model layer plan

Status: documentation baseline, 9 September 2026. Defines future `src/Model` and `test/Model` work only. Read with the [Business plan](IWAS_BUSINESS_PLAN.md), [UI/UX plan](IWAS_UI_UX_PLAN.md) and [backend delivery plan](IWAS_BACKEND_IMPLEMENTATION_BLUEPRINT.md). No entities, migrations, database objects or adapters are implemented by this revision.

## 1. Purpose and architectural choice

Model supplies trustworthy source records and shared typed contracts to Business. Under this project's three-layer constraint, it contains immutable source/read models, query abstractions, typed analytical contracts, source mapping and an internal read-only persistence adapter. This combines domain contracts and data access in one project intentionally; internal namespaces separate responsibilities without introducing a fourth application layer.

Model does not calculate FIFO, EOQ, supplier standing, matching scores, parsed quantities or report totals. It validates source shape and retrieves data. Business validates analytical sufficiency and calculates results. Presentation formats outputs. The existing `WorkspaceModel`, `DataTable` and `MatchFixture` stay Presentation view models; they must not be moved here as domain entities.

SOURCE: four existing datasets and read-only warehouse behavior, assignment page 1. OWNER: singular `Model` directory, alongside Business and Presentation under `src`; root `test`. All schema names, lengths, precision choices and adapter decisions below are RECOMMENDATIONS unless marked ASSUMPTION or SOURCE.

## 2. Proposed organization

| Area beneath `src/Model` | Responsibility |
|---|---|
| `Source` | Item, stock movement, supplier delivery and requisition read records |
| `Contracts/Queries` | Typed source query scopes, identifiers and date ranges |
| `Contracts/Results` | Typed analytical values consumed by Presentation |
| `Contracts/Reports` | R1-R5 data shapes, no layout/PDF dependencies |
| `Contracts/Common` | Issues, availability, provenance, paging and read context |
| `Repositories` | Public read-only query interfaces |
| `Data/Mapping` | Internal source-column/type/value translations |
| `Data/Queries` | Internal persistence query implementations |
| `Data/Validation` | Duplicate, shape, identity and mapping diagnostics |
| `Data/Consistency` | Internal consistent-read coordination |

Proposed test areas under `test/Model`: Contracts, Mapping, Queries, ReadOnly, Consistency and SourceValidation. No files are scaffolded now. Shared result records carry data only; Business services, clocks and analytical strategies remain in Business.

## 3. Source catalogue and relationships

| Record | Identity | Relationship | Persistence status |
|---|---|---|---|
| Item | Item ID | Parent of movements and supplied-item delivery links | Existing source |
| Stock movement | Movement ID | Exactly one item | Existing source |
| Supplier delivery | Verified delivery/order identity | Supplier and item linkage | Existing source; granularity requires verification |
| Requisition | Requisition ID | No stored catalogue match | Existing source |
| Match/candidate | Request-scoped identity | Derived requisition-to-item association | Never persisted by default |
| FIFO layer | Receipt movement identity | Derived remaining portion | Never a new warehouse record |

Supplier identity can be projected from existing delivery data; do not invent a fifth operational supplier table merely to normalize the application. A future projection may separate distinct orders from item links when the real source is multi-line. That projection must preserve purchase-order counts and is not a new record-maintenance feature.

Requisition-to-item linkage is computed, not an ORM relationship to save. Report rows, dashboard counts, drafts and clarification statuses are derived contracts, never source entities.

## 4. Field dictionary

Shapes below describe proposed typed contracts, not SQL DDL. Actual source mappings must be verified. Retain raw source values for safe diagnosis without silently coercing invalid records into valid business data.

### MOD-01: Item

| Field | Proposed type/shape | Rules and meaning |
|---|---|---|
| ItemId | Unicode string, proposed max 100 | Required, unique canonical identity |
| Name | Unicode string, proposed max 200 | Required, preserve display text |
| Unit | Unicode string, proposed max 50 | Required base inventory unit |
| Category | Unicode string, proposed max 100 | Optional/empty permitted; display fallback is Presentation's job |
| HoldingCostPerUnitPerYear | Decimal, proposed 19 digits/4 fractional | SOURCE input; nonnegative source value; Business requires >0 for EOQ |
| OrderingCostPerPurchaseOrder | Decimal, proposed 19/4 | SOURCE input; nonnegative |
| Description | Unicode string, proposed max 2000 | SOURCE matching text; empty may be represented but cannot support a useful match |
| CatalogueUnitPrice | Nullable decimal, proposed 19/4 | ASSUMPTION extension; nonnegative when present |
| PriceProvenance | Optional typed metadata | Identify configured source and availability reason |

Catalogue price is required for the M4 task but absent from the PDF's listed catalogue fields. If unavailable, preserve null and reason. Never substitute receipt price or parse a currency string from UI fixtures. Do not add a price-edit screen or modify the warehouse schema without a separately agreed source change.

### MOD-02: Stock movement

| Field | Proposed type/shape | Rules and meaning |
|---|---|---|
| MovementId | Unicode string, proposed max 100 | Required unique identity |
| ItemId | Canonical item identity | Must resolve to source Item |
| MovementDate | Date-only | SOURCE business movement date |
| MovementSequence | Nullable integer or verified source sequence | ASSUMPTION mapping for same-day chronology |
| MovementType | Receipt or Issue | Explicit source-code mapping; reject unknown code |
| Quantity | Decimal, proposed 19/4 | Strictly positive, item base unit |
| UnitPurchasePrice | Nullable decimal, proposed 19/4 | Receipt price needed for valuation; issue price ignored with warning |

A negative quantity is not automatically converted to an issue. An issue with a price must not become a receipt. Absence of receipt price is represented for quantity consumers and marked as unavailable for valuation. Model reports the defect; Business decides its effect on the requested use case.

Chronology must be proven by source sequence/timestamp semantics. Alphabetical movement ID is not inherently chronological. If multiple records share a date with no trustworthy order, return explicit chronology uncertainty. Production stock analysis then blocks. An illustrative ID-order fallback, if ever used, is clearly demo-only.

### MOD-03: Supplier delivery

| Field | Proposed type/shape | Rules and meaning |
|---|---|---|
| DeliveryRecordKey | Unicode stable key, proposed max 200 | Adapter identity; not supplied explicitly by PDF |
| PurchaseOrderId | Optional until source discovery; required for multi-line deduplication | Distinct order identity where rows are not one order each |
| SupplierId | Unicode string, proposed max 100 | Required stable supplier identity |
| SupplierName | Unicode string, proposed max 200 | Required; conflicting names require reconciliation |
| ItemId / item links | Canonical item identity | Item relationship per verified source granularity |
| PromisedDeliveryDate | Date-only | Required |
| ActualDeliveryDate | Nullable date-only | Null means not delivered/open, not zero delay |

The assignment measures purchase orders. Before implementing M3, establish whether one source row means an order, an order line or a delivery event. A stable row key alone does not prove order granularity. If split deliveries or multiple promised dates exist for an order, completion-date and promise-date policy must be agreed; never count all lines as separate orders by default.

Canonical view options: verified one-order rows with an item association, or a distinct-order projection with separately deduplicated item links. In the latter case supplier-wide counts use each order once; item-specific selection uses distinct linked orders. Both projections read the same original dataset and source snapshot. Do not deduplicate by matching dates/name/quantity guesses.

### MOD-04: Requisition

| Field | Proposed type/shape | Rules and meaning |
|---|---|---|
| RequisitionId | Unicode string, proposed max 100 | Required unique identity |
| Department | Unicode string, proposed max 200 | Required source context |
| RequisitionDate | Date-only | Required |
| Description | Unicode string, proposed max 4000 | Preserve complete raw text; empty produces a Business clarification reason |

Do not store matched item, similarity, parsed quantity, approval state or resolution against this record. Normalized terms exist only in derived result/cache structures. Do not replace original Bengali/Unicode text with normalized matching text.

## 5. Identity, precision and validation

Proposed canonical ID policy: trim request-edge whitespace, compare using ordinal case-insensitive identity, preserve source display spelling, and reject collisions under that comparison. Verify source collation; case-sensitive source IDs may require revising this assumption rather than merging distinct records. Stable output order uses canonical identity followed by original ordinal spelling where needed.

Mapping must reject overflow, invalid dates, truncated text and unsupported precision. Proposed lengths/decimal shapes are initial limits, not permission to truncate existing data. A source exceeding them triggers a mapping decision. Currency is BDT for the baseline; there is no exchange-rate system.

Validation split:

| Layer | Examples |
|---|---|
| Model shape/mapping | Missing identity, duplicates, orphan reference, invalid type/date, source sequence ambiguity, inconsistent units |
| Business analysis | Incomplete history, FIFO underflow, invalid EOQ denominator, unsafe unit enrichment, report completeness |
| Presentation input/display | Required fields, date syntax, visible error summary, formatting and labels |

Duplicate keys and orphan links are not silently dropped. Dataset-wide mapping failures block affected reads; entity-specific defects can be returned as attributable issues for interactive batches. Invalid objects must not look like successful canonical records. Do not replace null with an empty string or zero to satisfy a nonnullable field.

## 6. Query abstractions, MOD-05

Repositories expose intent-specific read operations, not generic CRUD and not `IQueryable`/database handles. Async operations accept cancellation in future implementation. Return immutable/materialized records or controlled streams within the read-context lifetime.

| Read capability | Query scope | Needed by |
|---|---|---|
| Item lookup/detail | Exact ID or bounded identity/name search | UI lookup and all Business modules |
| Item stock dataset | Item and through date; complete trusted history | Shared stock, M1/M2/M4 |
| Warehouse stock dataset | Bounded item set, through date, coverage | Reports and bulk analysis |
| Issue history | Item set and inclusive window | Business demand/dead-stock calculation |
| Delivered order dataset | Actual-date period, supplier/item criteria | M3 and selected-supplier delay |
| Open delivery records | Promised-date period, null actual date | Informational excluded count |
| Requisition detail/list | Exact ID or inclusive period, department | Matching and clarification |
| Read-source metadata | Mapping version, coverage, consistency capability | All authoritative analyses |

Model may perform source-equivalent filtering and projection, but Business owns analytical aggregates and computed status decisions. Source-only lookup paging is safe; computed OrderNow/WatchList/Clarification paging must happen after Business evaluates the candidate set. Model must not discard needed history to satisfy a display page size.

Use bulk reads for warehouse analyses instead of one query per item. Query limits cover candidate rows, movements, delivered orders, requisitions and text sizes; exceeding them yields a named limit issue. Never return a silently truncated ledger. A future database adapter may push equivalent source aggregation down only through a reviewed contract with equality tests; that optimization is not the baseline.

## 7. Persistence and read-only guarantees, MOD-06

RECOMMENDATION: EF Core with SQL Server fits the prior design, but the actual production database remains an external input. ASP.NET Core is owner-selected; SQL Server/EF Core are not assignment requirements. Match future package/framework versions to the actual host after an implementation compatibility review; do not upgrade the current .NET 8 UI as part of documentation.

Prefer canonical read views over scattering legacy column names throughout Business. Suggested logical views are Items, StockMovements, SupplierDeliveries and Requisitions. DBA-created views or direct read mappings are alternatives; neither is created now. Model's internal adapter owns translation, while public contracts remain provider-independent.

Proposed safeguards:

- Return no-tracking immutable projections; keep context and concrete repositories internal.
- Expose no add/update/delete/save operations through public query abstractions.
- Reject all save overloads in a future read context as defense in depth; prohibit mutation SQL and schema creation paths.
- Use a dedicated least-privilege database account with only necessary reads. No production migrations, automatic database creation or seed routines.
- Keep development seed/setup capability separate from the runtime identity and outside Business use cases.

EF Core documents no-tracking queries for read-only retrieval. No-tracking alone is not a write permission boundary; the restrictions above are project controls in addition to it. [Microsoft tracking documentation](https://learn.microsoft.com/en-us/ef/core/querying/tracking).

Verify denied-write behavior only against a disposable test database. Production verification uses permission inspection and harmless reads; do not trial writes or rely on rollback to erase possible side effects. Connection secrets remain outside source contracts and results.

## 8. Consistent reads and provenance, MOD-07

An analytical operation may read items, movements and deliveries at different moments unless consistency is explicitly coordinated. All reads for one aggregate/report must represent the same source version. Model provides a read-context abstraction; Business chooses its use-case boundary. Nested M1/M2/M3 calls join the existing context.

SQL Server snapshot transactions are a proposed implementation when enabled and verified. A transactionally consistent reporting replica or versioned immutable extract can also satisfy the contract. A timestamp label alone and a sequence of ordinary reads do not prove consistency. If a provider cannot guarantee the necessary consistency, do not mark combined financial results authoritative. EF transaction support depends on the provider and must be tested. [Microsoft transactions documentation](https://learn.microsoft.com/en-us/ef/core/saving/transactions).

Materialize immutable results, end the read context, then render HTML/PDF. Do not hold a transaction across user navigation. Do not run overlapping operations on a shared EF context; sequence database reads or use a separately designed safe orchestration.

Read metadata includes source/mapping version, read timestamp, actual snapshot identifier if available, consistency mode, reliable history start and known opening-stock basis. Do not invent an ID suggesting replayable historical data if the source only supports request-local consistency.

Across separate preview/PDF requests, either use a supported immutable source version or recalculate with new generation metadata. The project currently does not persist generated report data by default; therefore byte-identical regeneration later cannot be promised.

## 9. Shared request and result contracts, MOD-08

Contracts contain typed raw values, stable codes and context; no HTML, BDT-prefixed numbers, localized percentages, UI query-string state or HTTP status. Numeric nulls have typed availability reasons. Collections are read-only to consumers. Exact class signatures are deferred; this is a field-level contract specification.

### 9.1 Common envelope

| Field/group | Meaning |
|---|---|
| Parameters | Validated IDs, date/period, filters and scope |
| GeneratedAtUtc / BusinessTimeZone | Retrieval/generation metadata distinct from analysis date |
| ReadContext | Source version/consistency/coverage metadata |
| Data | Typed successful value or rows |
| Issues | Stable code, severity, affected field/entity, retryability |
| IsComplete | Required analytical scope is complete; optional enrichment status remains separate |
| Warnings | Accepted assumption/provenance notes |

For lists, define EvaluatedCount as source candidates evaluated. SuccessfulCount counts successful candidates passing computed filters. FailedCount counts failures whose computed membership is unknown. TotalCount = SuccessfulCount + FailedCount. Successful candidates not passing filters are excluded from TotalCount but remain part of EvaluatedCount. Page over a stable union of successful rows and failures; ReturnedCount = page data count + page issue count. ReturnedCount <= PageSize. Completeness is false when FailedCount > 0. Summary is absent then, unless a separately named partial diagnostic is explicitly requested.

Issue ordering is deterministic; failed computed keys sort after known keys, then by identity. UI must describe unknown membership and not assert that every failed row satisfied its status filter. Formal reports do not iterate paged UI results; they request full declared scope with completeness checks.

### 9.2 Module results

| Result | Required typed fields |
|---|---|
| StockPosition | ItemId, as-of date, coverage, received/issued quantities, closing quantity, remaining layers |
| FifoLayer | Receipt/movement ID, date/sequence, remaining quantity, nullable source price; valued result adds layer value |
| StockValuation | Item identity/name/unit, date, quantities, FIFO value, valued layers, warnings |
| SupplierPerformance | Supplier identity, period, delivered/on-time/late counts, ratio, nullable average late delay, standing, excluded open count |
| SupplierDelay | Item, supplier, selection window/reason, distinct order count, delay contribution and source/sample status |
| ReorderAnalysis | Item, date/windows, demand/costs, stock, raw/suggested EOQ, base/delay/effective lead time, safety, ROP, days, literal flag, recommendation, dead-stock/disposal flags and reasons |
| MatchCandidate | Item identity, common/request/item term counts, similarity, rank; no persisted association |
| RequisitionMatch | Requisition text/metadata, stock date, semantic status/reason, matched identity or null, candidates, quantity/unit parse status, price/total, stock/sufficiency and availability reasons |
| DashboardMetric | Metric code, typed value/unit, exact period, permitted destination, availability/completeness |

QuantityStatus, UnitCompatibility, PricingStatus and StockStatus are independent from MatchStatus. A match with unavailable price is not clarification merely because it cannot be priced. Candidate information is not an accepted match.

### 9.3 Report contracts

All report models carry report ID/title key, exact scope, generation/read context, assumptions and completeness. Company branding can be attached at the Presentation composition boundary; it is not a warehouse entity.

| Contract | Content |
|---|---|
| R1Data | Item/day opening, received, issued, closing and movement counts |
| R2Data | Typed item valuations/layers, warehouse total when complete |
| R3Data | Reorder rows, actionable count, typed draft lines |
| R4Data | Supplier section/period plus dead-stock quantities/FIFO values at separate date |
| R5Data | Requisition match rows and total/matched/clarification counts |

No PDF bytes, page coordinates, CSS classes or print layout belong in Model. Do not persist R5 as a log table simply because its report title says Log.

## 10. Issue vocabulary and null semantics

| Code | Meaning | Typical owner/consumer |
|---|---|---|
| validation_failed | Invalid request shape/value | Business/web mapping |
| entity_not_found | Requested source identity absent | Query/Business |
| source_schema_incompatible | Required mapping unavailable | Model |
| duplicate_source_key | Canonical identity collision | Model |
| orphan_item_reference | Movement/delivery item absent | Model |
| ambiguous_movement_order | Chronology not proven | Model -> stock consumer |
| delivery_identity_ambiguous | Purchase-order granularity not proven | Model -> M3 |
| stock_history_insufficient | Invalid opening/prefix stock evidence | Business |
| insufficient_demand_history | Required window not reliably covered | Business |
| receipt_price_missing | Required valuation price absent | Business |
| holding_cost_invalid | EOQ denominator invalid | Business |
| catalogue_price_unavailable | Optional pricing source absent | Model metadata -> M4 |
| quantity_unit_unverified | Parsed amount cannot safely be treated as base units | Business |
| analysis_input_too_large | Explicit input cap exceeded | Model/Business |
| dependency_unavailable | Source cannot be read | Model |
| numeric_overflow | Computation/representation exceeds limits | Model/Business |

This is an initial registry, not a hardcoded exception implementation. New codes require contract documentation, safe UI copy and tests. Do not make source-data errors resemble successful empty queries. Never serialize a stack trace or raw source row as error detail.

## 11. Source discovery and readiness checklist

| Gate | Evidence needed | Unresolved behavior |
|---|---|---|
| S01 | Actual provider, schema/column map, encoding, precision | Adapter remains unimplemented/unready |
| S02 | Identity/collation uniqueness and item references | Affected source read fails explicitly |
| S03 | Trusted movement sequence and opening layers | Stock-dependent results unavailable |
| S04 | Reliable history coverage for stock, 365-day demand and 180-day issues | Do not infer zero from missing history |
| S05 | Distinct-order identity and split-delivery semantics | M3/item delay and affected R4 cannot be authoritative |
| S06 | Catalogue-price availability and unit basis | Match possible; pricing unavailable |
| S07 | Consistent read mechanism and permission evidence | No authoritative combined report |
| S08 | Base lead-time source/configuration accepted | M2 demo assumption labeled; production decision outstanding |

None of these facts is invented from the current UI fixtures or IDE's JSON-server extension label. No repository data source for production has been established by that label. A future JSON fixture adapter may help tests, but it is not a selected production backend.

## 12. Test scenarios and acceptance

| Test ID | Future verification |
|---|---|
| MOD-T01 | Exact mapping of all four datasets, including Unicode and decimal precision |
| MOD-T02 | Duplicate/case-colliding IDs and orphan references are detected |
| MOD-T03 | Null actual delivery preserved; no sample fabricated |
| MOD-T04 | Multiple delivery lines do not inflate distinct-order count after agreed mapping |
| MOD-T05 | Movement ordering metadata and missing chronology preserved accurately |
| MOD-T06 | History queries include required opening data and inclusive boundaries |
| MOD-T07 | Missing price stays null; no UI-string parsing or receipt-price substitution |
| MOD-T08 | Bounded bulk queries fail explicitly rather than truncate |
| MOD-T09 | Read-only interfaces/context and restricted disposable database deny writes |
| MOD-T10 | Concurrent source changes cannot produce mixed-version authoritative results |
| MOD-T11 | Cancellation/timeout closes resources and returns safe failure |
| MOD-T12 | Typed contracts round-trip values, reasons and full-scope count relationships |

Use small deterministic query fixtures and a disposable instance of the chosen real provider for integration semantics. An in-memory fake alone cannot prove SQL collation, isolation, permissions or precision. Source validation tests belong here; formula tests belong in `test/Business`; rendered values/accessibility belong in `test/Presentation`.

Later completion requires verified mappings, pure data contracts, query-only access, read-context evidence, no model/business dependency cycle, accepted source assumptions and passing relevant tests. Current completion is this documentation only.
