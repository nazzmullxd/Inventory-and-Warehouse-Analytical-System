# UI refresh — 10 September 2026

The presentation layer now uses a navy sidebar, blue actions and pale blue surfaces. IBM Plex Sans remains local; body text is 15px, supporting labels are 12–13px, and inputs use 16px text with 46px minimum heights. Warning and success colors retain their separate meanings.

Shared styles own colors, typography, controls and tables without competing overrides. The local SVG sprite and `_Icon` partial replace CSS placeholder icons; no icon library, web font request or frontend runtime dependency was added. Edited Razor views are formatted for maintenance.

Tables have visible titles, record counts, aligned numeric columns, subtle alternate rows and status labels. Horizontal scrolling stays within the table on mobile and supports keyboard navigation. Scrolling guidance appears only when needed. Report selection uses the intended two-column layout on desktop and stacks on mobile. Print styles retain all report columns and hide navigation.

Controllers, Business calculations, database access, routes, form fields and existing filter behavior are unchanged. Badge classification is presentation-only and now handles computed order recommendations, instead of matching a single sample quantity.

## Verification

- Presentation build: no warnings or errors.
- Automated axe WCAG AA checks: all 12 routes at desktop and mobile widths, with no violations detected.
- Layout checks at 320, 390, 768, 1024 and 1440px; mobile menu and keyboard table scrolling verified.
- Table values and form contracts compared with the preceding application across all 12 routes: unchanged.
- Valuation, invalid input focus, stale-result hiding, and operation without JavaScript verified.
- R1–R5 print layout checked and PDF samples generated; no browser script errors.
- Dashboard, valuation, supplier, report selector and mobile screenshots visually reviewed.

Run the focused UI checks with the application running:

```powershell
cd test/Presentation
npm ci
npm run test:ui
```

The runner uses installed Microsoft Edge by default. `IWAS_BROWSER` selects another installed Playwright browser channel; `IWAS_URL` overrides the application URL. Set `IWAS_BASELINE_URL` only when a previous application build is available for comparison. Screenshots, PDF samples and results are written to ignored `test/Presentation/artifacts/ui/`.

The historical `npm test` suite retains fixture-specific expectations that predate the database integration. `test:ui` validates the current computed application. Automated accessibility checks complement the keyboard and visual checks; they are not a complete accessibility audit.
