# CODEX MASTER PROMPT — IWAS

You are the primary implementation agent for the **Inventory and Warehouse Analytics System (IWAS)**.

Your job is to build the complete project from start to finish, following the supplied project specification and `IWAS_PROJECT_PLAN.md`.

Do not treat this as a prototype. Build a clean, working, testable semester-project implementation that can be demonstrated, printed, explained in a viva, and extended safely.

---

# 1. AUTHORITATIVE SOURCES

Before writing code, read these files completely:

1. `IWAS_PROJECT_PLAN.md`
2. `CSC470_Project09_Warehouse_Analytics.pdf`

Authority order:

1. **The PDF is the source of truth for functional requirements, formulas, thresholds, reports, and project constraints.**
2. **`IWAS_PROJECT_PLAN.md` is the implementation blueprint.**
3. **The project owner's explicit technology decisions override recommendations about technology.**

If the plan and PDF conflict regarding business behavior, follow the PDF.

If a requirement is not specified by the PDF, do not pretend that it is.

Use the classifications already established in the project plan:

- `ASSUMPTION`
- `RECOMMENDATION`
- `OPEN QUESTION`

Do not silently convert assumptions into requirements.

---

# 2. PROJECT OBJECTIVE

Build a web-based **Inventory and Warehouse Analytics System** that reads existing warehouse data and performs analytical computations.

IWAS must implement:

- M1 — Stock Valuation using FIFO
- M2 — Reorder and EOQ Analysis
- M3 — Supplier Performance
- M4 — AI Requisition-to-Catalogue Matching
- M5 — Reporting

The system reads existing data.

It must not become a general warehouse-management or ERP application.

---

# 3. FIXED TECHNOLOGY STACK

Use:

## Backend

- ASP.NET Core
- C#
- ASP.NET Core MVC
- ASP.NET Core Web API controllers where useful
- Entity Framework Core
- SQL Server
- built-in ASP.NET Core dependency injection
- built-in logging
- strongly typed Options/configuration

## Frontend

Use only:

- HTML
- CSS
- JavaScript
- Razor views
- Bootstrap 5

Do not introduce:

- React
- Angular
- Vue
- Blazor
- jQuery-heavy architecture
- Node.js frontend build systems unless absolutely required by an existing dependency

The browser UI should remain simple, conventional, maintainable, and easy to demonstrate.

---

# 4. REQUIRED THREE-LAYER ARCHITECTURE

The solution must use exactly these main application layers:

```text
IWAS.Presentation
IWAS.Business
IWAS.Models
```

Dependency direction:

```text
IWAS.Presentation
        |
        v
IWAS.Business
        |
        v
IWAS.Models
```

Allowed references:

```text
IWAS.Presentation -> IWAS.Business
IWAS.Presentation -> IWAS.Models
IWAS.Business     -> IWAS.Models
```

Forbidden:

```text
IWAS.Models -> IWAS.Business
IWAS.Models -> IWAS.Presentation

IWAS.Business -> IWAS.Presentation
```

Never create cyclic project references.

---

# 5. LAYER RESPONSIBILITIES

## IWAS.Presentation

Contains:

- ASP.NET Core startup/configuration
- Controllers
- API controllers
- Razor views
- ViewModels
- authentication
- authorization
- middleware
- frontend JavaScript
- frontend CSS
- Bootstrap
- PDF/report rendering adapter
- request/response handling

Presentation must not contain the warehouse formulas.

Controllers must remain thin.

A controller should generally:

1. validate/request-bind input;
2. call a Business service;
3. return a view, JSON response, or PDF.

Do not access EF Core DbContext directly from controllers.

---

## IWAS.Business

Contains:

- FIFO calculations
- EOQ calculations
- reorder-point calculations
- supplier-performance calculations
- dead-stock logic
- cosine similarity
- text normalization
- stop-word handling
- quantity parsing
- requisition matching
- report-data orchestration
- business validation
- analytical services

Business code must be independent of:

- HTTP
- Razor
- HTML
- Bootstrap
- JavaScript

Formula/calculation classes should be deterministic and easy to unit-test.

---

## IWAS.Models

Contains:

- Entity models
- Enums
- Result DTOs
- Value objects
- Options/configuration models
- EF Core DbContext
- EF Core mappings
- Repository interfaces
- Read-only repository implementations

Do not put business calculations into entities or the DbContext.

---

# 6. MOST IMPORTANT PROJECT CONSTRAINT

IWAS is a **read-only analytical system** with respect to operational warehouse data.

Do not create application functionality for:

- adding items;
- editing items;
- deleting items;
- adding stock receipts;
- editing receipts;
- deleting receipts;
- adding issues;
- editing issues;
- deleting issues;
- adding supplier-delivery records;
- editing supplier-delivery records;
- deleting supplier-delivery records;
- adding requisitions;
- editing requisitions;
- deleting requisitions.

There must be no operational-domain CRUD UI.

There must be no operational-domain mutation API such as:

```http
POST /api/items
PUT /api/items/{id}
DELETE /api/items/{id}
```

