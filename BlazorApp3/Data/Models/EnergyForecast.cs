using BlazorApp3.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace BlazorApp3.Data.Models;

public class EnergyForecast
{
    public int Id { get; set; }
    
    [Required]
    public int AssetId { get; set; }
    
    public EnergyAsset? Asset { get; set; }
    
    [Required]
    public DateTime ForecastTimestamp { get; set; }
    
    [Required]
    public double ExpectedKw { get; set; }
    
    [Required]
    public ConfidenceLevel ConfidenceLevel { get; set; }
}
