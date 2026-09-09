using System.Globalization;
using Iwas.Business.Abstractions;
using Iwas.Business.Matching;
using Iwas.Business.Reorder;
using Iwas.Business.Suppliers;
using Iwas.Business.Valuation;
using Iwas.Model.Contracts.Common;
using Iwas.Presentation.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Iwas.Presentation.Controllers;

public sealed class WorkspaceController(IWarehouseAnalytics analytics) : Controller
{
    private static readonly CultureInfo C = CultureInfo.InvariantCulture;
    private WorkspaceModel ModelFor(string page, string title, string description)
    {
        string Read(string key, string fallback) => Request.Query.TryGetValue(key, out var value) ? value.ToString() : fallback;
        var m = new WorkspaceModel { Page = page, Title = title, Description = description, Date = Read("date", "2026-07-08"), Start = Read("start", page == "Clarifications" ? "2026-07-08" : "2026-01-01"), End = Read("end", page == "Clarifications" ? "2026-07-08" : "2026-06-30"), Item = Read("item", "IT-1108"), Requisition = Read("requisition", "RQ-0871"), Filter = Read("filter", "all"), Report = Read("report", "R1"), Applied = Request.Query.ContainsKey("apply"), Scenario = "success" };
        var returnUrl = Read("returnUrl", "/requisitions/clarifications"); if (Url.IsLocalUrl(returnUrl)) m.ReturnUrl = returnUrl;
        if (!TryDate(m.Date, out var analysisDate) || !TryDate(m.Start, out var start) || !TryDate(m.End, out var end)) m.Error = "Enter valid dates in the required fields."; else if (start > end) m.Error = "The start date must be on or before the end date."; else if (analysisDate > new DateOnly(2026, 7, 8)) { m.Available = false; m.Error = "Results unavailable beyond the demonstration snapshot date."; }
        return m;
    }

    [HttpGet("/")]
    public async Task<IActionResult> Dashboard(CancellationToken token)
    {
        var m = ModelFor("Dashboard", "Dashboard", "Your warehouse, at a glance.");
        if (m.Error is null) { var r = await analytics.BuildOverviewAsync(Date(m.Date), token); Apply(m, r.Context, r.Issues); if (r.Data is { } d) { m.Metrics["value"] = d.WarehouseFifoValue?.ToString("N2", C) ?? "Unavailable"; m.Metrics["order"] = d.OrderNowCount.ToString(C); m.Metrics["dead"] = d.DeadStockCount.ToString(C); m.Metrics["watch"] = d.WatchListSupplierCount.ToString(C); m.Metrics["clarification"] = d.ClarificationCount.ToString(C); } }
        return View("~/Views/Dashboard/Index.cshtml", m);
    }

    [HttpGet("/stock-valuation")]
    public async Task<IActionResult> StockValuation(CancellationToken token)
    {
        var m = ModelFor("StockValuation", "Stock Valuation", "Inspect closing stock and the receipt layers behind its value.");
        if (m.Applied && string.IsNullOrWhiteSpace(m.Item)) m.Error = "Enter an item ID.";
        if (m.Error is null && m.Applied) { var r = await analytics.ValueItemAsync(m.Item, Date(m.Date), token); Apply(m, r.Context, r.Issues); if (r.Data is { } v) { m.Metrics["name"] = v.Name; m.Metrics["unit"] = v.Unit; m.Metrics["quantity"] = N(v.ClosingQuantity); m.Metrics["value"] = Money(v.FifoValue); m.PrimaryTable = ValuationLayers(v); } }
        return View("~/Views/StockValuation/Index.cshtml", m);
    }

    [HttpGet("/reorder")]
    public async Task<IActionResult> Reorder(CancellationToken token)
    {
        var m = ModelFor("Reorder", "Reorder and EOQ", "See what to order, when to order, and the evidence behind it.");
        if (m.Error is null) { var r = await analytics.AnalyzeReorderAsync(Date(m.Date), token); Apply(m, r.Context, r.Issues); if (r.Data is { } rows) m.PrimaryTable = ReorderTable(FilterReorders(rows, m.Filter)); }
        return View("~/Views/Reorder/Index.cshtml", m);
    }