Do not implement those endpoints.

Protect this restriction at multiple levels.

### Application protection

The production DbContext must use:

```csharp
QueryTrackingBehavior.NoTracking
```

where appropriate.

Override:

```csharp
SaveChanges()
SaveChangesAsync()
```

to reject writes.

### Database protection

Production should connect using a database account with `SELECT` permission only.

The application restriction and database restriction must both exist.

---

# 7. INPUT DATASETS

The project must work from the four source datasets defined by the specification.

## Item Catalogue

Required fields:

```text
Item ID
Name
Unit
Category
Holding cost per unit per year
Ordering cost per purchase order
Description
```

The project plan identifies catalogue price as a source gap.

If a catalogue unit price exists in the real dataset, map it.

Otherwise keep:

```csharp
decimal? CatalogueUnitPrice
```

nullable.

Never silently substitute another price source unless explicitly authorized.

---

## Stock Movement

Fields:

```text
Movement ID
Item ID
Date
Type: Receipt or Issue
Quantity
Unit purchase price for receipts
```

---

## Supplier Delivery

Fields:

```text
Supplier ID
Supplier name
Item ID supplied
Promised delivery date
Actual delivery date
```

---

## Requisition

Fields:

```text
Requisition ID
Department
Date
Free-text description
```

---

# 8. DATABASE MODEL

Create entities approximately equivalent to:

```csharp
Item
StockMovement
SupplierDelivery
Requisition
```

Relationships:

```text
Item 1 ----- many StockMovement
Item 1 ----- many SupplierDelivery
```

Requisition-to-item matching is a **computed relationship**.

Do not persist a matching result back into the requisition source table.

Use `decimal` for:

- money;
- purchase price;
- holding cost;
- ordering cost;
- stock quantities where necessary.

Use `DateOnly` for business dates unless the real database requires another representation.

---

# 9. M1 — STOCK VALUATION USING FIFO

Implement correct FIFO layer computation.

For a selected item and date:

```text
Closing stock
= total receipt quantity
- total issue quantity
```

Issues consume the oldest receipt layers first.

Closing stock therefore consists of the remaining quantities from later receipt layers.

FIFO value:

```text
FIFO Value
= sum(
    remaining quantity in receipt layer
    × receipt unit price
  )
```

Use an actual FIFO queue/layer algorithm.

Do not use:

- weighted average;
- LIFO;
- latest-price multiplication;
- average purchase price.

Return:

```text
item
date
closing quantity
FIFO value
remaining receipt layers
```

Each layer should contain:

```text
receipt date
movement ID
remaining quantity
unit price
layer value
```

---

# 10. M1 MANDATORY ACCEPTANCE TEST

The following case must pass exactly:

```text
02/07 Receipt 100 @ 520
06/07 Receipt 80 @ 535
07/07 Issue   120
08/07 Receipt 60 @ 550
08/07 Issue    30
```

Expected:

```text
Total receipts = 240
Total issues = 150
Closing quantity = 90

FIFO layers:
30 @ 535
60 @ 550

FIFO value:

30 × 535 = 16,050
60 × 550 = 33,000

Total = BDT 49,050
```

Create a unit test for this immediately when M1 is implemented.

If an issue attempts to consume more stock than available receipts, do not calculate a fake negative FIFO result.

Throw a source-data-integrity/business exception.

---

# 11. M2 — REORDER AND EOQ

Implement the specification formulas.

## EOQ

```text
Q* = sqrt((2 × D × So) / H)
```

Where:

```text
D  = annual demand
So = ordering cost per purchase order
H  = holding cost per unit per year
```

## Average daily demand

```text
d̄ = D / 365
```

## Reorder point

```text
ROP = L × d̄ + 0.2 × L × d̄
```

Equivalent:

```text
ROP = normal lead-time demand + 20% safety stock
```

Do not hide the safety-stock calculation inside unexplained constants.

Calculate:

```text
LeadTimeDemand = L × d̄

SafetyStock =
0.20 × L × d̄

ROP =
LeadTimeDemand + SafetyStock
```

## Reorder rule

```text
ReorderNow when S <= ROP
```

where:

```text
S = current stock
```

## Dead-stock rule

```text
Dead stock when there has been no issue of the item
during the last 180 days.
```

---

# 12. ANNUAL DEMAND DEFAULT

The PDF defines `D` as annual demand but does not specify precisely how history should be selected.

Follow the project-plan assumption unless later replaced:

```text
D =
sum of ISSUE quantity
during the rolling 365-day period ending on analysisDate
```

Keep this behavior isolated so it can easily be changed later.

---

# 13. LEAD TIME

The source requires lead time `L` but does not provide a complete base-lead-time source in its dataset definition.

Follow the plan assumption.

Use:

```text
configured base lead time
+
supplier average late delay
=
effective lead time
```

Default demonstration base lead time:

```text
5 days
```

Do not hardcode 5 throughout the code.

Use configuration:

```json
"Reorder": {
  "DefaultBaseLeadTimeDays": 5
}
```

