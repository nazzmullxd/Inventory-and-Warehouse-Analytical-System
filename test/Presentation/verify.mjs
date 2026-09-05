import { chromium } from "playwright";
import AxeBuilder from "@axe-core/playwright";
import { mkdir, writeFile } from "node:fs/promises";
import assert from "node:assert/strict";

const base = process.env.IWAS_URL || "http://127.0.0.1:5080";
await mkdir("artifacts", { recursive: true });
const browser = await chromium.launch({ headless: true });
const context = await browser.newContext({
  viewport: { width: 1440, height: 900 },
});
const page = await context.newPage();
const failures = [];
const checks = [];
page.on("pageerror", (error) => failures.push(error.message));
const visit = async (path) => {
  const response = await page.goto(base + path);
  assert.equal(response.status(), 200, path);
};
const check = async (name, run) => {
  try {
    await run();
    checks.push({ name, passed: true });
    console.log(`PASS ${name}`);
  } catch (error) {
    checks.push({ name, passed: false, error: error.message });
    console.error(`FAIL ${name}: ${error.message}`);
  }
};
const routes = [
  "/",
  "/stock-valuation?apply=true",
  "/reorder",
  "/suppliers/performance",
  "/requisitions/matching?apply=true",
  "/requisitions/clarifications",
  "/reports",
  ...["R1", "R2", "R3", "R4", "R5"].map((r) => `/reports/${r}/preview`),
];

