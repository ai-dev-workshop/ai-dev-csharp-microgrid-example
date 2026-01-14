using BlazorApp3.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<EnergyAsset> Assets => Set<EnergyAsset>();
    public DbSet<EnergyReading> Readings => Set<EnergyReading>();
    public DbSet<EnergyForecast> Forecasts => Set<EnergyForecast>();
    public DbSet<FlexibleLoadProfile> FlexibleLoadProfiles => Set<FlexibleLoadProfile>();
    public DbSet<EnergyTradeOffer> TradeOffers => Set<EnergyTradeOffer>();
    public DbSet<EnergyTradeTransaction> TradeTransactions => Set<EnergyTradeTransaction>();
    public DbSet<EnergyWallet> Wallets => Set<EnergyWallet>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EnergyAsset>()
            .Property(e => e.Type)
            .HasConversion<string>();

        modelBuilder.Entity<EnergyForecast>()
            .Property(e => e.ConfidenceLevel)
            .HasConversion<string>();

        modelBuilder.Entity<EnergyTradeOffer>()
            .Property(e => e.TradeType)
            .HasConversion<string>();
    }
}
