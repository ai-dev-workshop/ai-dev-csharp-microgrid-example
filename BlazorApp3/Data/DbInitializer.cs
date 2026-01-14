using BlazorApp3.Data.Enums;
using Microsoft.EntityFrameworkCore;
using BlazorApp3.Data.Models;

namespace BlazorApp3.Data;

public static class DbInitializer
{
    public static async Task Initialize(AppDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        if (await context.Assets.AnyAsync())
        {
            return; // DB has been seeded
        }

        var assets = new List<EnergyAsset>
        {
            new EnergyAsset { Name = "North Roof Solar Panel", Type = AssetType.Solar, MaxCapacityKw = 10.0, IsActive = true },
            new EnergyAsset { Name = "East Wing Solar Array", Type = AssetType.Solar, MaxCapacityKw = 15.0, IsActive = true },
            new EnergyAsset { Name = "Main House HVAC", Type = AssetType.Household, MaxCapacityKw = 5.0, IsActive = true },
            new EnergyAsset { Name = "Tesla Wallbox", Type = AssetType.Battery, MaxCapacityKw = 11.0, IsActive = true },
            new EnergyAsset { Name = "Village Water Pump", Type = AssetType.Household, MaxCapacityKw = 2.5, IsActive = true },
            new EnergyAsset { Name = "Community Battery Hub", Type = AssetType.Battery, MaxCapacityKw = 50.0, IsActive = true },
            new EnergyAsset { Name = "Main Grid Connection", Type = AssetType.Grid, MaxCapacityKw = 100.0, IsActive = true }
        };

        context.Assets.AddRange(assets);
        await context.SaveChangesAsync();

        // Seed 7 days of hourly readings
        var random = new Random();
        var readings = new List<EnergyReading>();
        var startDate = DateTime.UtcNow.AddDays(-7);

        foreach (var asset in assets)
        {
            for (int i = 0; i < 24 * 7; i++)
            {
                var timestamp = startDate.AddHours(i);
                double valueKw = GenerateRealisticValue(asset, timestamp, random);
                
                readings.Add(new EnergyReading
                {
                    AssetId = asset.Id,
                    Timestamp = timestamp,
                    ValueKw = valueKw,
                    ConsumptionKwh = Math.Abs(valueKw) // Simplified energy calculation
                });
            }
        }

        context.Readings.AddRange(readings);
        await context.SaveChangesAsync();

        // Seed flexible load profiles
        var flexibleProfiles = new List<FlexibleLoadProfile>();
        foreach (var asset in assets)
        {
            if (asset.Type == AssetType.Battery || asset.Name.Contains("Wallbox"))
            {
                flexibleProfiles.Add(new FlexibleLoadProfile
                {
                    AssetId = asset.Id,
                    MinKw = -asset.MaxCapacityKw,
                    MaxKw = asset.MaxCapacityKw,
                    IsShiftable = true,
                    PreferredStartHour = 22,
                    PreferredEndHour = 6
                });
            }
        }
        context.FlexibleLoadProfiles.AddRange(flexibleProfiles);
        await context.SaveChangesAsync();

        // Seed initial 24-hour forecast (today and tomorrow)
        var forecasts = new List<EnergyForecast>();
        var datesToForecast = new[] { DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(1) };

        foreach (var date in datesToForecast)
        {
            foreach (var asset in assets)
            {
                for (int h = 0; h < 24; h++)
                {
                    var timestamp = date.AddHours(h);
                    // Simple average of historical data (last 7 days at same hour)
                    var historicalReadings = readings
                        .Where(r => r.AssetId == asset.Id && r.Timestamp.Hour == h)
                        .Select(r => r.ValueKw)
                        .ToList();
                    
                    double avgValue = historicalReadings.Any() ? historicalReadings.Average() : 0;

                    forecasts.Add(new EnergyForecast
                    {
                        AssetId = asset.Id,
                        ForecastTimestamp = timestamp,
                        ExpectedKw = avgValue,
                        ConfidenceLevel = ConfidenceLevel.Medium
                    });
                }
            }
        }
        context.Forecasts.AddRange(forecasts);
        await context.SaveChangesAsync();

        // Seed wallets for all assets
        var wallets = assets.Select(a => new EnergyWallet
        {
            AssetId = a.Id,
            CreditBalance = 100.0 // Everyone starts with 100 credits
        }).ToList();
        context.Wallets.AddRange(wallets);
        await context.SaveChangesAsync();

        // Seed a few sample trade offers
        var sampleOffers = new List<EnergyTradeOffer>
        {
            new EnergyTradeOffer 
            { 
                AssetId = assets.First(a => a.Type == AssetType.Solar).Id, 
                TradeType = TradeType.Sell, 
                AvailableKwh = 5.0, 
                PricePerKwh = 0.15,
                IsActive = true
            },
            new EnergyTradeOffer 
            { 
                AssetId = assets.First(a => a.Type == AssetType.Household).Id, 
                TradeType = TradeType.Buy, 
                AvailableKwh = 2.0, 
                PricePerKwh = 0.25,
                IsActive = true
            }
        };
        context.TradeOffers.AddRange(sampleOffers);
        await context.SaveChangesAsync();
    }

    private static double GenerateRealisticValue(EnergyAsset asset, DateTime time, Random random)
    {
        switch (asset.Type)
        {
            case AssetType.Solar:
                // Only produces during daylight
                if (time.Hour > 6 && time.Hour < 18)
                {
                    double peakFactor = Math.Sin((time.Hour - 6) * Math.PI / 12);
                    return asset.MaxCapacityKw * peakFactor * (0.8 + random.NextDouble() * 0.4);
                }
                return 0;
            case AssetType.Wind:
                return asset.MaxCapacityKw * (0.2 + random.NextDouble() * 0.6);
            case AssetType.Household:
                // Higher consumption in morning and evening
                double baseLoad = 0.2;
                if ((time.Hour >= 7 && time.Hour <= 9) || (time.Hour >= 18 && time.Hour <= 22))
                    baseLoad = 0.8;
                return -asset.MaxCapacityKw * (baseLoad + random.NextDouble() * 0.2);
            case AssetType.Battery:
                // Randomly charging or discharging
                return (random.NextDouble() - 0.5) * asset.MaxCapacityKw;
            case AssetType.Grid:
                // Grid balances things, but let's just make it a random flow for now
                return (random.NextDouble() - 0.5) * 20.0;
            default:
                return 0;
        }
    }
}