Support item-specific overrides through configuration if useful.

Clearly document this as an assumption.

---

# 14. M2 WORKED EXAMPLE TEST

For:

```text
D  = 3650
So = 800
H  = 40
L  = 5
S  = 90
```

Expected:

```text
EOQ ≈ 382

Average daily demand:
3650 / 365 = 10

ROP:
5 × 10
+
0.2 × 5 × 10

= 50 + 10
= 60
```

Therefore:

```text
90 > 60
ReorderNow = false
Days to ROP = 3
```

Create an automated test reproducing this example.

For the isolated test, supplier delay must be zero so the example remains identical to the specification.

---

# 15. DAYS TO REORDER POINT

Default calculation:

If:

```text
S <= ROP
```

then:

```text
DaysToROP = 0
```

Otherwise, if:

```text
average daily demand > 0
```

calculate:

```text
ceil((S - ROP) / averageDailyDemand)
```

If demand is zero:

```text
DaysToROP = null
```

---

# 16. M3 — SUPPLIER PERFORMANCE

For every supplier in the selected period calculate:

```text
total delivered orders
on-time orders
late orders
on-time percentage
average delay of late orders
standing
```

An order is on time when:

```text
ActualDeliveryDate <= PromisedDeliveryDate
```

On-time percentage:

```text
OnTimePercentage =
OnTimeOrders / TotalOrders × 100
```

Late delay:

```text
DelayDays =
ActualDeliveryDate - PromisedDeliveryDate
```

Average delay must include **late deliveries only**.

Do not include early or on-time deliveries as zero-delay entries in the average.

---

# 17. SUPPLIER STANDING

Required rule:

```text
below 80% on-time
=> Watch List
```

Use the project-plan classification for sample-report compatibility:

```text
100%        => Excellent
80%–99.99% => Good
<80%        => Watch List
```

Only the below-80% watch-list boundary is a strict formula requirement.

---

# 18. M3 ACCEPTANCE TEST

For:

```text
20 total delivered orders
17 on time
3 late

late delays:
2 days
4 days
6 days
```

Expected:

```text
On-time percentage = 85%

Average late delay =
(2 + 4 + 6) / 3
= 4 days

Standing = Good
```

Implement this as an automated test.

---

# 19. M3 -> M2 INTERACTION

Supplier performance is not isolated from reorder calculations.

The specification demonstrates supplier average delay being added to the lead time used by M2.

Use:

```text
EffectiveLeadTime =
BaseLeadTime
+
AverageSupplierLateDelay
```

Keep this orchestration in the Business layer.

Do not copy supplier calculations into `ReorderAnalysisService`.

Call/reuse the proper service/calculator.

---

# 20. M4 — AI REQUISITION MATCHING

Implement the required binary cosine similarity algorithm first.

Do not start with an external AI API or LLM.

Do not require:

- OpenAI API;
- embeddings API;
- cloud AI;
- Python ML service.

The source-required algorithm must work locally.

---

# 21. TEXT NORMALIZATION

For a requisition and every catalogue description:

1. lowercase;
2. normalize punctuation;
3. tokenize;
4. remove stop words;
5. normalize obvious simple plurals;
6. create a set of distinct terms.

The specification's example effectively normalizes:

```text
pens -> pen
```

Implement conservative simple singular normalization sufficient to reproduce the example.

Avoid complicated stemming algorithms unless later required.

---

# 22. BINARY COSINE SIMILARITY

Create binary term sets:

```text
A = requisition terms
C = catalogue-description terms
```

Calculate:

```text
similarity =
(A · C)
/
(||A|| × ||C||)
```

For binary term sets:

```text
A · C
=
number of common distinct terms
```

and:

```text
||A|| =
sqrt(number of distinct terms in A)

||C|| =
sqrt(number of distinct terms in C)
```

If either set is empty:

```text
similarity = 0
```

Never divide by zero.

---

# 23. MATCHING THRESHOLD

Required:

```text
similarity >= 0.80
=> automatic match
```

Otherwise:

```text
similarity < 0.80
=> clarification queue
```

Keep threshold configurable but default to:

```json
0.80
```

Do not casually change it.

---

# 24. MATCHING TIES

The specification does not define tied top scores.

Follow the project-plan safe default:

If multiple catalogue items have the same highest score and that score qualifies for automatic matching:

```text
send requisition to clarification
```

Do not select an item arbitrarily.

Mark this behavior clearly as an implementation assumption/recommendation.

---

# 25. M4 REQUIRED EXAMPLE

Catalogue item terms:

```text
{pen, gel, black, ink, office}
```

Requisition:

```text
"Black gel pens with smooth ink for the accounts office, one dozen."
```

Normalized matching terms should conceptually produce:

```text
{black, gel, pen, ink, smooth}
```

Common terms:

```text
{black, gel, pen, ink}
```

Therefore:

```text
similarity =
4 / (sqrt(5) × sqrt(5))

= 4 / 5

= 0.80
```

Expected:

```text
AutoMatched = true
```

