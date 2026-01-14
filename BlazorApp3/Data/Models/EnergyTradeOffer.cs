using System.ComponentModel.DataAnnotations;
using BlazorApp3.Data.Enums;

namespace BlazorApp3.Data.Models;

public class EnergyTradeOffer
{
    public int Id { get; set; }

    [Required]
    public int AssetId { get; set; }
    public EnergyAsset? Asset { get; set; }

    [Required]
    public TradeType TradeType { get; set; }

    [Required]
    [Range(0, 1000000)]
    public double AvailableKwh { get; set; }

    [Required]
    [Range(0, 1000)]
    public double PricePerKwh { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}
