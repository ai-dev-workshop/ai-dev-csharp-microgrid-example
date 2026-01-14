using BlazorApp3.Components;
using BlazorApp3.Data;
using BlazorApp3.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Database
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite("Data Source=ecogrid.db"));

// Add Application Services
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IEnergyForecastService, ForecastService>();
builder.Services.AddScoped<ILoadOptimizationService, OptimizationService>();
builder.Services.AddScoped<IStatsService, StatsService>();
builder.Services.AddScoped<IEnergyTradingService, TradingService>();

var app = builder.Build();

// Initialize Database
using (var scope = app.Services.CreateScope())
{
    var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var context = contextFactory.CreateDbContext();
    await DbInitializer.Initialize(context);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