Write an automated unit test that verifies this exact result.

---

# 26. QUANTITY PARSING

The specification demonstrates:

```text
"one dozen" => 12 units
```

Implement at minimum:

```text
"12"        => 12
"one"       => 1
"two"       => 2
"1 dozen"   => 12
"one dozen" => 12
"2 dozen"   => 24
"two dozen" => 24
```

Do not fabricate a quantity if text cannot be confidently parsed.

Use:

```text
ParsedQuantity = null
```

for uncertain cases.

---

# 27. REQUISITION PRICING

The specification requires matched requisitions to be priced from the catalogue.

However, its initial catalogue field list does not explicitly include catalogue unit price.

Use:

```csharp
decimal? CatalogueUnitPrice
```

as documented in `IWAS_PROJECT_PLAN.md`.

If the source database has a price column, map it.

If not:

```text
Price unavailable
```

must be displayed.

Never silently use:

- FIFO value per unit;
- last receipt price;
- average purchase price;

as catalogue price unless the project owner explicitly changes the rule.

---

# 28. STOCK AVAILABILITY FOR MATCHED REQUISITIONS

For an auto-matched requisition:

1. determine current/appropriate stock from stock movements;
2. show stock on hand;
3. if quantity was parsed, calculate:

```text
SufficientStock =
StockOnHand >= ParsedQuantity
```

Do not write/reserve/issue stock.

This is analysis only.

---

# 29. CLARIFICATION QUEUE

The clarification queue is a **computed view**.

A requisition belongs there when:

```text
best similarity < 80%
```

or when the safe tie rule applies.

Do not create a database table merely to persist clarification results unless the specification is formally changed.

No:

```text
Approve
Edit
Resolve
Update Requisition
```

workflow is required.

The page should simply identify items that require human clarification.

---

# 30. OPTIONAL TF-IDF

The specification permits TF-IDF as an optional upgrade.

Do not implement it until:

1. binary cosine is complete;
2. its tests pass;
3. M1–M5 core requirements work.

If implemented later, hide it behind an interface such as:

```csharp
ITextSimilarityCalculator
```

Binary cosine remains the default.

---

# 31. M5 — REPORTING

Implement five reports.

## R1

Daily Stock Movement Report.

Columns should include:

```text
Item
Unit
Opening
Received
Issued
Closing
```

Footer:

```text
number of receipt movements
number of issue movements
```

---

## R2

Stock Valuation Report — FIFO.

Include:

```text
Item
Closing quantity
FIFO layer quantities
Unit prices
FIFO value
```

Footer:

```text
Total warehouse value
```

Reuse M1.

Do not implement a second FIFO algorithm in reporting code.

---

## R3

Reorder List.

Include:

```text
Item
Stock
ROP
EOQ
Days to ROP
Action
```

Actions:

```text
S <= ROP
=> Order {EOQ} now

S > ROP and days available
=> Order in {days} days

otherwise
=> OK
```

Include count of items needing orders today.

The source mentions a purchase requisition draft.

Treat this as generated printable report output only.

Do not create/persist a purchase-order transaction.

---

## R4

Supplier Performance and Dead-Stock Report.

Supplier section:

```text
Supplier
Orders
On Time %
Average Delay
Standing
```

Dead-stock section:

```text
Item
Stock quantity
Stock value
Disposal recommendation
```

The dead-stock sample contains value but does not define its valuation method.

Follow the plan recommendation:

```text
use FIFO value from M1
```

and document it as an assumption.

---

## R5

AI Requisition Matching Log.

Include:

```text
Requisition ID
Department
Matched item / best candidate
Similarity
Status
```

Footer:

```text
total requisitions
auto-matched
clarification count
```

Reuse M4.

Do not duplicate matching code.

---

# 32. REPORT FORMAT

All five reports must support:

```text
A4 page size
company header
report title
report date/period
page number
BDT currency formatting
print
PDF download
```

Default company name can be configurable:

```text
Padma Trading Ltd
```

Do not permanently hardcode the company name into report logic.

Use configuration.

---

# 33. REPORT PDF ARCHITECTURE

Create:

```csharp
IReportDocumentRenderer
```

Business services prepare report data.

Presentation-side infrastructure renders PDF.

A concrete renderer may use an appropriate .NET PDF library.

Do not allow the whole application to depend directly on the PDF vendor package.

Example:

```text
IReportDocumentRenderer
        ^
        |
QuestPdfReportDocumentRenderer
```

If a different library is selected later, only the adapter should change.

---

# 34. REQUIRED FRONTEND PAGES

Build:

```text
Dashboard
Stock Valuation
Reorder & EOQ
Supplier Performance
Requisition Matching
Clarification Queue
Reports
Login
```

Navigation:

```text
IWAS
├── Dashboard
├── Stock Valuation
├── Reorder & EOQ
├── Supplier Performance
├── Requisition Matching
│   └── Clarification Queue
├── Reports
└── Sign Out
```

Do not include:

