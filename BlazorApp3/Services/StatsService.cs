using BlazorApp3.Data;
using BlazorApp3.Data.Enums;
using BlazorApp3.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Services;

public class StatsService : IStatsService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILoadOptimizationService _optimizationService;

    public StatsService(IDbContextFactory<AppDbContext> contextFactory, ILoadOptimizationService optimizationService)
    {
        _contextFactory = contextFactory;
        _optimizationService = optimizationService;
    }

    public async Task<double> GetTotalProductionTodayAsync()
    {
        using var context = _contextFactory.CreateDbContext();
        var today = DateTime.UtcNow.Date;
        return await context.Readings
            .Where(r => r.Timestamp >= today && r.ValueKw > 0)
            .SumAsync(r => r.ValueKw);
    }

    public async Task<double> GetTotalConsumptionTodayAsync()
    {
        using var context = _contextFactory.CreateDbContext();
        var today = DateTime.UtcNow.Date;
        return await context.Readings
            .Where(r => r.Timestamp >= today && r.ValueKw < 0)
            .SumAsync(r => Math.Abs(r.ValueKw));
    }

    public async Task<double> GetEfficiencyScoreAsync()
    {
        var prod = await GetTotalProductionTodayAsync();
        var cons = await GetTotalConsumptionTodayAsync();
        if (cons == 0) return 100;
        return Math.Min(100, (prod / cons) * 100);
    }

    public async Task<EnergyAsset?> GetTopConsumerAsync()
    {
        using var context = _contextFactory.CreateDbContext();
        var today = DateTime.UtcNow.Date;
        var topAssetId = await context.Readings
            .Where(r => r.Timestamp >= today && r.ValueKw < 0)
            .GroupBy(r => r.AssetId)
            .OrderByDescending(g => g.Sum(r => Math.Abs(r.ValueKw)))
            .Select(g => g.Key)
            .FirstOrDefaultAsync();

        return await context.Assets.FindAsync(topAssetId);
    }

    public async Task<List<EnergyReading>> GetRecentReadingsAsync(int count)
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.Readings
            .Include(r => r.Asset)
            .OrderByDescending(r => r.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    public async Task SimulateHourAsync()
    {
        using var context = _contextFactory.CreateDbContext();
        var assets = await context.Assets.Where(a => a.IsActive).ToListAsync();
        var profiles = await context.FlexibleLoadProfiles.ToListAsync();
        var random = new Random();
        var now = DateTime.UtcNow;

        foreach (var asset in assets)
        {
            double value = GenerateRealisticValue(asset, now, random);

            // Apply optimization logic if enabled
            if (_optimizationService.IsOptimizationApplied)
            {
                var profile = profiles.FirstOrDefault(p => p.AssetId == asset.Id);
                if (profile != null && profile.IsShiftable)
                {
                    // If it's a battery or shiftable load, reduce consumption during peak deficit
                    // For simulation: if it's evening, reduce load; if it's noon, increase (charging during surplus)
                    if (now.Hour >= 17 && now.Hour <= 21) // Peak evening deficit
                    {
                        value *= 0.5; // Reduce load/charging
                    }
                    else if (now.Hour >= 11 && now.Hour <= 15) // Peak solar surplus
                    {
                        value *= 1.5; // Increase charging/use
                    }
                }
            }

            context.Readings.Add(new EnergyReading
            {
                AssetId = asset.Id,
                Timestamp = now,
                ValueKw = value,
                ConsumptionKwh = Math.Abs(value)
            });
        }

        await context.SaveChangesAsync();
    }

    private double GenerateRealisticValue(EnergyAsset asset, DateTime time, Random random)
    {
        switch (asset.Type)
        {
            case AssetType.Solar:
                if (time.Hour > 6 && time.Hour < 18)
                {
                    double peakFactor = Math.Sin((time.Hour - 6) * Math.PI / 12);
                    return asset.MaxCapacityKw * peakFactor * (0.8 + random.NextDouble() * 0.4);
                }
                return 0;
            case AssetType.Wind:
                return asset.MaxCapacityKw * (0.2 + random.NextDouble() * 0.6);
            case AssetType.Household:
                return -asset.MaxCapacityKw * (0.4 + random.NextDouble() * 0.4);
            case AssetType.Battery:
                return (random.NextDouble() - 0.5) * asset.MaxCapacityKw;
            case AssetType.Grid:
                return (random.NextDouble() - 0.5) * 20.0;
            default:
                return 0;
        }
    }
}
