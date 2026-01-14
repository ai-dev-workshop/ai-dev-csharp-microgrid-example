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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EnergyAsset>()
            .Property(e => e.Type)
            .HasConversion<string>();
    }
}