    [HttpGet("/suppliers/performance")]
    public async Task<IActionResult> Suppliers(CancellationToken token)
    {
        var m = ModelFor("Suppliers", "Supplier Performance", "Review delivery reliability over a clearly defined period.");
        if (m.Error is null) { var r = await analytics.AnalyzeSuppliersAsync(Date(m.Start), Date(m.End), token); Apply(m, r.Context, r.Issues); if (r.Data is { } rows) m.PrimaryTable = SupplierTable(rows.Where(x => m.Filter switch { "watch" => x.Standing == SupplierStanding.WatchList, "good" => x.Standing == SupplierStanding.Good, "excellent" => x.Standing == SupplierStanding.Excellent, _ => true })); }
        return View("~/Views/Suppliers/Index.cshtml", m);
    }

    [HttpGet("/requisitions/matching")]
    public async Task<IActionResult> Matching(CancellationToken token)
    {
        var m = ModelFor("Matching", "Requisition Matching", "Inspect how an existing requisition relates to the catalogue.");
        if (m.Applied && string.IsNullOrWhiteSpace(m.Requisition)) m.Error = "Enter a requisition ID.";
        if (m.Error is null && m.Applied) { var r = await analytics.MatchRequisitionAsync(m.Requisition, Date(m.Date), token); Apply(m, r.Context, r.Issues); if (r.Data is { } match) { m.MatchResult = MatchView(match); m.PrimaryTable = CandidateTable(match); } }
        return View("~/Views/Requisitions/Matching.cshtml", m);
    }

    [HttpGet("/requisitions/clarifications")]
    public async Task<IActionResult> Clarifications(CancellationToken token)
    {
        var m = ModelFor("Clarifications", "Clarification Needed", "Requisitions that need a closer look outside IWAS.");
        if (m.Error is null) { var r = await analytics.MatchPeriodAsync(Date(m.Start), Date(m.End), Date(m.Date), token); Apply(m, r.Context, r.Issues); if (r.Data is { } rows) { var filtered = rows.Where(x => x.Status == MatchStatus.ClarificationRequired).Where(x => m.Filter switch { "low" => x.Reason == MatchReason.BelowThreshold, "tie" => x.Reason == MatchReason.AmbiguousTopScore, "none" => x.Reason is MatchReason.NoCandidates or MatchReason.EmptyRequisitionText, _ => true }); m.PrimaryTable = MatchLogTable(filtered); } }
        return View("~/Views/Requisitions/Clarifications.cshtml", m);
    }

    [HttpGet("/reports")]
    public IActionResult Reports() { var m = ModelFor("Reports", "Reports", "Choose a report, confirm its scope, and review a printable result."); return WorkspaceModel.ReportTitles.ContainsKey(m.Report) ? View("~/Views/Reports/Index.cshtml", m) : Missing(); }

    [HttpGet("/reports/{reportId}/preview")]
    public async Task<IActionResult> Preview(string reportId, CancellationToken token)
    {
        if (!WorkspaceModel.ReportTitles.TryGetValue(reportId, out var title)) return Missing();
        var m = ModelFor("Preview", title, "Review the complete computed scope before printing."); m.Report = reportId; m.Applied = true;
        if (m.Error is null) await PopulateReport(m, token); return View("~/Views/Reports/Preview.cshtml", m);
    }

    private async Task PopulateReport(WorkspaceModel m, CancellationToken token)
    {
        switch (m.Report)
        {
            case "R1": { var r = await analytics.BuildDailyMovementAsync(Date(m.Date), token); Apply(m, r.Context, r.Issues); if (r.Data is { } x) m.PrimaryTable = MovementTable(x); break; }
            case "R2": { var r = await analytics.ValueWarehouseAsync(Date(m.Date), token); Apply(m, r.Context, r.Issues); if (r.Data is { } x) m.PrimaryTable = ValuationTable(x); break; }
            case "R3": { var r = await analytics.AnalyzeReorderAsync(Date(m.Date), token); Apply(m, r.Context, r.Issues); if (r.Data is { } x) m.PrimaryTable = ReorderTable(x); break; }
            case "R4": { var s = await analytics.AnalyzeSuppliersAsync(Date(m.Start), Date(m.End), token); Apply(m, s.Context, s.Issues); if (s.Data is { } x) m.PrimaryTable = SupplierTable(x); var d = await analytics.AnalyzeReorderAsync(Date(m.Date), token); if (d.Data is { } y) m.SecondaryTable = DeadStockTable(y.Where(z => z.IsDisposalCandidate)); break; }
            case "R5": { var r = await analytics.MatchPeriodAsync(Date(m.Date), Date(m.Date), Date(m.Date), token); Apply(m, r.Context, r.Issues); if (r.Data is { } x) m.PrimaryTable = MatchLogTable(x); break; }
        }
    }

