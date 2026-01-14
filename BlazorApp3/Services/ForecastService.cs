using BlazorApp3.Data;
using BlazorApp3.Data.Enums;
using BlazorApp3.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Services;

public class ForecastService(AppDbContext context) : IEnergyForecastService
{
    public async Task Generate24HourForecastAsync(DateTime start)
    {
        // Clear old forecasts
        var oldForecasts = await context.Forecasts.ToListAsync();
        context.Forecasts.RemoveRange(oldForecasts);

        var assets = await context.Assets.ToListAsync();
        var sevenDaysAgo = start.AddDays(-7);

        foreach (var asset in assets)
        {
            // Get all readings for this asset in the last 7 days
            var readings = await context.Readings
                .Where(r => r.AssetId == asset.Id && r.Timestamp >= sevenDaysAgo && r.Timestamp < start)
                .ToListAsync();

            for (int h = 0; h < 24; h++)
            {
                var targetTime = start.AddHours(h);
                var hourlyReadings = readings.Where(r => r.Timestamp.Hour == targetTime.Hour).ToList();
                
                double avgValue = hourlyReadings.Any() ? hourlyReadings.Average(r => r.ValueKw) : 0;

                context.Forecasts.Add(new EnergyForecast
                {
                    AssetId = asset.Id,
                    ForecastTimestamp = targetTime,
                    ExpectedKw = avgValue,
                    ConfidenceLevel = hourlyReadings.Count > 5 ? ConfidenceLevel.High : ConfidenceLevel.Medium
                });
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task<List<EnergyForecast>> GetForecastAsync()
    {
        return await context.Forecasts
            .Include(f => f.Asset)
            .OrderBy(f => f.ForecastTimestamp)
            .ToListAsync();
    }

    public async Task<List<ForecastSummary>> GetForecastSummaryAsync()
    {
        var forecasts = await context.Forecasts
            .Include(f => f.Asset)
            .ToListAsync();

        return forecasts
            .GroupBy(f => f.ForecastTimestamp)
            .Select(g => new ForecastSummary
            {
                Hour = g.Key,
                TotalProduction = g.Where(f => f.ExpectedKw > 0).Sum(f => f.ExpectedKw),
                TotalConsumption = g.Where(f => f.ExpectedKw < 0).Sum(f => f.ExpectedKw)
            })
            .OrderBy(s => s.Hour)
            .ToList();
    }

    public async Task<List<SurplusDeficitWindow>> GetSurplusDeficitWindowsAsync()
    {
        var summaries = await GetForecastSummaryAsync();
        var windows = new List<SurplusDeficitWindow>();

        if (!summaries.Any()) return windows;

        SurplusDeficitWindow? currentWindow = null;

        foreach (var summary in summaries)
        {
            bool isSurplus = summary.NetBalance > 0;

            if (currentWindow == null || (currentWindow.IsSurplus != isSurplus))
            {
                if (currentWindow != null)
                {
                    currentWindow.EndTime = summary.Hour;
                    windows.Add(currentWindow);
                }

                currentWindow = new SurplusDeficitWindow
                {
                    StartTime = summary.Hour,
                    TotalNetKw = summary.NetBalance
                };
            }
            else
            {
                currentWindow.TotalNetKw += summary.NetBalance;
            }
        }

        if (currentWindow != null)
        {
            currentWindow.EndTime = currentWindow.StartTime.AddHours(1);
            windows.Add(currentWindow);
        }

        return windows;
    }
}
