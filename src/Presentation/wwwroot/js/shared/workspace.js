// Progressive enhancement only. GET forms and navigation work without JavaScript.
(() => {
  const toggle = document.querySelector(".nav-toggle");
  const nav = document.getElementById("primary-nav");
  const narrow = window.matchMedia("(max-width: 991px)");
  const setNavigation = () => {
    nav.hidden = narrow.matches;
    toggle.setAttribute("aria-expanded", String(!nav.hidden));
  };
  setNavigation();
  narrow.addEventListener("change", setNavigation);
  toggle.addEventListener("click", () => {
    nav.hidden = !nav.hidden;
    toggle.setAttribute("aria-expanded", String(!nav.hidden));
  });

  // Keep table regions keyboard accessible; show guidance only for horizontal overflow.
  const tables = document.querySelectorAll(".table-region");
  const updateTables = () => tables.forEach((region) => {
    const scrolls = region.scrollWidth > region.clientWidth + 1;
    const hint = region.nextElementSibling;
    if (hint?.classList.contains("scroll-hint")) hint.hidden = !scrolls;
  });
  const tableObserver = new ResizeObserver(updateTables);
  tables.forEach((region) => tableObserver.observe(region));
  document.fonts.ready.then(updateTables);

  const form = document.getElementById("filters");
  const results = document.querySelector("[data-results]");
  const status = document.getElementById("request-status");
  if (document.getElementById("validation-summary"))
    document.getElementById("validation-summary").focus();
  if (!form) return;
  // Invalidating results immediately prevents a changed date/ID from retaining old evidence.
  form.addEventListener("input", () => {
    if (results) results.hidden = true;
    if (status) {
      status.classList.remove("visually-hidden");
      status.textContent = "Apply filters to view updated results.";
    }
  });
  const addError = (input, message) => {
    input.setAttribute("aria-invalid", "true");
    const error = document.createElement("span");
    error.id = `${input.id}-error`;
    error.className = "field-error";
    error.textContent = message;
    input.insertAdjacentElement("afterend", error);
    input.dataset.originalDescription =
      input.getAttribute("aria-describedby") || "";
    input.setAttribute(
      "aria-describedby",
      [input.dataset.originalDescription, error.id].filter(Boolean).join(" "),
    );
  };
  form.noValidate = true;
  form.addEventListener("submit", (event) => {
    form.querySelectorAll(".field-error").forEach((x) => x.remove());
    form.querySelectorAll("[aria-invalid]").forEach((x) => {
      x.removeAttribute("aria-invalid");
      x.setAttribute("aria-describedby", x.dataset.originalDescription || "");
    });
    document.getElementById("client-errors")?.remove();
    const errors = [];
    form.querySelectorAll("[required]").forEach((input) => {
      if (!input.value.trim() || !input.checkValidity())
        errors.push([input, "Enter a valid value for this required field."]);
    });
    const start = form.querySelector("[name=start]");
    const end = form.querySelector("[name=end]");
    if (start?.value && end?.value && start.value > end.value)
      errors.push([end, "The end date must be on or after the start date."]);
    if (errors.length) {
      event.preventDefault();
      const summary = document.createElement("div");
      summary.id = "client-errors";
      summary.className = "notice notice-error";
      summary.tabIndex = -1;
      summary.setAttribute("role", "alert");
      const heading = document.createElement("strong");
      heading.textContent = "Check your inputs";
      summary.append(heading);
      const list = document.createElement("ul");
      errors.forEach(([input, message]) => {
        addError(input, message);
        const item = document.createElement("li");
        const link = document.createElement("a");
        link.href = `#${input.id}`;
        link.textContent = `${form.querySelector(`label[for="${input.id}"]`)?.textContent || "Field"}: ${message}`;
        item.append(link);
        list.append(item);
      });
      summary.append(list);
      form.before(summary);
      summary.focus();
      return;
    }
    if (status) {
      status.classList.remove("visually-hidden");
      status.textContent = "Loading results...";
    }
    form.setAttribute("aria-busy", "true");
    // Native navigation owns request cancellation and Back/Forward; no asynchronous result races.
  });
  window.addEventListener("pageshow", (event) => {
    form.removeAttribute("aria-busy");
    if (event.persisted || performance.getEntriesByType("navigation")[0]?.type === "back_forward") {
      // Browsers may restore edited controls over server-rendered results after Back.
      // Restore the HTML defaults, which represent the canonical URL context.
      form.reset();
      if (results) results.hidden = false;
      if (status) { status.textContent = ""; status.classList.add("visually-hidden"); }
      document.getElementById("client-errors")?.remove();
      form.querySelectorAll(".field-error").forEach(element => element.remove());
      form.querySelectorAll("[aria-invalid]").forEach(element => {
        element.removeAttribute("aria-invalid");
        element.setAttribute("aria-describedby", element.dataset.originalDescription || "");
      });
    }
  });
})();