```text
Manage Inventory
Add Item
Edit Item
Delete Item
Create Requisition
Receive Stock
Dispatch Stock
Manage Suppliers
```

---

# 35. UI STYLE

Use Bootstrap 5.

Target:

- professional university-project appearance;
- clean table layout;
- clear computed KPI values;
- minimal visual clutter;
- responsive pages;
- readable print output;
- obvious badges for status.

Suggested badge concepts:

```text
Good
Excellent
Watch List
Reorder Now
Dead Stock
Auto Matched
Clarification Required
```

Do not spend excessive effort on animations or decorative dashboards.

Correctness is more important than visual effects.

---

# 36. JAVASCRIPT

Use page-specific ES modules.

Create approximately:

```text
wwwroot/js/api-client.js
wwwroot/js/stock-valuation.js
wwwroot/js/reorder.js
wwwroot/js/supplier-performance.js
wwwroot/js/requisition-matching.js
wwwroot/js/reports.js
```

`api-client.js` should centralize GET/fetch error handling.

Prefer:

```javascript
const
async/await
fetch
textContent
```

Avoid raw insertion of requisition/source text using unsafe `innerHTML`.

---

# 37. STATE MANAGEMENT

This is not a SPA.

Do not introduce Redux or another global state-management library.

Use:

- Razor server-rendered pages;
- query parameters;
- local JavaScript state;
- returned API data;
- authentication cookie.

Do not treat browser localStorage as authoritative business storage.

---

# 38. API RULES

Analytical operations should use GET endpoints.

Examples:

```http
GET /api/items
GET /api/items/{id}

GET /api/stock-valuation
GET /api/reorder
GET /api/reorder/{itemId}

GET /api/suppliers/performance

GET /api/requisitions/{id}/match
GET /api/requisitions/clarification
GET /api/requisitions/matches

GET /reports/r1
GET /reports/r2
GET /reports/r3
GET /reports/r4
GET /reports/r5
```

No domain mutation endpoints.

---

# 39. ERROR RESPONSES

Use ASP.NET Core `ProblemDetails`.

Map approximately:

```text
not found
=> 404

invalid analytical input
=> 400

source-data integrity problem
=> 422

calculation impossible because of source data
=> 422

unauthenticated
=> 401

not authorized
=> 403

unexpected server failure
=> 500
```

Never send development stack traces to end users in production.

---

# 40. ERROR HANDLING

Create specific Business exceptions such as:

```csharp
BusinessValidationException
DataIntegrityException
EntityNotFoundException
CalculationException
```

Handle them centrally through middleware.

Examples of source-data integrity problems:

```text
negative source quantity
receipt without required purchase price
issues greater than available receipts
invalid costs used in EOQ
```

Do not silently correct invalid source data.

---

# 41. LOGGING

Use structured:

```csharp
ILogger<T>
```

Log important analytical events.

Examples:

```text
M1 valuation completed
M2 reorder analysis completed
M3 supplier analysis completed
M4 matching completed
M5 report generated
source data integrity failure
```

Include useful dimensions such as:

```text
ItemId
SupplierId
RequisitionId
AnalysisDate
ReportType
CorrelationId
RecordCount
ElapsedMilliseconds
```

Do not log:

```text
passwords
cookies
connection strings
full requisition text at normal information level
```

---

# 42. AUTHENTICATION

Authentication is not a core PDF requirement.

Implement only the lightweight architecture documented in `IWAS_PROJECT_PLAN.md`.

Recommended:

```text
ASP.NET Core cookie authentication
configured users
no user-management CRUD UI
```

Passwords must be stored as secure hashes.

Never commit plaintext passwords.

If authentication creates major implementation conflict with the course environment, keep it isolated so it can be simplified without affecting Business logic.

---

# 43. ROLES

Unless later changed, support:

```text
WarehouseManager
Storekeeper
PurchaseOfficer
Viewer
```

All roles remain read-only.

No role receives source-data write permissions.

If the final project requires only one user, reduce this to:

```text
WarehouseManager
```

without changing the analytical architecture.

---

# 44. CONFIGURATION

Use strongly typed options.

Important defaults:

```json
{
  "IWAS": {
    "CompanyName": "Padma Trading Ltd",
    "CurrencyCode": "BDT",
    "BusinessTimeZone": "Asia/Dhaka"
  },
  "Matching": {
    "AutoMatchThreshold": 0.80,
    "TopCandidateCount": 3
  },
  "Reorder": {
    "SafetyStockRate": 0.20,
    "DeadStockDays": 180,
    "AnnualDemandDays": 365,
    "DefaultBaseLeadTimeDays": 5
  },
  "Supplier": {
    "WatchListThreshold": 0.80
  },
  "Reporting": {
    "PageSize": "A4",
    "PersistGeneratedReports": false
  }
}
```

Do not scatter these numbers as magic literals through application code.

---

# 45. SECURITY

Implement:

