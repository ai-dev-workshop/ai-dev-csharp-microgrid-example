using System.ComponentModel.DataAnnotations;

namespace BlazorApp3.Data.Models;

public class FlexibleLoadProfile
{
    public int Id { get; set; }
    
    [Required]
    public int AssetId { get; set; }
    
    public EnergyAsset? Asset { get; set; }
    
    [Required]
    public double MinKw { get; set; }
    
    [Required]
    public double MaxKw { get; set; }
    
    public bool IsShiftable { get; set; }
    
    [Range(0, 23)]
    public int PreferredStartHour { get; set; }
    
    [Range(0, 23)]
    public int PreferredEndHour { get; set; }
}
