using Iwas.Presentation.Fixtures;
using Iwas.Presentation.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Iwas.Presentation.Controllers;

// Read-only view routing and input validation only. Replace fixture selection at integration.
public class WorkspaceController : Controller
{
    private WorkspaceModel ContextModel(string page, string title, string description)
    {
        string Read(string key, string fallback) => Request.Query.TryGetValue(key, out var v) ? v.ToString() : fallback;
        var m = new WorkspaceModel {
            Page = page, Title = title, Description = description,
            Date = Read("date", "2026-07-08"), Start = Read("start", page == "Clarifications" ? "2026-07-08" : "2026-01-01"), End = Read("end", page == "Clarifications" ? "2026-07-08" : "2026-06-30"),
            Item = Read("item", "IT-1108"), Requisition = Read("requisition", "RQ-0871"),
            Filter = Read("filter", "all"), Scenario = Read("scenario", "success"), Report = Read("report", "R1"),
            Applied = Request.Query.ContainsKey("apply")
        };
        var returnUrl = Read("returnUrl", "/requisitions/clarifications");
        if (Url.IsLocalUrl(returnUrl)) m.ReturnUrl = returnUrl;
        if (!DateOnly.TryParseExact(m.Date, "yyyy-MM-dd", out _) ||
            !DateOnly.TryParseExact(m.Start, "yyyy-MM-dd", out var start) ||
            !DateOnly.TryParseExact(m.End, "yyyy-MM-dd", out var end))
            m.Error = "Enter valid dates in the required fields.";
        else if (start > end) m.Error = "The start date must be on or before the end date.";
        m.Available = m.Date == "2026-07-08" && (page is not ("Suppliers" or "Reports" or "Preview") ||
            (m.Start == "2026-01-01" && m.End == "2026-06-30"));
        if (page == "Clarifications") m.Available = m.Date == "2026-07-08" && m.Start == "2026-07-08" && m.End == "2026-07-08";
        return m;
    }
    [HttpGet("/")]
    public IActionResult Dashboard() => View("~/Views/Dashboard/Index.cshtml", ContextModel("Dashboard", "Dashboard", "Your warehouse, at a glance."));
    [HttpGet("/stock-valuation")]
    public IActionResult StockValuation()
    {
        var m = ContextModel("StockValuation", "Stock Valuation", "Inspect closing stock and the receipt layers behind its value.");
        if (m.Applied && string.IsNullOrWhiteSpace(m.Item)) m.Error = "Enter an item ID.";
        if (m.Item != "IT-1108") m.Available = false;
        return View("~/Views/StockValuation/Index.cshtml", m);
    }
    [HttpGet("/reorder")]
    public IActionResult Reorder() => View("~/Views/Reorder/Index.cshtml", ContextModel("Reorder", "Reorder and EOQ", "See what to order, when to order, and the evidence behind it."));
    [HttpGet("/suppliers/performance")]
    public IActionResult Suppliers() => View("~/Views/Suppliers/Index.cshtml", ContextModel("Suppliers", "Supplier Performance", "Review delivery reliability over a clearly defined period."));
    [HttpGet("/requisitions/matching")]
    public IActionResult Matching()
    {
        var m = ContextModel("Matching", "Requisition Matching", "Inspect how an existing requisition relates to the catalogue.");
        if (m.Applied && string.IsNullOrWhiteSpace(m.Requisition)) m.Error = "Enter a requisition ID.";
        if (!DisplayFixtures.Matches.Any(x => x.Id == m.Requisition)) m.Available = false;
        return View("~/Views/Requisitions/Matching.cshtml", m);
    }
    [HttpGet("/requisitions/clarifications")]
    public IActionResult Clarifications() => View("~/Views/Requisitions/Clarifications.cshtml", ContextModel("Clarifications", "Clarification Needed", "Requisitions that need a closer look outside IWAS."));
    [HttpGet("/reports")]
    public IActionResult Reports()
    {
        var m = ContextModel("Reports", "Reports", "Choose a report, confirm its scope, and review a printable sample.");
        if (!DisplayFixtures.Reports.ContainsKey(m.Report)) return Missing();
        return View("~/Views/Reports/Index.cshtml", m);
    }
    [HttpGet("/reports/{reportId}/preview")]
    public IActionResult Preview(string reportId)
    {
        if (!DisplayFixtures.Reports.TryGetValue(reportId, out var title)) return Missing();
        var m = ContextModel("Preview", title, "Review the complete illustrative scope before printing.");
        m.Report = reportId; m.Applied = true;
        if (reportId is not "R4") m.Available = m.Date == "2026-07-08";
        return View("~/Views/Reports/Preview.cshtml", m);
    }
    [HttpGet("/errors/{kind}")]
    public IActionResult ErrorPage(string kind)
    {
        Response.StatusCode = kind == "forbidden" ? 403 : 503;
        var m = ContextModel("Errors", kind == "forbidden" ? "You do not have access to this page" : "This page is unavailable", "Return to the workspace or try again. No source records have changed.");
        return View("~/Views/Errors/Index.cshtml", m);
    }
    public IActionResult Missing()
    {
        Response.StatusCode = 404;
        return View("~/Views/Errors/Index.cshtml", ContextModel("Errors", "We couldn’t find that page", "Check the address or return to your warehouse overview."));
    }
}