- HTTPS;
- HSTS outside development;
- secure cookie settings;
- anti-forgery protection where applicable;
- parameterized EF Core queries;
- Razor HTML encoding;
- input length validation;
- safe report filenames;
- no user-controlled file paths;
- production read-only DB permissions;
- no operational `SaveChanges`;
- no secrets in source control.

Never use `Html.Raw` on untrusted requisition/source strings.

---

# 46. DATABASE DEVELOPMENT ENVIRONMENT

The real system reads existing data.

For development/testing only, create a local schema and seed data.

Files:

```text
database/development-schema.sql
database/development-seed.sql
database/read-only-user.sql
```

Seed examples needed for:

- exact FIFO worked example;
- EOQ worked example;
- supplier performance worked example;
- requisition matching worked example;
- all report demonstrations;
- at least ten requisition matching demonstration records.

Do not expose CRUD pages just because development tables exist.

---

# 47. REPOSITORY RULE

Repositories must contain read operations only.

Allowed names:

```text
GetAllAsync
GetByIdAsync
SearchAsync
GetByDateAsync
GetForItemAsync
GetByPeriodAsync
```

Do not create:

```text
AddAsync
InsertAsync
UpdateAsync
DeleteAsync
SaveAsync
```

for operational source entities.

---

# 48. TESTING REQUIREMENT

Use xUnit.

Testing is not optional.

Create:

```text
IWAS.Business.Tests
IWAS.Integration.Tests
```

Prioritize Business-layer unit tests.

Every formula/example from the specification should become an automated test.

---

# 49. MANDATORY M1 TESTS

At minimum:

```text
PDF worked FIFO example
single receipt
partial layer consumption
multiple-layer consumption
zero closing stock
issue exceeding available stock
same-date deterministic behavior
```

---

# 50. MANDATORY M2 TESTS

At minimum:

```text
PDF EOQ example
PDF ROP example
S == ROP => reorder
S < ROP => reorder
S > ROP => not reorder now
days to ROP
zero demand
zero holding cost handling
180-day dead stock
supplier delay increases effective lead time
```

---

# 51. MANDATORY M3 TESTS

At minimum:

```text
17/20 => 85%
2,4,6 => 4-day average delay
79% => Watch List
80% => Good
100% => Excellent
no late orders => average delay null
```

---

# 52. MANDATORY M4 TESTS

At minimum:

```text
exact PDF 80% example
exactly 80% => auto-match
below 80% => clarification
stop-word removal
pens => pen
empty vectors
one dozen => 12
2 dozen => 24
tie case
missing catalogue price
```

---

# 53. INTEGRATION TESTS

Verify:

```text
API endpoints respond correctly
PDF endpoints return PDF content
authorization works
source DbContext rejects SaveChanges
no operational mutation routes exist
report service reuses Business services
startup configuration validates correctly
```

---

# 54. AT LEAST TEN REQUISITION DEMONSTRATIONS

The final project must include at least ten sample requisitions for demonstrating M4.

Create a meaningful test/demo set including:

- obvious high-confidence match;
- exactly 80% boundary case;
- high confidence >90%;
- low-confidence clarification;
- ambiguous/tie case;
- quantity with `one dozen`;
- numeric quantity;
- plural words;
- unrelated catalogue terms;
- missing/uncertain quantity.

Do not manually hardcode expected matches into production code.

They must come from the matching algorithm.

---

# 55. REQUIRED REPOSITORY STRUCTURE

Use the structure specified in `IWAS_PROJECT_PLAN.md`.

At minimum:

```text
src/
    IWAS.Models/
    IWAS.Business/
    IWAS.Presentation/

tests/
    IWAS.Business.Tests/
    IWAS.Integration.Tests/

database/
docs/
```

Within these projects, follow the file/folder design in the plan unless a small structural adjustment improves clarity without breaking three-layer architecture.

---

# 56. DIAGRAMS AND COURSE DOCUMENTATION

The project assessment also expects design artifacts.

Prepare Markdown/Mermaid source files for:

```text
Context Diagram
DFD Level 0
DFD Level 1
ER Diagram
Class Diagram
```

Store under:

```text
docs/diagrams/
```

Also prepare:

```text
docs/requirements-traceability.md
docs/api-contracts.md
docs/viva-test-cases.md
```

Do not generate diagrams that imply unsupported CRUD behavior.

---

# 57. DEVELOPMENT ORDER

Follow this sequence.

## Phase 0

Repository/solution scaffold.

Do not begin UI feature coding before the structure compiles.

---

## Phase 1

Models + database + repositories.

Goal:

```text
all four datasets can be read
```

and:

```text
no writes are possible
```

---

## Phase 2

M1 FIFO.

This is a critical project requirement.

Finish and test it before proceeding.

---

## Phase 3

M3 Supplier Performance.

Build M3 before final M2 because M3 delay affects M2 lead time.

---

## Phase 4

M2 Reorder & EOQ.

Integrate M1/current stock and M3 supplier-delay information appropriately.

---

## Phase 5

M4 AI Matching.

Start with required binary cosine only.

---

## Phase 6

M5 Reports.

Build:

```text
R1
R2
R3
R4
R5
```

