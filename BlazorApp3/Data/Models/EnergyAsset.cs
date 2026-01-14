using System.ComponentModel.DataAnnotations;
using BlazorApp3.Data.Enums;

namespace BlazorApp3.Data.Models;

public class EnergyAsset
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public AssetType Type { get; set; }

    [Required]
    [Range(0, 1000000, ErrorMessage = "Capacity must be positive.")]
    public double MaxCapacityKw { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<EnergyReading> Readings { get; set; } = new List<EnergyReading>();
}
