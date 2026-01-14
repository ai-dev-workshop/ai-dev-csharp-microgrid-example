using System.ComponentModel.DataAnnotations;

namespace BlazorApp3.Data.Models;

public class EnergyTradeTransaction
{
    public int Id { get; set; }

    [Required]
    public int SellerAssetId { get; set; }
    public EnergyAsset? SellerAsset { get; set; }

    [Required]
    public int BuyerAssetId { get; set; }
    public EnergyAsset? BuyerAsset { get; set; }

    [Required]
    [Range(0, 1000000)]
    public double TradedKwh { get; set; }

    [Required]
    [Range(0, 1000)]
    public double PricePerKwh { get; set; }

    [Required]
    public double TotalPrice { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public bool IsExternal { get; set; }
}
