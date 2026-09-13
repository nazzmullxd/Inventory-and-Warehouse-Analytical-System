import { chromium } from "playwright";
import AxeBuilder from "@axe-core/playwright";
import assert from "node:assert/strict";
import { mkdir, writeFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";

const base = process.env.IWAS_URL || "http://127.0.0.1:5080";
const baseline = process.env.IWAS_BASELINE_URL;
const artifacts = new URL("./artifacts/ui/", import.meta.url);
await mkdir(artifacts, { recursive: true });
const browser = await chromium.launch({ channel: process.env.IWAS_BROWSER || "msedge" });
const context = await browser.newContext();
const page = await context.newPage();
const errors = [], checks = [];
page.on("pageerror", error => errors.push(error.message));
const routes = ["/", "/stock-valuation?apply=true", "/reorder", "/suppliers/performance",
  "/requisitions/matching?apply=true", "/requisitions/clarifications", "/reports",
  ...["R1", "R2", "R3", "R4", "R5"].map(id => `/reports/${id}/preview`)];
const visit = async (route, origin = base) => {
  assert.equal((await page.goto(origin + route)).status(), 200, route);
  await page.evaluate(() => document.fonts.ready);
};
const check = async (name, run) => {
  try { await run(); checks.push({ name, passed: true }); console.log(`PASS ${name}`); }
  catch (error) { checks.push({ name, passed: false, error: error.message }); console.error(`FAIL ${name}: ${error.message}`); }
};
try {
  await check("Desktop and mobile pages meet WCAG AA checks without page overflow", async () => {
    for (const width of [1440, 390]) {
      await page.setViewportSize({ width, height: 960 });
      for (const route of routes) {
        await visit(route);
        const { violations } = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa", "wcag22aa"]).analyze();
        assert.deepEqual(violations.map(v => `${v.id}: ${v.nodes.map(n => n.target.join(" ")).join(", ")}`), [], `${width}px ${route}`);
        assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), `${width}px ${route} overflows`);
        assert.equal(await page.locator("h1").count(), 1);
      }
    }
  });
  await check("Small screens retain navigation and keyboard-scrollable tables", async () => {
    for (const width of [320, 768, 1024]) {
      await page.setViewportSize({ width, height: 900 });
      for (const route of ["/", "/reorder", "/reports", "/reports/R4/preview"]) {
        await visit(route);
        assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), `${width}px ${route}`);
      }
    }
    await page.setViewportSize({ width: 390, height: 844 });
    await visit("/reorder");
    const toggle = page.getByRole("button", { name: "Menu" });
    await toggle.click();
    assert.equal(await toggle.getAttribute("aria-expanded"), "true");
    assert.ok(await page.getByRole("link", { name: "Supplier Performance", exact: true }).isVisible());
    await toggle.click();
    await page.locator(".table-region").first().focus();
    await page.keyboard.press("ArrowRight");
    await page.waitForFunction(() => document.querySelector(".table-region").scrollLeft > 0);
    assert.ok(await page.locator(".scroll-hint").isVisible());
  });
  await check("Existing valuation, validation and stale-result behavior are preserved", async () => {
    await visit("/stock-valuation?apply=true");
    assert.match(await page.locator("[data-results]").innerText(), /49,050\.00/);
    await page.getByLabel("As-of date").fill("2026-07-09");
    assert.equal(await page.locator("[data-results]").isVisible(), false);
    await page.getByRole("button", { name: "Compute valuation" }).click();
    await page.waitForURL(url => url.searchParams.get("date") === "2026-07-09");
    assert.match(await page.locator("main").innerText(), /Results unavailable/);
    await visit("/stock-valuation?apply=true");
    await page.getByLabel("Item ID", { exact: true }).fill("");
    await page.getByRole("button", { name: "Compute valuation" }).click();
    assert.ok(await page.locator("#client-errors").evaluate(el => el === document.activeElement));
  });
  if (baseline) await check("All table values and form contracts match the previous application", async () => {
    const evidence = () => page.evaluate(() => ({
      rows: [...document.querySelectorAll("tbody tr")].map(row => [...row.children].map(cell => cell.textContent.trim())),
      forms: [...document.forms].map(form => ({ action: new URL(form.action).pathname, method: form.method,
        fields: [...form.elements].map(field => [field.tagName, field.type, field.name, field.value]) }))
    }));
    for (const route of routes) {
      await visit(route, baseline); const previous = await evidence();
      await visit(route); assert.deepEqual(await evidence(), previous, route);
    }
  });
  await check("Report printing retains all columns and removes the app shell", async () => {
    await page.setViewportSize({ width: 794, height: 1123 });
    for (const id of ["R1", "R2", "R3", "R4", "R5"]) {
      await visit(`/reports/${id}/preview`);
      await page.emulateMedia({ media: "print" });
      assert.equal(await page.locator(".sidebar").isVisible(), false);
      assert.ok(await page.locator(".table-region").evaluateAll(regions => regions.every(el => el.scrollWidth <= el.clientWidth + 1)), id);
      await page.pdf({ path: fileURLToPath(new URL(`${id}.pdf`, artifacts)), format: "A4", printBackground: true });
      await page.emulateMedia({ media: "screen" });
    }
  });
  await check("Navigation and GET forms still work without JavaScript", async () => {
    const context = await browser.newContext({ javaScriptEnabled: false, viewport: { width: 390, height: 844 } });
    const plain = await context.newPage();
    await plain.goto(base + "/stock-valuation");
    assert.ok(await plain.getByRole("link", { name: "Dashboard", exact: true }).isVisible());
    await plain.getByRole("button", { name: "Compute valuation" }).click();
    await plain.waitForURL(url => url.searchParams.has("apply"));
    assert.match(await plain.locator("[data-results]").innerText(), /49,050\.00/);
    await context.close();
  });
  await page.emulateMedia({ media: "screen" });
  for (const [name, route, width] of [["dashboard", "/", 1440], ["valuation", "/stock-valuation?apply=true", 1440],
    ["suppliers", "/suppliers/performance", 1440], ["reports", "/reports?report=R4", 1440], ["mobile-reorder", "/reorder", 390]]) {
    await page.setViewportSize({ width, height: 960 });
    await visit(route);
    await page.screenshot({ path: fileURLToPath(new URL(`${name}.png`, artifacts)), fullPage: true });
  }
  await check("No browser script errors", async () => assert.deepEqual(errors, []));
} finally {
  await writeFile(new URL("results.json", artifacts), JSON.stringify(checks, null, 2));
  await browser.close();
}
if (checks.some(check => !check.passed)) process.exitCode = 1;