Reuse existing Business services.

---

## Phase 7

Dashboard and UI polish.

Do not create new Business rules during this phase.

---

## Phase 8

Authentication/security hardening.

---

## Phase 9

Integration tests, documentation, diagrams, deployment readiness, and viva/demo assets.

---

# 58. HOW YOU SHOULD WORK

Do not attempt to generate the entire project as one uncontrolled code dump.

Implement incrementally.

For each phase:

1. inspect existing repository state;
2. identify dependencies;
3. implement the smallest coherent slice;
4. compile;
5. run tests;
6. fix failures;
7. commit logical code structure if version-control tooling is available;
8. continue to the next slice.

Do not leave the solution knowingly uncompilable between major phases.

---

# 59. BUILD GATES

Do not continue past a core calculation phase if its required worked-example tests fail.

Required gates:

```text
M1 worked example passes
before M1 is considered complete.

M3 worked example passes
before supplier logic is considered complete.

M2 worked example passes
before reorder logic is considered complete.

M4 80% worked example passes
before matching is considered complete.
```

---

# 60. CODE QUALITY

Prefer understandable software-engineering-lab code over excessive abstraction.

Use:

- dependency injection;
- interfaces at meaningful boundaries;
- cohesive services;
- pure calculators;
- repositories;
- options;
- DTOs/results;
- structured exceptions.

Avoid:

- giant God services;
- unnecessary generic repository frameworks;
- reflection-heavy abstractions;
- dynamic code;
- global service locators;
- static mutable state;
- formula logic in Razor/JavaScript;
- duplicated formulas.

---

# 61. C# CONVENTIONS

Use:

```text
nullable reference types
file-scoped namespaces
async/await for I/O
CancellationToken
decimal for money
DateOnly for business dates
PascalCase public members
camelCase locals
private readonly injected fields
```

Suffix asynchronous methods with:

```text
Async
```

Example:

```csharp
CalculateAsync(...)
GetByIdAsync(...)
GenerateR2Async(...)
```

---

# 62. BUSINESS SERVICE RULE

Each business service should orchestrate, not become a random utility collection.

Examples:

```text
StockValuationService
ReorderAnalysisService
SupplierPerformanceService
RequisitionMatchingService
ReportService
```

Separate reusable mathematical logic when appropriate:

```text
FifoCalculator
EoqCalculator
ReorderPointCalculator
SupplierMetricCalculator
CosineSimilarityCalculator
```

---

# 63. NO DUPLICATED BUSINESS RULES

There must be one authoritative implementation of each calculation.

Example:

```text
R2
-> StockValuationService
-> FifoCalculator
```

Never:

```text
R2
-> custom second FIFO implementation
```

Likewise:

```text
R4
-> SupplierPerformanceService
```

and:

```text
R5
-> RequisitionMatchingService
```

---

# 64. EMPTY AND ERROR STATES

Every frontend module must handle:

```text
no results
loading
invalid input
source-data error
server error
not found
```

Use Bootstrap alerts/spinners/table placeholders.

Do not leave the user with a blank screen.

---

# 65. PRINTING

PDF reports are the authoritative print form.

Also make browser pages reasonably printable where easy.

Use:

```text
wwwroot/css/print.css
```

to hide:

```text
navbar
buttons
sidebar
unnecessary controls
```

when printing regular pages.

---

# 66. PERFORMANCE

Use efficient read queries.

Prefer:

```text
AsNoTracking
database-side filtering
date filtering
item filtering
projection to needed columns
```

Indexes in development schema should include approximately:

```text
StockMovement(ItemId, Date)

SupplierDelivery(SupplierId, PromisedDeliveryDate)

SupplierDelivery(ItemId, PromisedDeliveryDate)

Requisition(Date)
```

Do not prematurely optimize at the cost of correctness.

---

# 67. M4 PERFORMANCE

Catalogue normalization may be cached because matching every requisition against every item repeatedly can duplicate work.

If caching normalized catalogue terms:

- keep the cache internal;
- assume the external source can change;
- use a reasonable expiry or application-lifetime refresh mechanism;
- never treat cached data as permanently authoritative.

Do not persist matching results just to improve performance.

---

# 68. DEPLOYMENT

Produce an application that can be deployed using:

```text
ASP.NET Core
+
SQL Server
+
HTTPS
```

Optional containerization is allowed.

Do not make Docker mandatory for normal development if it complicates the course environment.

Create appropriate:

```text
README
appsettings example
environment variable documentation
database read-only user instructions
```

---

# 69. README

By project completion, `README.md` should explain:

1. project overview;
2. architecture;
3. prerequisites;
4. configuration;
5. database setup;
6. running locally;
7. authentication;
8. running tests;
9. report generation;
10. source-data read-only constraint;
11. known assumptions/open questions;
12. module descriptions;
13. demo procedure.

---

# 70. IMPLEMENTATION ASSUMPTIONS

Keep a visible assumptions section in documentation.

Important assumptions currently include:

