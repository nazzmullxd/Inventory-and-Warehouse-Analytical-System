var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
var app = builder.Build();
app.UseExceptionHandler("/errors/unavailable");
app.UseStaticFiles();
app.UseRouting();
app.MapControllers();
app.MapFallbackToController("Missing", "Workspace");
app.Run();
