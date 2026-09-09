# IWAS backend implementation handoff

Status: implemented demonstration integration, 9 September 2026.

The ASP.NET Core host now references Model and Business directly in the required dependency direction. All analytical routes resolve `IWarehouseAnalytics`; controller mapping formats typed values for Razor. `DisplayFixtures` is retained only as historical source material and is no longer read by the active analytical views or controller.

Implemented blueprint phases: BM-2, calculation portions of BM-4 through BM-7, BM-8 orchestration, BM-9 Presentation integration, and computed HTML/print previews for BM-10. The demonstration adapter is a bounded immutable extract with identity/source validation, query limits and read-only interfaces.

Active routes compute dashboard, FIFO valuation, reorder/dead stock, supplier performance, requisition matching/clarifications, and R1–R5 report tables. Reports use full Business results rather than paged screen data and carry snapshot/read-time metadata. Print remains browser-based; no claim is made that it is a dedicated PDF renderer.

Verification:

- Model, Business and Presentation projects build with zero warnings.
- Model acceptance suite: 8/8.
- Business acceptance suite: 6/6.
- Direct HTTP smoke verification: dashboard, five module routes and R1–R5 previews return HTTP 200 without unavailable-page output.
- Legacy Presentation accessibility, responsive, print, keyboard, safe-route and client-error checks continue to pass. Fixture-specific assertions are obsolete because preview scenarios and ten synthetic fixture outcomes were deliberately replaced by computed coherent data; update that suite as a separate UI-test baseline revision.

Production blockers remain S01–S08 from the Model plan: actual provider/schema, identity rules, movement chronology/opening basis, reliable history coverage, delivery granularity, price/unit source, consistent-read mechanism and production lead-time policy. Authentication/authorization and a formal server-side PDF engine also remain unimplemented. No production schema or credentials were invented.