```text
web app chosen
SQL Server / EF Core
rolling 365-day annual demand
nullable catalogue price
configured base lead time
supplier delay added to lead time
multiple-supplier default selection rule
Asia/Dhaka business time zone
lightweight authentication
tie handling
dead-stock monetary valuation using FIFO
```

If real source information later resolves one of these, update:

- code;
- tests;
- documentation;
- plan notes.

---

# 71. DO NOT INVENT REQUIREMENTS

Do not add features such as:

```text
purchase-order workflow
supplier registration
item registration
stock receiving screen
dispatch screen
stock adjustment
approval workflow
email notification
SMS notification
machine learning API
barcode scanner
user administration panel
audit CRUD history
warehouse transfers
multi-warehouse management
payment processing
```

unless explicitly requested later.

---

# 72. SOURCE-GAP HANDLING

When you encounter missing details:

Do this:

```text
1. check PDF;
2. check IWAS_PROJECT_PLAN.md;
3. check existing repository/configuration;
4. use an already documented assumption if available;
5. isolate the assumption cleanly;
6. document it.
```

Do not block implementation for every minor ambiguity.

Do not invent hidden rules.

---

# 73. REQUIREMENTS TRACEABILITY

Maintain:

```text
docs/requirements-traceability.md
```

Map each source requirement to:

```text
Business service/calculator
API/controller
frontend page
report
unit/integration test
```

This is particularly important for:

```text
FIFO
EOQ
ROP
dead stock
supplier watch list
supplier delay integration
cosine matching
80% threshold
quantity parsing
R1-R5
read-only constraint
```

---

# 74. FINAL ACCEPTANCE CHECKLIST

Before declaring the project complete, verify all of the following.

## Architecture

- [ ] Three-layer architecture exists.
- [ ] Dependencies point in the allowed direction.
- [ ] Presentation does not implement formulas.
- [ ] Presentation does not directly query DbContext.

## Read-only constraint

- [ ] No source CRUD pages exist.
- [ ] No source mutation API exists.
- [ ] `SaveChanges` is blocked.
- [ ] Production DB account can only read.

## M1

- [ ] FIFO layer algorithm implemented.
- [ ] Closing stock correct.
- [ ] FIFO value correct.
- [ ] BDT 49,050 example passes.

## M2

- [ ] annual demand implemented.
- [ ] EOQ implemented.
- [ ] daily demand implemented.
- [ ] 20% safety stock implemented.
- [ ] ROP implemented.
- [ ] reorder-now implemented.
- [ ] days-to-ROP implemented.
- [ ] dead-stock rule implemented.
- [ ] supplier delay integrated.
- [ ] EOQ 382 / ROP 60 example passes.

## M3

- [ ] on-time percentage implemented.
- [ ] late-only average delay implemented.
- [ ] below 80% watch list implemented.
- [ ] 85% / 4-day example passes.

## M4

- [ ] preprocessing implemented.
- [ ] stop words implemented.
- [ ] simple plural normalization implemented.
- [ ] binary term vectors implemented.
- [ ] cosine similarity implemented.
- [ ] >=80% auto-match implemented.
- [ ] clarification queue implemented.
- [ ] quantity parsing implemented.
- [ ] one dozen -> 12.
- [ ] PDF example returns 80%.
- [ ] at least ten demonstration requisitions exist.

## M5

- [ ] R1 complete.
- [ ] R2 complete.
- [ ] R3 complete.
- [ ] R4 complete.
- [ ] R5 complete.
- [ ] reports are A4.
- [ ] company header appears.
- [ ] date/period appears.
- [ ] page number appears.
- [ ] PDF print/download works.

## Engineering

- [ ] unit tests pass.
- [ ] integration tests pass.
- [ ] no secrets committed.
- [ ] structured logging exists.
- [ ] ProblemDetails/error middleware exists.
- [ ] source-data validation exists.
- [ ] README complete.
- [ ] diagrams complete.
- [ ] requirements traceability complete.

---

# 75. FINAL INSTRUCTION

Start by reading:

```text
IWAS_PROJECT_PLAN.md
CSC470_Project09_Warehouse_Analytics.pdf
```

Then inspect the current repository.

If the repository is empty:

1. create `IWAS.sln`;
2. create `IWAS.Models`;
3. create `IWAS.Business`;
4. create `IWAS.Presentation`;
5. create test projects;
6. configure correct project references;
7. make the empty solution compile;
8. begin Phase 1.

If some implementation already exists:

1. inspect it;
2. compare it against the project plan;
3. preserve correct work;
4. refactor only where necessary;
5. do not blindly regenerate working files.

Work phase by phase.

Compile and test continuously.

Do not implement unsupported CRUD.

Do not change source formulas.

Do not hide assumptions.

Do not duplicate calculation logic.

Do not declare completion until all acceptance criteria pass.

The final product must be a focused, read-only, explainable **Inventory and Warehouse Analytics System** implementing M1–M5 correctly with ASP.NET Core, JavaScript, HTML, CSS, Bootstrap, and the required Presentation/Business/Models three-layer architecture.