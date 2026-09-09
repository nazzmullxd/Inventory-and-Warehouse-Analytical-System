namespace Iwas.Presentation.ViewModels;

public record DataTable(string Caption, string[] Headers, string[][] Rows, string? Footer = null);
public record MatchFixture(string Id, string Department, string Text, string Item, string Similarity,
    string Status, string Reason, string Quantity, string Price, string Total, string Stock, string Sufficiency);

public class WorkspaceModel
{
    public static readonly IReadOnlyDictionary<string, string> ReportTitles = new Dictionary<string, string>
    {
        ["R1"] = "Daily Stock Movement", ["R2"] = "Stock Valuation (FIFO)", ["R3"] = "Reorder List",
        ["R4"] = "Supplier Performance and Dead Stock", ["R5"] = "Requisition Matching Log"
    };
    public string Page { get; set; } = "Dashboard";
    public string Title { get; set; } = "Dashboard";
    public string Description { get; set; } = "A clear view of your warehouse.";
    public string Date { get; set; } = "2026-07-08";
    public string Start { get; set; } = "2026-01-01";
    public string End { get; set; } = "2026-06-30";
    public string Item { get; set; } = "IT-1108";
    public string Requisition { get; set; } = "RQ-0871";
    public string Filter { get; set; } = "all";
    public string Report { get; set; } = "R1";
    public bool Applied { get; set; }
    public string? Error { get; set; }
    public bool Available { get; set; } = true;
    public string ReturnUrl { get; set; } = "/requisitions/clarifications";
    public DataTable? PrimaryTable { get; set; }
    public DataTable? SecondaryTable { get; set; }
    public MatchFixture? MatchResult { get; set; }
    public Dictionary<string, string> Metrics { get; } = new(StringComparer.OrdinalIgnoreCase);
    public string Snapshot { get; set; } = "Not loaded";
    public string GeneratedAt { get; set; } = "";
    public string DisplayDate(string value) => DateOnly.TryParseExact(value, "yyyy-MM-dd", out var date)
        ? date.ToString("dd MMM yyyy", System.Globalization.CultureInfo.InvariantCulture) : value;
    public string DateLabel => DisplayDate(Date);
    public string PeriodLabel => $"{DisplayDate(Start)} – {DisplayDate(End)} (inclusive)";
    public string ReportUrl(string id) => $"/reports/{id}/preview?date={Date}&start={Start}&end={End}";
}
