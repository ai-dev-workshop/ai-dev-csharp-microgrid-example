using BlazorApp3.Data;
using BlazorApp3.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Services;

public class OptimizationService(
    IDbContextFactory<AppDbContext> contextFactory,
    IEnergyForecastService forecastService) : ILoadOptimizationService
{
    private bool _isOptimizationApplied;

    public bool IsOptimizationApplied => _isOptimizationApplied;

    public async Task<List<OptimizationRecommendation>> GetRecommendationsAsync()
    {
        using var context = contextFactory.CreateDbContext();
        var profiles = await context.FlexibleLoadProfiles
            .Include(p => p.Asset)
            .Where(p => p.IsShiftable)
            .ToListAsync();

        var windows = await forecastService.GetSurplusDeficitWindowsAsync();
        var surplusWindows = windows.Where(w => w.IsSurplus).ToList();
        
        var recommendations = new List<OptimizationRecommendation>();

        foreach (var profile in profiles)
        {
            if (profile.Asset == null) continue;

            // Simple logic: if there is a surplus window outside the preferred hour, suggest shifting
            var bestSurplus = surplusWindows.OrderByDescending(w => w.TotalNetKw).FirstOrDefault();
            
            if (bestSurplus != null && (profile.PreferredStartHour > bestSurplus.StartTime.Hour || profile.PreferredEndHour < bestSurplus.StartTime.Hour))
            {
                recommendations.Add(new OptimizationRecommendation
                {
                    AssetName = profile.Asset.Name,
                    OriginalTimeWindow = $"{profile.PreferredStartHour:00}:00 - {profile.PreferredEndHour:00}:00",
                    OptimizedTimeWindow = $"{bestSurplus.StartTime.Hour:00}:00 - {bestSurplus.EndTime.Hour:00}:00",
                    ExpectedSavingsKwh = Math.Abs(profile.MaxKw) * 0.5 // Estimated savings from using surplus
                });
            }
        }

        return recommendations;
    }

    public void ToggleOptimization(bool apply)
    {
        _isOptimizationApplied = apply;
    }

    public async Task ApplyOptimizationAsync()
    {
        _isOptimizationApplied = true;
        await Task.CompletedTask;
    }
}
