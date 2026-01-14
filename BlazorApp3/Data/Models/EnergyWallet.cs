using System.ComponentModel.DataAnnotations;

namespace BlazorApp3.Data.Models;

public class EnergyWallet
{
    [Key]
    public int AssetId { get; set; }
    public EnergyAsset? Asset { get; set; }

    [Required]
    public double CreditBalance { get; set; }

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
