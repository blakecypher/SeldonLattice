namespace SeldonLattice.Domain.Options;

public class GovernanceDialecticsOptions : DialecticsOptions
{
    public string MediatorStrategy { get; set; } = "SingleAuto";
    public bool PerTurnMediationEnabled { get; set; } = false;
    public string[] RawlsKeywords { get; set; } = [
        "fairness", "justice", "veil of ignorance", "equity", "rights",
        "least advantaged", "distributive", "impartial", "reasonable", "legitimacy",
        "public reason", "primary goods", "basic structure", "moral arbitrariness"
    ];
    public string[] FriedmanKeywords { get; set; } = [
        "efficiency", "freedom", "liberty", "cost", "regulation",
        "innovation", "competition", "market", "welfare", "choice",
        "unintended consequences", "overreach", "compliance burden", "rent-seeking"
    ];
    public string[] ComplianceFrameworks { get; set; } = [
        "EU AI Act", "GDPR", "CCPA", "NIST AI RMF", "ISO 42001"
    ];
    public double BiasDetectionThreshold { get; set; } = 0.15;
    public double FairnessConfidenceMinimum { get; set; } = 0.6;
}