await check(
  "All analytical pages and five report previews render accessibly",
  async () => {
    for (const route of routes) {
      await visit(route);
      assert.equal(await page.locator("h1").count(), 1);
      assert.equal(
        (await page.locator("[aria-current=page]").count()) >= 1,
        true,
      );
      const result = await new AxeBuilder({ page })
        .withTags(["wcag2a", "wcag2aa", "wcag21aa", "wcag22aa"])
        .analyze();
      assert.deepEqual(
        result.violations.map(
          (v) =>
            `${v.id}: ${v.nodes.map((n) => n.target.join(" ")).join(", ")}`,
        ),
        [],
        route,
      );
    }
  },
);
await check(
  "Supplied valuation, zero, partial and unavailable stay distinct",
  async () => {
    await visit("/stock-valuation?apply=true");
    assert.match(
      await page.locator("[data-results]").innerText(),
      /49,050\.00/,
    );
    assert.match(await page.locator("tbody").innerText(), /30 reams/);
    await visit("/stock-valuation?apply=true&scenario=empty");
    assert.match(
      await page.locator("[data-results]").innerText(),
      /No remaining FIFO layers/,
    );
    assert.match(await page.locator("[data-results]").innerText(), /0\.00/);
    await visit("/stock-valuation?apply=true&scenario=partial");
    assert.match(
      await page.locator("[data-results]").innerText(),
      /Unavailable/,
    );
    assert.equal(
      await page.locator('[data-results] a[href*="preview"]').count(),
      0,
    );
    await visit("/stock-valuation?apply=true&item=UNKNOWN");
    assert.equal(await page.locator("[data-results]").count(), 0);
    assert.match(await page.locator("main").innerText(), /Results unavailable/);
  },
);
await check(
  "Changed filters hide old evidence and Back restores the scope",
  async () => {
    await visit("/stock-valuation?apply=true");
    await page.getByLabel("As-of date").fill("2026-07-09");
    assert.equal(await page.locator("[data-results]").isVisible(), false);
    await page.getByRole("button", { name: "Compute valuation" }).click();
  await page.waitForURL(url => url.searchParams.get("date") === "2026-07-09");
    assert.match(await page.locator("main").innerText(), /Results unavailable/);
    await page.goBack();
    assert.equal(
      await page.getByLabel("As-of date").inputValue(),
      "2026-07-08",
    );
    assert.equal(await page.locator("[data-results]").isVisible(), true);
  },
);
await check(
  "Validation retains input and directs focus to the error summary",
  async () => {
    await visit("/suppliers/performance");
    await page.getByLabel("Period start").fill("2026-08-01");
    await page.getByRole("button", { name: "Analyze suppliers" }).click();
    assert.equal(
      await page
        .locator("#client-errors")
        .evaluate((e) => e === document.activeElement),
      true,
    );
    assert.equal(
      await page.getByLabel("Period start").inputValue(),
      "2026-08-01",
    );
    assert.equal(await page.locator("[aria-invalid=true]").count(), 1);
    assert.equal(await page.locator("[data-results]").isVisible(), false);
  },
);
await check(
  "All ten requisition scenarios preserve their supplied distinctions",
  async () => {
    const outcomes = [
      "BDT 180.00",
      "92%",
      "51%",
      "Tied candidates",
      "বিশেষ যন্ত্রের",
      "Source text is empty",
      "Quantity not identified",
      "Price unavailable",
      "0 units",
      "Insufficient stock",
    ];
    for (let i = 0; i < outcomes.length; i++) {
      await visit(
        `/requisitions/matching?requisition=RQ-${String(871 + i).padStart(4, "0")}&apply=true`,
      );
      assert.ok(
        (await page.locator("[data-results]").innerText()).includes(
          outcomes[i],
        ),
        outcomes[i],
      );
    }
  },
);
await check("Clarification inspect and return preserve filters", async () => {
  await visit("/requisitions/clarifications?filter=tie");
  await page.getByRole("link", { name: "Inspect RQ-0874" }).click();
  await page.waitForURL("**/requisitions/matching?**");
  assert.match(await page.locator("main").innerText(), /Tied candidates/);
  await page
    .getByRole("link", { name: "Back to clarification results" })
    .click();
  await page.waitForURL("**/requisitions/clarifications?filter=tie");
  assert.equal(await page.getByLabel("Reason").inputValue(), "tie");
});
await check(
  "Report scope, draft and required sections are explicit",
  async () => {
    await visit("/reports/R2/preview");
    assert.match(
      await page.locator(".report-paper").innerText(),
      /complete illustrative warehouse/,
    );
    assert.match(
      await page.locator(".report-paper").innerText(),
      /218,200\.00/,
    );
    await visit("/reports/R3/preview");
    assert.match(
      await page.locator(".report-paper").innerText(),
      /No order has been placed/,
    );
    await visit("/reports/R4/preview");
    assert.match(
      await page.locator(".report-paper").innerText(),
      /Dead stock and disposal/,
    );
    assert.match(
      await page.locator(".report-paper").innerText(),
      /FIFO valuation basis is an assumption/,
    );
    await visit("/reports/R5/preview");
    assert.match(
      await page.locator(".report-paper").innerText(),
      /Auto-matched: 2/,
    );
    await visit('/reports/R4/preview?scenario=empty');
    assert.match(await page.locator('.report-paper').innerText(), /No delivered orders/);
    assert.match(await page.locator('.report-paper').innerText(), /No dead-stock items/);
    await visit('/reports/R2/preview?scenario=partial');
    assert.equal(await page.locator('.report-paper').count(), 0);
    assert.equal(await page.getByRole('button', { name: 'Print sample' }).count(), 0);
  },
);
await check(
  "Viewport matrix has no page overflow; tables scroll independently",
  async () => {
    for (const [width, height] of [
      [1440, 900],
      [1024, 768],
      [768, 1024],
      [390, 844],
      [320, 800],
    ]) {
      await page.setViewportSize({ width, height });
      for (const route of routes) {
        await visit(route);
        assert.ok(
          await page.evaluate(
            () => document.documentElement.scrollWidth <= innerWidth + 1,
          ),
          `${width}px ${route}`,
        );
        if (width === 1440 || width === 320) await page.screenshot({ path: `artifacts/page-${route.replace(/[^a-z0-9]/gi, '_') || 'dashboard'}-${width}.png`, fullPage: true });
      }
      await visit("/");
      await page.screenshot({
        path: `artifacts/dashboard-${width}.png`,
        fullPage: true,
      });
    }
  },
);
await check("Mobile menu and skip link work by keyboard", async () => {
  await page.setViewportSize({ width: 390, height: 844 });
  await visit("/");
  assert.equal(await page.locator("#primary-nav").isVisible(), false);
  await page.keyboard.press("Tab");
  assert.equal(
    await page
      .locator(".skip-link")
      .evaluate((e) => e === document.activeElement),
    true,
  );
  await page.keyboard.press("Enter");
  assert.equal(
    await page.locator("#main").evaluate((e) => e === document.activeElement),
    true,
  );
  const menu = page.getByRole("button", { name: "Menu" });
  await menu.focus();
  await page.keyboard.press("Enter");
  assert.equal(await menu.getAttribute("aria-expanded"), "true");
  assert.equal(await page.locator("#primary-nav").isVisible(), true);
});
await check('200 percent text enlargement retains reachable navigation and page reflow', async () => {
  await page.setViewportSize({ width: 1440, height: 900 });
  for (const route of ['/', '/reorder', '/reports', '/requisitions/matching?apply=true']) {
    await visit(route);
    await page.evaluate(() => document.documentElement.style.fontSize = '200%');
    assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), route);
    assert.equal(await page.locator('.sidebar').evaluate(e => getComputedStyle(e).overflowY), 'auto');
    await page.getByRole('link', { name: 'Reports', exact: true }).focus();
    assert.ok(await page.getByRole('link', { name: 'Reports', exact: true }).evaluate(e => e.getBoundingClientRect().bottom <= innerHeight));
  }
});
await check("Five A4 print samples hide workspace chrome", async () => {
  await page.setViewportSize({ width: 1440, height: 900 });
  for (const id of ["R1", "R2", "R3", "R4", "R5"]) {
    await visit(`/reports/${id}/preview`);
    await page.emulateMedia({ media: "print" });
    assert.equal(await page.locator(".sidebar").isVisible(), false);
    assert.equal(await page.locator(".report-paper").isVisible(), true);
    await page.pdf({
      path: `artifacts/${id}.pdf`,
      format: "A4",
      preferCSSPageSize: true,
      printBackground: true,
      displayHeaderFooter: true,
      headerTemplate: "<span></span>",
      footerTemplate:
        '<div style="width:100%;text-align:center;font-size:9px">Illustrative sample · Page <span class="pageNumber"></span> of <span class="totalPages"></span></div>',
    });
    await page.emulateMedia({ media: "screen" });
  }
});
await check('Long Unicode report content paginates without hiding evidence', async () => {
  await visit('/reports/R2/preview');
  await page.emulateMedia({ media: 'print' });
  await page.locator('.report-paper').evaluate(report => {
    const body = report.querySelector('tbody');
    const template = body.rows[0].cloneNode(true); body.replaceChildren();
    for (let i = 0; i < 60; i++) {
      const row = template.cloneNode(true);
      row.cells[0].textContent = `Layout example ${i + 1} · অফিসের জন্য কাগজ · Long warehouse catalogue item description`;
      row.cells[4].textContent = '999,999,999.99';
      body.append(row);
    }
    report.querySelector('tfoot td').textContent = 'Layout stress fixture only · no authoritative totals';
  });
  await page.pdf({ path: 'artifacts/R2-multipage.pdf', format: 'A4', preferCSSPageSize: true, printBackground: true });
  assert.equal(await page.locator('.report-paper tbody tr').count(), 60);
  await page.emulateMedia({ media: 'screen' });
});
await check("Server-rendered journeys work without JavaScript", async () => {
  const noJs = await browser.newContext({ javaScriptEnabled: false });
  const p = await noJs.newPage();
  await p.goto(base + "/stock-valuation");
  await p.getByRole("button", { name: "Compute valuation" }).click();
  assert.match(await p.locator("main").innerText(), /49,050\.00/);
  await p.getByRole("link", { name: "Warehouse valuation report" }).click();
  assert.match(await p.locator(".report-paper").innerText(), /218,200\.00/);
  await noJs.close();
});
await check("Unknown routes and forbidden views are safe", async () => {
  assert.equal((await page.goto(base + "/unknown")).status(), 404);
  assert.match(
    await page.locator("main").innerText(),
    /couldn’t find that page/,
  );
  assert.equal((await page.goto(base + "/errors/forbidden")).status(), 403);
  assert.equal(await page.locator("[data-results]").count(), 0);
  await visit("/requisitions/matching?returnUrl=https://example.com");
  assert.equal(
    await page
      .getByRole("link", { name: "Back to clarification results" })
      .getAttribute("href"),
    "/requisitions/clarifications",
  );
});
await check("No client-side exceptions", async () =>
  assert.deepEqual(failures, []),
);
await writeFile(
  "artifacts/results.json",
  JSON.stringify({ browser: await browser.version(), checks }, null, 2),
);
await browser.close();
if (checks.some((x) => !x.passed)) process.exitCode = 1;
