using BlazorApp3.Data;
using BlazorApp3.Data.Enums;
using BlazorApp3.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Services;

public class TradingService : IEnergyTradingService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public TradingService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task GenerateTradeOffersAsync(DateTime timestamp)
    {
        using var context = _contextFactory.CreateDbContext();
        
        // Deactivate old offers
        var oldOffers = await context.TradeOffers.Where(o => o.IsActive).ToListAsync();
        foreach (var o in oldOffers) o.IsActive = false;

        var forecasts = await context.Forecasts
            .Where(f => f.ForecastTimestamp >= timestamp && f.ForecastTimestamp < timestamp.AddHours(1))
            .Include(f => f.Asset)
            .ToListAsync();

        foreach (var forecast in forecasts)
        {
            if (forecast.Asset == null || forecast.Asset.Type == AssetType.Grid) continue;

            if (forecast.ExpectedKw > 0) // Surplus -> Sell
            {
                context.TradeOffers.Add(new EnergyTradeOffer
                {
                    AssetId = forecast.AssetId,
                    TradeType = TradeType.Sell,
                    AvailableKwh = forecast.ExpectedKw,
                    PricePerKwh = 0.15, // Demo price
                    CreatedAt = timestamp,
                    IsActive = true
                });
            }
            else if (forecast.ExpectedKw < 0) // Deficit -> Buy
            {
                context.TradeOffers.Add(new EnergyTradeOffer
                {
                    AssetId = forecast.AssetId,
                    TradeType = TradeType.Buy,
                    AvailableKwh = Math.Abs(forecast.ExpectedKw),
                    PricePerKwh = 0.25, // Demo price
                    CreatedAt = timestamp,
                    IsActive = true
                });
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task ExecuteTradingCycleAsync(DateTime timestamp)
    {
        using var context = _contextFactory.CreateDbContext();
        
        var sellOffers = await context.TradeOffers
            .Where(o => o.IsActive && o.TradeType == TradeType.Sell)
            .OrderBy(o => o.PricePerKwh)
            .ThenBy(o => o.CreatedAt)
            .ToListAsync();

        var buyOffers = await context.TradeOffers
            .Where(o => o.IsActive && o.TradeType == TradeType.Buy)
            .OrderByDescending(o => o.PricePerKwh)
            .ThenBy(o => o.CreatedAt)
            .ToListAsync();

        foreach (var buyOffer in buyOffers)
        {
            foreach (var sellOffer in sellOffers.Where(s => s.AvailableKwh > 0 && s.AssetId != buyOffer.AssetId))
            {
                if (buyOffer.AvailableKwh <= 0) break;

                double tradedKwh = Math.Min(buyOffer.AvailableKwh, sellOffer.AvailableKwh);
                double price = sellOffer.PricePerKwh;
                double total = tradedKwh * price;

                var transaction = new EnergyTradeTransaction
                {
                    SellerAssetId = sellOffer.AssetId,
                    BuyerAssetId = buyOffer.AssetId,
                    TradedKwh = tradedKwh,
                    PricePerKwh = price,
                    TotalPrice = total,
                    Timestamp = timestamp,
                    IsExternal = false
                };
                context.TradeTransactions.Add(transaction);

                var sellerWallet = await context.Wallets.FindAsync(sellOffer.AssetId);
                var buyerWallet = await context.Wallets.FindAsync(buyOffer.AssetId);

                if (sellerWallet != null) sellerWallet.CreditBalance += total;
                if (buyerWallet != null) buyerWallet.CreditBalance -= total;

                buyOffer.AvailableKwh -= tradedKwh;
                sellOffer.AvailableKwh -= tradedKwh;

                if (sellOffer.AvailableKwh <= 0) sellOffer.IsActive = false;
            }

            if (buyOffer.AvailableKwh <= 0) buyOffer.IsActive = false;
        }

        // Grid fallback
        var gridAsset = await context.Assets.FirstOrDefaultAsync(a => a.Type == AssetType.Grid);
        if (gridAsset != null)
        {
            foreach (var buyOffer in buyOffers.Where(o => o.IsActive))
            {
                double tradedKwh = buyOffer.AvailableKwh;
                double price = 0.30;
                double total = tradedKwh * price;

                context.TradeTransactions.Add(new EnergyTradeTransaction
                {
                    SellerAssetId = gridAsset.Id,
                    BuyerAssetId = buyOffer.AssetId,
                    TradedKwh = tradedKwh,
                    PricePerKwh = price,
                    TotalPrice = total,
                    Timestamp = timestamp,
                    IsExternal = true
                });

                var buyerWallet = await context.Wallets.FindAsync(buyOffer.AssetId);
                if (buyerWallet != null) buyerWallet.CreditBalance -= total;

                buyOffer.AvailableKwh = 0;
                buyOffer.IsActive = false;
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task<MarketSummary> GetMarketSummaryAsync()
    {
        using var context = _contextFactory.CreateDbContext();
        var today = DateTime.UtcNow.Date;

        var transactions = await context.TradeTransactions
            .Where(t => t.Timestamp >= today)
            .ToListAsync();

        double totalTraded = transactions.Where(t => !t.IsExternal).Sum(t => t.TradedKwh);
        double avgPrice = transactions.Any(t => !t.IsExternal) 
            ? transactions.Where(t => !t.IsExternal).Average(t => t.PricePerKwh) 
            : 0;
        
        double totalDemand = transactions.Sum(t => t.TradedKwh);
        double gridEnergy = transactions.Where(t => t.IsExternal).Sum(t => t.TradedKwh);
        double gridDep = totalDemand > 0 ? (gridEnergy / totalDemand) * 100 : 0;

        return new MarketSummary
        {
            TotalEnergyTradedToday = totalTraded,
            AveragePricePerKwh = avgPrice,
            GridDependencyPercentage = gridDep
        };
    }

    public async Task<List<EnergyTradeOffer>> GetActiveOffersAsync()
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.TradeOffers
            .Where(o => o.IsActive)
            .Include(o => o.Asset)
            .ToListAsync();
    }

    public async Task<List<EnergyTradeTransaction>> GetTradeHistoryAsync(int take = 50)
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.TradeTransactions
            .Include(t => t.SellerAsset)
            .Include(t => t.BuyerAsset)
            .OrderByDescending(t => t.Timestamp)
            .Take(take)
            .ToListAsync();
    }
}
