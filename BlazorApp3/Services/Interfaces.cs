using BlazorApp3.Data.Models;

namespace BlazorApp3.Services;

public interface IAssetService
{
    Task<List<EnergyAsset>> GetAllAssetsAsync();
    Task<EnergyAsset?> GetAssetByIdAsync(int id);
    Task AddAssetAsync(EnergyAsset asset);
    Task UpdateAssetAsync(EnergyAsset asset);
    Task DeleteAssetAsync(int id);
}

public interface IStatsService
{
    Task<double> GetTotalProductionTodayAsync();
    Task<double> GetTotalConsumptionTodayAsync();
    Task<double> GetEfficiencyScoreAsync();
    Task<EnergyAsset?> GetTopConsumerAsync();
    Task<List<EnergyReading>> GetRecentReadingsAsync(int count);
    Task SimulateHourAsync();
}
