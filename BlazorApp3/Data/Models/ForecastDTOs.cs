namespace BlazorApp3.Data.Models;

public class ForecastSummary
{
    public DateTime Hour { get; set; }
    public double TotalProduction { get; set; }
    public double TotalConsumption { get; set; }
    public double NetBalance => TotalProduction + TotalConsumption; // Production is +ve, Consumption is -ve
}

public class SurplusDeficitWindow
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double TotalNetKw { get; set; }
    public bool IsSurplus => TotalNetKw > 0;
}

public class OptimizationRecommendation
{
    public string AssetName { get; set; } = string.Empty;
    public string OriginalTimeWindow { get; set; } = string.Empty;
    public string OptimizedTimeWindow { get; set; } = string.Empty;
    public double ExpectedSavingsKwh { get; set; }
}