    [HttpGet("/errors/{kind}")]
    public IActionResult ErrorPage(string kind) { Response.StatusCode = kind == "forbidden" ? 403 : 503; return View("~/Views/Errors/Index.cshtml", ModelFor("Errors", kind == "forbidden" ? "You do not have access to this page" : "This page is unavailable", "Return to the workspace or try again. No source records have changed.")); }
    public IActionResult Missing() { Response.StatusCode = 404; return View("~/Views/Errors/Index.cshtml", ModelFor("Errors", "We couldn’t find that page", "Check the address or return to your warehouse overview.")); }

    private static void Apply(WorkspaceModel m, ReadContextMetadata context, IReadOnlyList<AnalysisIssue> issues) { m.Snapshot = context.SnapshotId; m.GeneratedAt = context.ReadAtUtc.ToString("u", C); var error = issues.FirstOrDefault(x => x.Severity == IssueSeverity.Blocking); if (error is not null) { m.Available = false; m.Error = error.Message; } }
    private static IEnumerable<ReorderAnalysis> FilterReorders(IEnumerable<ReorderAnalysis> rows, string filter) => rows.Where(x => filter switch { "now" => x.ReorderNow, "projected" => x.Action == ReorderAction.OrderInDays, "ok" => x.Action == ReorderAction.Ok, "dead" => x.IsDeadStock, _ => true });
    private static DataTable ValuationLayers(StockValuation v) => new("Remaining FIFO layers", ["Receipt date", "Remaining quantity", "Unit purchase price", "Layer value"], v.Layers.Select(x => new[] { x.Layer.ReceiptDate.ToString("dd MMM yyyy", C), $"{N(x.Layer.RemainingQuantity)} {v.Unit}s", Money(x.Layer.UnitPurchasePrice!.Value), Money(x.LayerValue) }).ToArray(), $"Closing stock: {N(v.ClosingQuantity)} {v.Unit} · FIFO value: {Money(v.FifoValue)}");
    private static DataTable ValuationTable(IEnumerable<StockValuation> rows) { var a = rows.ToArray(); return new("Warehouse stock valuation", ["Item", "Closing quantity", "FIFO value"], a.Select(x => new[] { $"{x.Name} · {x.ItemId}", $"{N(x.ClosingQuantity)} {x.Unit}", Money(x.FifoValue) }).ToArray(), $"Warehouse total: {Money(a.Sum(x => x.FifoValue))}"); }
    private static DataTable ReorderTable(IEnumerable<ReorderAnalysis> rows) => new("Reorder recommendations", ["Item ID", "Stock", "Annual demand", "EOQ", "Lead time", "ROP", "Days to ROP", "Dead stock", "Recommendation"], rows.Select(x => new[] { x.ItemId, N(x.StockOnHand), N(x.AnnualDemand), N(x.SuggestedEoq), $"{N(x.EffectiveLeadTimeDays)} days", N(x.ReorderPoint), x.DaysToReorderPoint?.ToString(C) ?? "N/A", x.IsDeadStock ? "Yes" : "No", Action(x) }).ToArray());
    private static DataTable DeadStockTable(IEnumerable<ReorderAnalysis> rows) => new("Dead stock · no issue in the last 180 days", ["Item ID", "Quantity", "Last issue", "Recommendation"], rows.Select(x => new[] { x.ItemId, N(x.StockOnHand), x.LastIssueDate?.ToString("dd MMM yyyy", C) ?? "No issue", "Review for disposal outside IWAS" }).ToArray());
    private static DataTable SupplierTable(IEnumerable<SupplierPerformance> rows) => new("Supplier delivery reliability", ["Supplier / ID", "Delivered", "On-time", "On-time ratio", "Late", "Average late delay", "Standing"], rows.Select(x => new[] { $"{x.SupplierName} · {x.SupplierId}", x.DeliveredOrders.ToString(), x.OnTimeOrders.ToString(), x.OnTimeRatio.ToString("P0", C), x.LateOrders.ToString(), x.AverageLateDelayDays is null ? "No late-delivery sample" : $"{N(x.AverageLateDelayDays.Value)} days", Standing(x.Standing) }).ToArray());
    private static DataTable MovementTable(IEnumerable<DailyMovement> rows) { var a = rows.ToArray(); return new("Daily stock movement", ["Item", "Unit", "Opening", "Received", "Issued", "Closing"], a.Select(x => new[] { $"{x.ItemName} · {x.ItemId}", x.Unit, N(x.OpeningQuantity), N(x.ReceivedQuantity), N(x.IssuedQuantity), N(x.ClosingQuantity) }).ToArray(), $"Movements recorded today: {a.Sum(x => x.ReceiptMovementCount)} receipts · {a.Sum(x => x.IssueMovementCount)} issues"); }
    private static DataTable MatchLogTable(IEnumerable<RequisitionMatch> rows) => new("Requisition matching log", ["Requisition", "Department", "Matched item / best candidate", "Similarity", "Status"], rows.Select(x => new[] { x.RequisitionId, x.Department ?? "—", x.MatchedItemId ?? x.Candidates.FirstOrDefault()?.ItemId ?? "No candidate", x.Candidates.FirstOrDefault() is { } c ? c.Similarity.ToString("P0", C) : "N/A", x.Status == MatchStatus.AutoMatched ? "Matched" : "Clarification" }).ToArray());
    private static DataTable CandidateTable(RequisitionMatch x) => new("Computed catalogue candidates", ["Rank", "Item", "Similarity", "Common terms"], x.Candidates.Select(c => new[] { c.Rank.ToString(), c.ItemId, c.Similarity.ToString("P0", C), c.CommonTerms.ToString() }).ToArray());
    private static MatchFixture MatchView(RequisitionMatch x) => new(x.RequisitionId, x.Department ?? "—", x.SourceText ?? "", x.MatchedItemId ?? x.Candidates.FirstOrDefault()?.ItemId ?? "No candidate", x.Candidates.FirstOrDefault() is { } c ? c.Similarity.ToString("P0", C) : "N/A", x.Status == MatchStatus.AutoMatched ? "Matched" : "Clarification", x.Reason.ToString(), x.Quantity.Quantity is null ? "Quantity not identified" : $"{N(x.Quantity.Quantity.Value)} {x.Quantity.Unit ?? "units"}", x.UnitPrice is null ? "Price unavailable" : Money(x.UnitPrice.Value), x.TotalPrice is null ? "Price unavailable" : Money(x.TotalPrice.Value), x.StockOnHand is null ? "Not available" : N(x.StockOnHand.Value), x.HasSufficientStock is null ? "Unknown" : x.HasSufficientStock.Value ? "Sufficient" : "Insufficient");
    private static string Action(ReorderAnalysis x) => x.Action switch { ReorderAction.OrderNow => $"Order {N(x.SuggestedEoq)} now", ReorderAction.OrderInDays => $"Order in {x.DaysToReorderPoint} days", ReorderAction.ReviewDeadStock => "Review dead stock", ReorderAction.ReviewNoDemand => "Review no demand", _ => "OK" };
    private static string Standing(SupplierStanding x) => x switch { SupplierStanding.WatchList => "Watch list", SupplierStanding.Excellent => "Excellent", _ => "Good" };
    private static string N(decimal value) => value.ToString("0.####", C);
    private static string Money(decimal value) => $"BDT {value:N2}";
    private static bool TryDate(string value, out DateOnly date) => DateOnly.TryParseExact(value, "yyyy-MM-dd", C, DateTimeStyles.None, out date);
    private static DateOnly Date(string value) => DateOnly.ParseExact(value, "yyyy-MM-dd", C);
}
