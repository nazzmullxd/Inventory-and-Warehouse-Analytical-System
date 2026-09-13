using Iwas.Business.Abstractions;
using Iwas.Business.Dashboard;
using Iwas.Business.Matching;
using Iwas.Business.Reorder;
using Iwas.Business.Stock;
using Iwas.Business.Suppliers;
using Iwas.Business.Valuation;
using Iwas.Model.Data.MySql;
using Iwas.Model.Repositories;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables().AddCommandLine(args);
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IWarehouseReadStore>(_ => new MySqlWarehouseReadStore(
    builder.Configuration.GetConnectionString("Warehouse")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Warehouse. See docs/DATABASE_SETUP.md.")));
builder.Services.AddSingleton<StockLedgerService>();
builder.Services.AddSingleton<FifoValuationService>();
builder.Services.AddSingleton<SupplierPerformanceService>();
builder.Services.AddSingleton<ReorderService>();
builder.Services.AddSingleton<RequisitionMatchingService>();
builder.Services.AddScoped<IWarehouseAnalytics, WarehouseAnalyticsService>();
var app = builder.Build();
app.UseExceptionHandler("/errors/unavailable");
app.UseStaticFiles();
app.UseRouting();
app.MapControllers();
app.MapFallbackToController("Missing", "Workspace");
app.Run();
