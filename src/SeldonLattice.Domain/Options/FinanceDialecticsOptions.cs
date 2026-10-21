namespace SeldonLattice.Domain.Options;

public class FinanceDialecticsOptions : DialecticsOptions
{
    public string MediatorStrategy { get; set; } = "SingleAuto";
    public bool PerTurnMediationEnabled { get; set; } = false;
    public string[] KeynesKeywords { get; set; } = [
        "aggregate demand", "animal spirits", "liquidity", "multiplier", "fiscal",
        "keynesian", "paradox of thrift", "effective demand", "marginal efficiency", "general theory"
    ];
    public string[] HayekKeywords { get; set; } = [
        "price signal", "spontaneous order", "knowledge problem", "austrian", "malinvestment",
        "business cycle", "capital structure", "pure time preference", "roundabout production", "denationalisation"
    ];
    public double RiskConfidenceMinimum { get; set; } = 0.6;
    public double MaxPortfolioRisk { get; set; } = 0.25;
    public string[] MarketDataSources { get; set; } = [
        "filings", "reports", "news", "analyst_notes", "earnings_calls"
    ];
}
