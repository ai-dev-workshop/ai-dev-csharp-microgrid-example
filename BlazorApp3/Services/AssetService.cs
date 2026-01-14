using BlazorApp3.Data;
using BlazorApp3.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Services;

public class AssetService : IAssetService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public AssetService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<EnergyAsset>> GetAllAssetsAsync()
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.Assets.ToListAsync();
    }

    public async Task<EnergyAsset?> GetAssetByIdAsync(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.Assets.FindAsync(id);
    }

    public async Task AddAssetAsync(EnergyAsset asset)
    {
        using var context = _contextFactory.CreateDbContext();
        context.Assets.Add(asset);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAssetAsync(EnergyAsset asset)
    {
        using var context = _contextFactory.CreateDbContext();
        context.Entry(asset).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

    public async Task DeleteAssetAsync(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        var asset = await context.Assets.FindAsync(id);
        if (asset != null)
        {
            context.Assets.Remove(asset);
            await context.SaveChangesAsync();
        }
    }
}
