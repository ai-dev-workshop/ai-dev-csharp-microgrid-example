using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorApp3.Data.Models;

public class EnergyReading
{
    public int Id { get; set; }

    [Required]
    public int AssetId { get; set; }

    [ForeignKey(nameof(AssetId))]
    public EnergyAsset? Asset { get; set; }

    [Required]
    public DateTime Timestamp { get; set; }

    [Required]
    public double ValueKw { get; set; }

    [Required]
    public double ConsumptionKwh { get; set; }
}
